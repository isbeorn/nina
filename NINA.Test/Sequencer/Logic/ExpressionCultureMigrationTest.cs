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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Globalization;

namespace NINA.Test.Sequencer.Logic {

    [TestFixture]
    public class ExpressionCultureMigrationTest {

        [Test]
        public void LegacyNumericExpressionBackfills_UseInvariantCulture_WhenCurrentCultureUsesDecimalComma() {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo originalUICulture = CultureInfo.CurrentUICulture;

            try {
                CultureInfo commaDecimalCulture = CultureInfo.GetCultureInfo("de-DE");
                CultureInfo.CurrentCulture = commaDecimalCulture;
                CultureInfo.CurrentUICulture = commaDecimalCulture;

                IProfileService profileService = CreateProfileService();

                WaitForAltitude waitForAltitude = new WaitForAltitude(profileService);
                waitForAltitude.Data.Offset = 12.5;
                waitForAltitude.OffsetExpression = new Expression(string.Empty, waitForAltitude);

                waitForAltitude.OnDeserialized(default);

                waitForAltitude.OffsetExpression.Definition.Should().Be("12.5");

                WaitUntilAboveHorizon waitUntilAboveHorizon = new WaitUntilAboveHorizon(profileService);
                waitUntilAboveHorizon.Data.Offset = 12.5;
                waitUntilAboveHorizon.OffsetExpression = new Expression(string.Empty, waitUntilAboveHorizon);

                waitUntilAboveHorizon.OnDeserialized(default);

                waitUntilAboveHorizon.OffsetExpression.Definition.Should().Be("12.5");

                WaitForMoonAltitude waitForMoonAltitude = new WaitForMoonAltitude(profileService);
                waitForMoonAltitude.Data.Offset = 12.5;
                waitForMoonAltitude.OffsetExpression = new Expression(string.Empty, waitForMoonAltitude);

                waitForMoonAltitude.OnDeserialized(default);

                waitForMoonAltitude.OffsetExpression.Definition.Should().Be("12.5");

                WaitForSunAltitude waitForSunAltitude = new WaitForSunAltitude(profileService);
                waitForSunAltitude.Data.Offset = 12.5;
                waitForSunAltitude.OffsetExpression = new Expression(string.Empty, waitForSunAltitude);

                waitForSunAltitude.OnDeserialized(default);

                waitForSunAltitude.OffsetExpression.Definition.Should().Be("12.5");

                TakeSubframeExposure subframeExposure = new TakeSubframeExposure(
                    profileService,
                    new Mock<ICameraMediator>().Object,
                    new Mock<IImagingMediator>().Object,
                    new Mock<IImageSaveMediator>().Object,
                    new Mock<IImageHistoryVM>().Object);

                subframeExposure.ROI = 0.125;

                subframeExposure.ROIPctExpression.Definition.Should().Be("12.5");
                subframeExposure.ROI.Should().BeApproximately(0.125, 1e-10);

                SlewScopeToAltAz slewScopeToAltAz = new SlewScopeToAltAz(
                    profileService,
                    new Mock<ITelescopeMediator>().Object,
                    new Mock<IGuiderMediator>().Object);
                slewScopeToAltAz.Coordinates.Coordinates = new TopocentricCoordinates(
                    Angle.ByDegree(123.5),
                    Angle.ByDegree(12.5),
                    Angle.Zero,
                    Angle.Zero,
                    0);

                slewScopeToAltAz.OnDeserialized(default);

                slewScopeToAltAz.AltExpression.Definition.Should().Be("12.5");
                slewScopeToAltAz.AzExpression.Definition.Should().Be("123.5");

                AltitudeCondition altitudeCondition = new AltitudeCondition(profileService);
                altitudeCondition.Data.Coordinates.Coordinates = new Coordinates(
                    Angle.ByHours(1.25),
                    Angle.ByDegree(-12.5),
                    Epoch.J2000);
                altitudeCondition.Data.Offset = 12.5;

                altitudeCondition.OnDeserialized(default);

                altitudeCondition.RaExpression.Definition.Should().Be("1.25");
                altitudeCondition.DecExpression.Definition.Should().Be("-12.5");
                altitudeCondition.OffsetExpression.Definition.Should().Be("12.5");

                AboveHorizonCondition aboveHorizonCondition = new AboveHorizonCondition(profileService);
                aboveHorizonCondition.Data.Coordinates.Coordinates = new Coordinates(
                    Angle.ByHours(1.25),
                    Angle.ByDegree(-12.5),
                    Epoch.J2000);
                aboveHorizonCondition.Data.Offset = 12.5;

                aboveHorizonCondition.OnDeserialized(default);

                aboveHorizonCondition.RaExpression.Definition.Should().Be("1.25");
                aboveHorizonCondition.DecExpression.Definition.Should().Be("-12.5");
                aboveHorizonCondition.OffsetExpression.Definition.Should().Be("12.5");

                MoonAltitudeCondition moonAltitudeCondition = new MoonAltitudeCondition(profileService);
                moonAltitudeCondition.Data.Offset = 12.5;
                moonAltitudeCondition.OffsetExpression = new Expression(string.Empty, moonAltitudeCondition);

                moonAltitudeCondition.OnDeserialized(default);

                moonAltitudeCondition.OffsetExpression.Definition.Should().Be("12.5");

                SunAltitudeCondition sunAltitudeCondition = new SunAltitudeCondition(profileService);
                sunAltitudeCondition.Data.Offset = 12.5;
                sunAltitudeCondition.OffsetExpression = new Expression(string.Empty, sunAltitudeCondition);

                sunAltitudeCondition.OnDeserialized(default);

                sunAltitudeCondition.OffsetExpression.Definition.Should().Be("12.5");
            } finally {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }

        private static IProfileService CreateProfileService() {
            NINA.Profile.Profile profile = new NINA.Profile.Profile();
            profile.AstrometrySettings.Latitude = 0;
            profile.AstrometrySettings.Longitude = 0;
            profile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;

            Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            return profileServiceMock.Object;
        }
    }
}
