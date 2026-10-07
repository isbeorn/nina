#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class SkyObjectBaseTest {

        [TestCase(false)]
        [TestCase(true)]
        public void Image_LoadsOnWorkerAndPublishesFrozenImage(bool deferredResult) {
            var callerThreadId = Environment.CurrentManagedThreadId;
            var factoryThreadId = 0;
            var factoryCalls = 0;
            using var factoryEntered = new ManualResetEventSlim();
            using var releaseFactory = new ManualResetEventSlim();
            var imageReady = new TaskCompletionSource<BitmapSource>(TaskCreationOptions.RunContinuationsAsynchronously);
            var imagePublished = new TaskCompletionSource<BitmapSource>(TaskCreationOptions.RunContinuationsAsynchronously);
            var source = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Gray8, null, new byte[] { 10, 20, 30, 40 }, 2);
            source.Freeze();
            var sut = new DeepSkyObject(
                "M31",
                new Coordinates(0.712, 41.269, Epoch.J2000, Coordinates.RAType.Hours),
                _ => {
                    Interlocked.Increment(ref factoryCalls);
                    factoryThreadId = Environment.CurrentManagedThreadId;
                    factoryEntered.Set();
                    if (deferredResult) {
                        return imageReady.Task;
                    }
                    if (!releaseFactory.Wait(TimeSpan.FromSeconds(5))) {
                        throw new TimeoutException("The test did not release the image factory.");
                    }
                    return Task.FromResult(source);
                },
                null);
            sut.PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(SkyObjectBase.Image)) {
                    imagePublished.TrySetResult(sut.Image);
                }
            };

            try {
                factoryCalls.Should().Be(0);
                sut.Image.Should().BeNull("the getter must return while the factory is still pending");
                factoryEntered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                factoryThreadId.Should().NotBe(callerThreadId);

                releaseFactory.Set();
                imageReady.TrySetResult(source);
                imagePublished.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                imagePublished.Task.Result.Should().BeSameAs(source);
                imagePublished.Task.Result.IsFrozen.Should().BeTrue();
                sut.Image.Should().BeSameAs(source);
                factoryCalls.Should().Be(1);
            } finally {
                releaseFactory.Set();
                imageReady.TrySetResult(source);
                imagePublished.Task.Wait(TimeSpan.FromSeconds(5));
            }
        }

        [Test]
        public void SetDateAndPosition_RefreshesBoundAltitudeData() {
            DeepSkyObject sut = new DeepSkyObject(
                "M31",
                new Coordinates(0.712, 41.269, Epoch.J2000, Coordinates.RAType.Hours),
                null);
            DateTime firstDate = new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Local);
            sut.SetDateAndPosition(firstDate, 50, 16.5);
            List<DataPoint> firstAltitudes = sut.Altitudes;
            List<DataPoint> boundAltitudes = firstAltitudes;
            sut.PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(SkyObjectBase.Altitudes)) {
                    boundAltitudes = sut.Altitudes;
                }
            };

            sut.SetDateAndPosition(firstDate.AddDays(1), 50, 16.5);

            boundAltitudes.Should().NotBeSameAs(firstAltitudes);
            boundAltitudes[0].X.Should().NotBe(firstAltitudes[0].X);
        }
    }
}