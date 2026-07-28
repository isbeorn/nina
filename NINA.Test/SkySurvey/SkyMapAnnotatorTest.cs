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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.SkySurvey;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Test.SkySurvey {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class SkyMapAnnotatorTest {

        [Test]
        public void ProjectionModeAndHorizon_LoadFromAndSaveToProfile() {
            FramingAssistantSettings settings = new FramingAssistantSettings {
                SkyMapProjectionMode = SkyMapProjectionMode.AltAz,
                ShowHorizon = true
            };
            Mock<IProfile> profile = new Mock<IProfile>();
            profile.SetupGet(x => x.FramingAssistantSettings).Returns(settings);
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);

            using SkyMapAnnotator sut = new SkyMapAnnotator(null, profileService.Object);

            sut.ProjectionMode.Should().Be(SkyMapProjectionMode.AltAz);
            sut.ShowHorizon.Should().BeTrue();

            sut.ProjectionMode = SkyMapProjectionMode.Equatorial;
            sut.ShowHorizon = false;

            settings.SkyMapProjectionMode.Should().Be(SkyMapProjectionMode.Equatorial);
            settings.ShowHorizon.Should().BeFalse();
        }

        [Test]
        public async Task ReenabledAltAzMode_AfterLocationChangeUsesNewObserver() {
            AstrometrySettings astrometrySettings = new AstrometrySettings {
                Latitude = 50,
                Longitude = 10
            };
            FramingAssistantSettings framingAssistantSettings = new FramingAssistantSettings();
            Mock<IProfile> profile = new Mock<IProfile>();
            profile.SetupGet(x => x.AstrometrySettings).Returns(astrometrySettings);
            profile.SetupGet(x => x.FramingAssistantSettings).Returns(framingAssistantSettings);
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            Coordinates center = CelestialCoordinates(85, 20);
            Coordinates target = CelestialCoordinates(90, 25);

            using SkyMapAnnotator sut = new SkyMapAnnotator(null, profileService.Object);
            await sut.Initialize(center, 40, 100, 100, 0, null, CancellationToken.None);
            sut.AnnotateGrid = false;
            sut.ProjectionMode = SkyMapProjectionMode.AltAz;
            sut.UpdateSkyMap();
            sut.Projection.Mode.Should().Be(SkyMapProjectionMode.AltAz);
            Point beforeLocationChange = sut.Projection.Project(target);

            sut.ProjectionMode = SkyMapProjectionMode.Equatorial;
            sut.UpdateSkyMap();
            astrometrySettings.Latitude = -30;
            profileService.Raise(x => x.LocationChanged += null, EventArgs.Empty);
            sut.ProjectionMode = SkyMapProjectionMode.AltAz;
            sut.UpdateSkyMap();

            Point afterLocationChange = sut.Projection.Project(target);
            afterLocationChange.Should().NotBeEquivalentTo(beforeLocationChange);
        }

        [Test]
        public async Task ProjectionChanged_OnlyFiresWhenViewportProjectionChanges() {
            FramingAssistantSettings framingAssistantSettings = new FramingAssistantSettings();
            Mock<IProfile> profile = new Mock<IProfile>();
            profile.SetupGet(x => x.FramingAssistantSettings).Returns(framingAssistantSettings);
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            using SkyMapAnnotator sut = new SkyMapAnnotator(null, profileService.Object);
            await sut.Initialize(CelestialCoordinates(85, 20), 40, 100, 100, 0, null, CancellationToken.None);
            SkyMapViewportProjection initialProjection = sut.Projection;
            int changes = 0;
            sut.ProjectionChanged += (_, _) => changes++;

            sut.UpdateSkyMap();

            sut.Projection.Should().BeSameAs(initialProjection);
            changes.Should().Be(0);

            sut.ShiftViewport(new Vector(10, 0));
            sut.UpdateSkyMap();
            SkyMapViewportProjection movedProjection = sut.Projection;
            sut.UpdateSkyMap();

            movedProjection.Should().NotBeSameAs(initialProjection);
            sut.Projection.Should().BeSameAs(movedProjection);
            changes.Should().Be(1);

            sut.ChangeFoV(20);
            changes.Should().Be(1);
            sut.UpdateSkyMap();
            changes.Should().Be(2);
        }

        [Test]
        public async Task ProjectionModeChanged_RebuildsProjectionWithoutExplicitRefreshCommand() {
            AstrometrySettings astrometrySettings = new AstrometrySettings {
                Latitude = 50,
                Longitude = 10
            };
            FramingAssistantSettings framingAssistantSettings = new FramingAssistantSettings();
            Mock<IProfile> profile = new Mock<IProfile>();
            profile.SetupGet(x => x.AstrometrySettings).Returns(astrometrySettings);
            profile.SetupGet(x => x.FramingAssistantSettings).Returns(framingAssistantSettings);
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            using SkyMapAnnotator sut = new SkyMapAnnotator(null, profileService.Object);
            await sut.Initialize(CelestialCoordinates(85, 20), 40, 100, 100, 0, null, CancellationToken.None);

            sut.ProjectionMode = SkyMapProjectionMode.AltAz;

            sut.Projection.Mode.Should().Be(SkyMapProjectionMode.AltAz);
        }

        [Test]
        public async Task ObservationTimeChanged_ReprojectsAltAzViewForSelectedTime() {
            AstrometrySettings astrometrySettings = new AstrometrySettings {
                Latitude = 50,
                Longitude = 10
            };
            FramingAssistantSettings framingAssistantSettings = new FramingAssistantSettings {
                SkyMapProjectionMode = SkyMapProjectionMode.AltAz
            };
            Mock<IProfile> profile = new Mock<IProfile>();
            profile.SetupGet(x => x.AstrometrySettings).Returns(astrometrySettings);
            profile.SetupGet(x => x.FramingAssistantSettings).Returns(framingAssistantSettings);
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            Coordinates target = CelestialCoordinates(90, 25);
            using SkyMapAnnotator sut = new SkyMapAnnotator(null, profileService.Object) {
                ObservationTime = new DateTime(2026, 7, 28, 18, 0, 0, DateTimeKind.Utc)
            };
            await sut.Initialize(CelestialCoordinates(85, 20), 40, 100, 100, 0, null, CancellationToken.None);
            Point first = sut.Projection.Project(target);

            sut.ObservationTime = sut.ObservationTime.Value.AddHours(6);

            Point later = sut.Projection.Project(target);
            later.Should().NotBeEquivalentTo(first);
        }

        [Test]
        public async Task ObservationTimeChanged_RefreshesObserverAtMinuteCadence() {
            AstrometrySettings astrometrySettings = new AstrometrySettings {
                Latitude = 50,
                Longitude = 10
            };
            FramingAssistantSettings framingAssistantSettings = new FramingAssistantSettings {
                SkyMapProjectionMode = SkyMapProjectionMode.AltAz
            };
            Mock<IProfile> profile = new Mock<IProfile>();
            profile.SetupGet(x => x.AstrometrySettings).Returns(astrometrySettings);
            profile.SetupGet(x => x.FramingAssistantSettings).Returns(framingAssistantSettings);
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            DateTime observationTime = new DateTime(2026, 7, 28, 18, 0, 0, DateTimeKind.Utc);
            using SkyMapAnnotator sut = new SkyMapAnnotator(null, profileService.Object) {
                ObservationTime = observationTime
            };
            await sut.Initialize(CelestialCoordinates(85, 20), 40, 100, 100, 0, null, CancellationToken.None);
            SkyMapViewportProjection initialProjection = sut.Projection;

            sut.ObservationTime = observationTime.AddSeconds(30);

            sut.Projection.Should().BeSameAs(initialProjection);

            sut.ObservationTime = observationTime.AddMinutes(1);

            sut.Projection.Should().NotBeSameAs(initialProjection);
        }

        private static Coordinates CelestialCoordinates(double rightAscension, double declination) {
            return new Coordinates(rightAscension, declination, Epoch.J2000, Coordinates.RAType.Degrees);
        }
    }
}