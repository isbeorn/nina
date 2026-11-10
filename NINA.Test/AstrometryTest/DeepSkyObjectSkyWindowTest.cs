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
using NINA.Core.Model;
using NUnit.Framework;
using static NINA.Test.HorizonTestData;
using System;
using System.Linq;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class DeepSkyObjectSkyWindowTest {
        private const double Latitude = 50;
        private const double Longitude = 10;

        // Dec 50 at latitude 50 transits at the zenith and never drops below 10 degrees
        private static DeepSkyObject CreateObject(SkyWindow window) {
            var coordinates = new Coordinates(Angle.ByHours(5), Angle.ByDegree(50), Epoch.J2000);
            var dso = new DeepSkyObject("test", coordinates, window);
            dso.SetDateAndPosition(new DateTime(2024, 6, 1, 12, 0, 0), Latitude, Longitude);
            // Altitudes and the horizon series are calculated lazily on first access of Altitudes
            _ = dso.Altitudes;
            return dso;
        }

        [Test]
        public void NoProfiles_HorizonSeriesAreEmpty() {
            var sut = CreateObject(new SkyWindow(null, null));

            sut.Altitudes.Should().NotBeEmpty();
            sut.Horizon.Should().BeEmpty();
            sut.UpperHorizon.Should().BeEmpty();
        }

        [Test]
        public void LegacyCustomHorizonOverload_OnlyFillsLowerHorizon() {
            var sut = new DeepSkyObject("test", new Coordinates(Angle.ByHours(5), Angle.ByDegree(50), Epoch.J2000), Flat(15));
            sut.SetDateAndPosition(new DateTime(2024, 6, 1, 12, 0, 0), Latitude, Longitude);
            _ = sut.Altitudes;

            sut.Horizon.Should().HaveCount(sut.Altitudes.Count);
            sut.Horizon.Should().OnlyContain(p => p.Y == 15);
            sut.UpperHorizon.Should().BeEmpty();
        }

        [Test]
        public void LegacyCustomHorizonOverload_KeepsAnEmbeddedUpperHorizon() {
            var sut = new DeepSkyObject("test", new Coordinates(Angle.ByHours(5), Angle.ByDegree(50), Epoch.J2000), Dual(10, 70));
            sut.SetDateAndPosition(new DateTime(2024, 6, 1, 12, 0, 0), Latitude, Longitude);
            _ = sut.Altitudes;

            sut.Horizon.Should().OnlyContain(p => p.Y == 10);
            sut.UpperHorizon.Should().HaveCount(sut.Altitudes.Count);
            sut.UpperHorizon.Should().OnlyContain(p => p.Y == 70);
        }

        [Test]
        public void ProtectedLowerHorizon_StaysInSyncWithTheSkyWindow() {
            var lower = Flat(15);
            var sut = new ProbeObject(new SkyWindow(lower, Flat(70)));

            sut.LowerHorizonField.Should().BeSameAs(lower);

            var replacement = Flat(20);
            sut.SetSkyWindow(new SkyWindow(replacement, null));
            sut.LowerHorizonField.Should().BeSameAs(replacement);

            sut.SetCustomHorizon(null);
            sut.LowerHorizonField.Should().BeNull();
        }

        /// <summary>Stands in for a plugin class that derives from SkyObjectBase and reads the protected lower horizon</summary>
        private sealed class ProbeObject : DeepSkyObject {

            public ProbeObject(SkyWindow window)
                : base("probe", new Coordinates(Angle.ByHours(5), Angle.ByDegree(50), Epoch.J2000), window) {
            }

            public CustomHorizon LowerHorizonField => customHorizon;
        }

        [Test]
        public void UpperProfile_UpperHorizonSeriesIsSampledLikeAltitudes() {
            var sut = CreateObject(new SkyWindow(null, Flat(60)));

            sut.Horizon.Should().BeEmpty();
            sut.UpperHorizon.Should().HaveCount(sut.Altitudes.Count);
            sut.UpperHorizon.Should().OnlyContain(p => p.Y == 60);
            sut.UpperHorizon.Select(p => p.X).Should().Equal(sut.Altitudes.Select(p => p.X));
        }

        [Test]
        public void SetSkyWindow_UpdatesSeries() {
            var sut = CreateObject(new SkyWindow(null, null));

            sut.SetSkyWindow(new SkyWindow(Flat(20), Flat(70)));

            sut.Horizon.Should().OnlyContain(p => p.Y == 20);
            sut.UpperHorizon.Should().OnlyContain(p => p.Y == 70);

            sut.SetSkyWindow(new SkyWindow(null, null));

            sut.Horizon.Should().BeEmpty();
            sut.UpperHorizon.Should().BeEmpty();
        }

    }
}
