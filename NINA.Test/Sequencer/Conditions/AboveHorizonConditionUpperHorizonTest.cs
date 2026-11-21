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
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NUnit.Framework;
using static NINA.Test.HorizonTestData;
using System;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class AboveHorizonConditionUpperHorizonTest {
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

            coordinates = new Coordinates(Angle.ByHours(3), Angle.ByDegree(30), Epoch.J2000);
            altitude = coordinates.Transform(Angle.ByDegree(Latitude), Angle.ByDegree(Longitude), 0, Time).Altitude.Degree;
            // The test relies on the target being well above the horizon at the test time
            altitude.Should().BeGreaterThan(25);
        }

        private AboveHorizonCondition CreateSut() {
            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            var dateProvider = new Mock<ICustomDateTime>();
            dateProvider.SetupGet(x => x.Now).Returns(Time);
            sut.DateTime = dateProvider.Object;
            sut.Data.Coordinates.Coordinates = coordinates;
            return sut;
        }

        [Test]
        public void NoUpperHorizon_LowerHorizonBehavesAsBefore() {
            settings.Horizon = Flat(altitude - 10);

            CreateSut().Check(default, default).Should().BeTrue();
        }

        [Test]
        public void BetweenLowerAndUpperHorizon_CheckTrue() {
            settings.Horizon = Dual(altitude - 10, altitude + 10);

            CreateSut().Check(default, default).Should().BeTrue();
        }

        [Test]
        public void AboveUpperHorizon_CheckFalse() {
            settings.Horizon = Dual(altitude - 20, altitude - 5);

            CreateSut().Check(default, default).Should().BeFalse();
        }

        [Test]
        public void BelowLowerHorizon_CheckFalse_EvenWithUpperHorizon() {
            settings.Horizon = Dual(altitude + 5, altitude + 20);

            CreateSut().Check(default, default).Should().BeFalse();
        }

        [Test]
        public void UpperOffsetMovesTheUpperBound_ButOffsetDoesNot() {
            settings.Horizon = Dual(null, altitude + 3);
            var sut = CreateSut();

            sut.Check(default, default).Should().BeTrue();

            // The lower offset does not touch the upper horizon (and stays below the target here)
            sut.Offset = 5;
            sut.Check(default, default).Should().BeTrue();

            // a 5 degree upper margin moves the upper bound below the target
            sut.UpperOffset = 5;
            sut.Check(default, default).Should().BeFalse();
        }

        [Test]
        public void UpperBelowLower_CheckFalse() {
            settings.Horizon = Dual(altitude + 10, altitude - 10);

            CreateSut().Check(default, default).Should().BeFalse();
        }

        [Test]
        public void Clone_KeepsTheUpperHorizonAndTheUpperOffset() {
            settings.Horizon = Dual(null, altitude - 5);
            var sut = CreateSut();
            sut.UpperOffset = 3;

            var clone = (AboveHorizonCondition)sut.Clone();

            clone.Data.HasUpperHorizon.Should().BeTrue();
            clone.UpperOffset.Should().Be(3);
            clone.Data.UpperOffset.Should().Be(3);
        }
    }
}
