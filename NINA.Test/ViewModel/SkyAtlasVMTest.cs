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
using NINA.ViewModel;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class SkyAtlasVMTest {

        [Test]
        public void IsAboveHorizonForDuration_DoesNotPopulateAltitudeChart() {
            var referenceDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
            var dso = new DeepSkyObject("Equator", new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours), null);

            var result = SkyAtlasVM.IsAboveHorizonForDuration(
                dso,
                referenceDate,
                referenceDate,
                referenceDate.AddHours(3),
                latitude: 0,
                siderealTimeAtReferenceDate: 0,
                customHorizon: null,
                minimumDuration: 2,
                token: CancellationToken.None);

            result.Should().BeTrue();
            GetAltitudeCache(dso).Should().BeNull();
        }

        [Test]
        public void IsAboveHorizonForDuration_RejectsBelowHorizonWindow() {
            var referenceDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
            var dso = new DeepSkyObject("Equator", new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours), null);

            var result = SkyAtlasVM.IsAboveHorizonForDuration(
                dso,
                referenceDate,
                referenceDate.AddHours(8),
                referenceDate.AddHours(10),
                latitude: 0,
                siderealTimeAtReferenceDate: 0,
                customHorizon: null,
                minimumDuration: 1,
                token: CancellationToken.None);

            result.Should().BeFalse();
            GetAltitudeCache(dso).Should().BeNull();
        }

        private static object? GetAltitudeCache(DeepSkyObject dso) {
            return typeof(SkyObjectBase)
                .GetField("_altitudes", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(dso);
        }
    }
}
