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
using Moq;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using static NINA.Test.HorizonTestData;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitUntilAboveHorizonUpperHorizonTest {
        private const double Latitude = 47;
        private const double Longitude = 8;
        private static readonly DateTime Time = DateTime.SpecifyKind(new DateTime(2020, 1, 1, 23, 0, 0), DateTimeKind.Utc);

        private AstrometrySettings settings;
        private Mock<IProfileService> profileServiceMock;
        private Coordinates coordinates;
        private double altitude;

        [SetUp]
        public void Setup() {
            settings = new AstrometrySettings() { Latitude = Latitude, Longitude = Longitude };
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.Setup(m => m.ActiveProfile.AstrometrySettings).Returns(settings);

            // The target position is frozen at a fixed time via the date provider of the coordinates
            var dateProvider = new Mock<ICustomDateTime>();
            dateProvider.SetupGet(x => x.Now).Returns(Time);
            coordinates = new Coordinates(Angle.ByHours(3), Angle.ByDegree(30), Epoch.J2000, dateProvider.Object);
            altitude = coordinates.Transform(Angle.ByDegree(Latitude), Angle.ByDegree(Longitude), 0).Altitude.Degree;
            altitude.Should().BeGreaterThan(25);
        }

        private WaitUntilAboveHorizon CreateSut() {
            var sut = new WaitUntilAboveHorizon(profileServiceMock.Object);
            sut.UpdateInterval = 0;
            // The item uses a default offset of 30 degrees for the lower horizon, which would move the lower bound above the target
            sut.Offset = 0;
            sut.Data.Coordinates.Coordinates = coordinates;
            sut.Coordinates = sut.Data.Coordinates;
            return sut;
        }

        [Test]
        public async Task TargetInsideWindow_ItemFinishesImmediately() {
            settings.Horizon = Dual(altitude - 10, altitude + 10);
            var sut = CreateSut();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await sut.Run(default, cts.Token);

            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
        }

        [Test]
        public async Task TargetAboveUpperHorizon_ItemKeepsWaiting() {
            settings.Horizon = Dual(altitude - 20, altitude - 5);
            var sut = CreateSut();
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300))) {
                Func<Task> run = () => sut.Execute(null, cts.Token);

                await run.Should().ThrowAsync<OperationCanceledException>();
            }
            sut.Data.UpperTargetAltitude.Should().BeApproximately(altitude - 5, 0.01);
        }

        [Test]
        public async Task UpperOffset_DefaultsToZero_AndMovesTheUpperBoundWhenSet() {
            settings.Horizon = Dual(altitude - 20, altitude + 5);
            var sut = CreateSut();
            sut.Data.UpperOffset.Should().Be(0);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))) {
                await sut.Run(default, cts.Token);
            }
            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);

            // A 10 degree upper margin puts the upper bound below the target
            var margin = CreateSut();
            margin.UpperOffset = 10;
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300))) {
                Func<Task> run = () => margin.Execute(null, cts.Token);

                await run.Should().ThrowAsync<OperationCanceledException>();
            }
            margin.Data.UpperTargetAltitude.Should().BeApproximately(altitude - 5, 0.01);
        }

        [Test]
        public void Clone_KeepsTheUpperOffsetExpressionAndData() {
            settings.Horizon = Dual(altitude - 20, altitude + 5);
            var sut = CreateSut();
            sut.UpperOffsetExpression.Definition = "7";
            _ = sut.UpperOffset;

            var clone = (WaitUntilAboveHorizon)sut.Clone();

            clone.UpperOffsetExpression.Definition.Should().Be("7");
            clone.UpperOffset.Should().Be(7);
            clone.Data.UpperOffset.Should().Be(7);
        }

        [Test]
        public void UpperOffset_SurvivesJsonRoundTrip() {
            settings.Horizon = Dual(altitude - 20, altitude + 5);
            var source = CreateSut();
            source.UpperOffset = 7;
            var json = JsonConvert.SerializeObject(source);

            // Sequences are restored by populating a freshly created instance
            var restored = new WaitUntilAboveHorizon(profileServiceMock.Object);
            JsonConvert.PopulateObject(json, restored);

            restored.UpperOffsetExpression.Definition.Should().Be("7");
            restored.Data.UpperOffset.Should().Be(7);
            restored.UpperOffset.Should().Be(7);
        }

        [Test]
        public async Task NoUpperHorizon_TargetAboveLowerHorizon_StillFinishes() {
            settings.Horizon = Flat(altitude - 10);
            var sut = CreateSut();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await sut.Run(default, cts.Token);

            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
        }

        [Test]
        public void Validate_TargetNeverInsideWindow_ReportsIssue() {
            settings.Horizon = Dual(50, 40);
            var sut = CreateSut();
            // a target in the deep south never gets close to the window at this latitude
            sut.Data.Coordinates.Coordinates = new Coordinates(Angle.ByHours(3), Angle.ByDegree(-80), Epoch.J2000);

            sut.Validate().Should().BeFalse();

            sut.Issues.Should().NotBeEmpty();
        }

        [Test]
        public void Validate_NoUpperHorizon_BehavesAsBefore() {
            settings.Horizon = Flat(altitude - 10);
            var sut = CreateSut();

            sut.Validate().Should().BeTrue();
            sut.Issues.Should().BeEmpty();
        }
    }
}
