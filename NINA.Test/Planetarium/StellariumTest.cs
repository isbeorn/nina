#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using NUnit.Framework;

namespace NINA.Test.Planetarium {

    [TestFixture]
    internal class StellariumTest {
        private Mock<IProfileService> mockProfileService;
        private IPlanetarium sut;
        private Stellarium stelSut;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            mockProfileService = new Mock<IProfileService>();
        }

        [SetUp]
        public void Init() {
            mockProfileService.Reset();

            mockProfileService.Setup(m => m.ActiveProfile.PlanetariumSettings.StellariumHost).Returns("localhost");
            mockProfileService.Setup(m => m.ActiveProfile.PlanetariumSettings.StellariumPort).Returns(8090);
            sut = new Stellarium(mockProfileService.Object);
            stelSut = new Stellarium(mockProfileService.Object);
        }

        [Test]
        public void TestName() {
            sut.Name.Should().Be("Stellarium");
        }

        [Test]
        public void TestSellariumViewDeSerialization() {
            const string response = "{ \"altAz\":[-0.952086, 0.188428, 0.240887],\"j2000\":[0.266389, -0.258249, 0.928625],\"jNow\":[0.264699, -0.256746, 0.929525]}";

            var result = JObject.Parse(response).ToObject<Stellarium.StellariumView>();

            result.Should().NotBeNull();
            result.AltAz[0].Should().Be(-0.952086);
            result.AltAz[1].Should().Be(0.188428);
            result.AltAz[2].Should().Be(0.240887);
            result.J2000[0].Should().Be(0.266389);
            result.J2000[1].Should().Be(-0.258249);
            result.J2000[2].Should().Be(0.928625);
            result.JNOW[0].Should().Be(0.264699);
            result.JNOW[1].Should().Be(-0.256746);
            result.JNOW[2].Should().Be(0.929525);
        }

        [Test]
        public void TestSellariumObjectDeSerialization() {
            const string response = "{\"above - horizon\":true,\"airmass\":3.5391812324523926,\"altitude\":16.3449764251709," +
                                    "\"altitude - geometric\":16.289349570873643,\"ambientInt\":0.949999988079071,\"ambientLum\":6755.634765625," +
                                    "\"appSidTm\":\"11h55m30.7s\",\"azimuth\":15.39025788905692,\"azimuth - geometric\":15.39025788905692," +
                                    "\"bV\":0.39999961853027344,\"bmag\":7.199999809265137,\"dec\":68.24346529224371,\"decJ2000\":68.16330033167878," +
                                    "\"elat\":72.85083043584604,\"elatJ2000\":72.84944837653795,\"elong\":26.352720519894575,\"elongJ2000\":26.07779815603667," +
                                    "\"found\":true,\"glat\":14.1925758405833,\"glong\":104.06157692698363,\"hourAngle - dd\":223.41287169362303," +
                                    "\"hourAngle - hms\":\"14h53m39.1s\",\"iauConstellation\":\"Cep\",\"localized - name\":" +
                                    "\"Iris Nebula\",\"meanSidTm\":\"11h55m31.8s\",\"morpho\":\"EN + OCL; I, VBR\",\"name\":" +
                                    "\"Iris Nebula\",\"parallacticAngle\":-36.974259185001884,\"ra\":-44.53477774758223," +
                                    "\"raJ2000\":-44.596316809094915,\"rise\":\"100h00m\",\"rise - dhr\":100,\"set\":\"100h00m\"," +
                                    "\"set - dhr\":100,\"sglat\":33.78917143683577,\"sglong\":12.867843192721454,\"size\":0.005235987964042448," +
                                    "\"size - dd\":0.30000001192092896,\"size - deg\":\" + 0.30000°\",\"size - dms\":\" + 0°18'00.00\\\"\"," +
                                    "\"surface-brightness\":11.295450210571289,\"transit\":\"2h11m\",\"transit-dhr\":2.176408529281616," +
                                    "\"type\":\"cluster associated with nebulosity\",\"vmag\":6.800000190734863,\"vmage\":7.256612300872803}";

            var result = JObject.Parse(response).ToObject<Stellarium.StellariumObject>();

            result.Should().NotBeNull();
            result.RightAscension.Should().Be(-44.596316809094915);
            result.Declination.Should().Be(68.16330033167878);
            result.Name.Should().Be("Iris Nebula");
        }

        [Test]
        public void TestSellariumStatusAndLocationDeSerialization() {
            const string response = "{\"location\":{\"altitude\":0,\"country\":\"United States of America\"," +
                                    "\"landscapeKey\":\"\",\"latitude\":33.02870178222656,\"longitude\":-117.08460235595703," +
                                    "\"name\":\"San Diego\",\"planet\":\"Earth\",\"role\":\"X\",\"state\":\"California\"}," +
                                    "\"selectioninfo\":\"\",\"time\":{\"deltaT\":0.0008233898413509664,\"gmtShift\":-0.2916666666666667," +
                                    "\"isTimeNow\":true,\"jday\":2459047.5159638654,\"local\":\"2020 - 07 - 16T17: 22:59.278\"," +
                                    "\"timeZone\":\"UTC - 07:00\",\"timerate\":1.1574074074074073e-05,\"utc\":\"2020 - 07 - 17T00: 22:59.278Z\"}," +
                                    "\"view\":{\"fov\":28.7772606394507}}";

            var result = JObject.Parse(response).ToObject<Stellarium.StellariumStatus>();

            result.Should().NotBeNull();
            result.Location.Latitude.Should().Be(33.02870178222656);
            result.Location.Longitude.Should().Be(-117.08460235595703);
            result.Location.Altitude.Should().Be(0);
        }

        [Test]
        public void TestSellariumOcularsDeSerialization() {
            const string response = "{\"Oculars.arrowButtonScale\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":6,\"typeString\":\"double\",\"value\":1.5,\"variantType\":\"double\"}," +
                                    "\"Oculars.ccdCropOverlaySize\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":2,\"typeString\":\"int\",\"value\":250,\"variantType\":\"int\"}," +
                                    "\"Oculars.enableCCD\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.enableCrosshairs\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.enableOcular\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.enableTelrad\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagAlignCrosshair\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagAutosetMountForCCD\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagDMSDegrees\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagGuiPanelEnabled\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagHideGridsLines\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagInitDirectionUsage\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagInitFOVUsage\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagLimitMagnitude\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagRequireSelection\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagScaleImageCircle\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagScalingFOVForCCD\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagScalingFOVForTelrad\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagSemiTransparency\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagShowCardinals\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagShowCcdCropOverlay\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagShowContour\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":false,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagShowOcularsButton\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.flagShowResolutionCriteria\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1,\"typeString\":\"bool\",\"value\":true,\"variantType\":\"bool\"}," +
                                    "\"Oculars.guiPanelFontSize\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":2,\"typeString\":\"int\",\"value\":12,\"variantType\":\"int\"}," +
                                    "\"Oculars.lineColor\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1024,\"typeString\":\"Vec3f\",\"value\":\"[0.77, 0.14, 0.16]\",\"variantType\":\"Vec3f\"}," +
                                    "\"Oculars.selectedCCDIndex\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":2,\"typeString\":\"int\",\"value\":2,\"variantType\":\"int\"}," +
                                    "\"Oculars.selectedCCDRotationAngle\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":6,\"typeString\":\"double\",\"value\":-246,\"variantType\":\"double\"}," +
                                    "\"Oculars.selectedLensIndex\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":2,\"typeString\":\"int\",\"value\":0,\"variantType\":\"int\"}," +
                                    "\"Oculars.selectedOcularIndex\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":2,\"typeString\":\"int\",\"value\":0,\"variantType\":\"int\"}," +
                                    "\"Oculars.selectedTelescopeIndex\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":2,\"typeString\":\"int\",\"value\":0,\"variantType\":\"int\"}," +
                                    "\"Oculars.textColor\":{\"canNotify\":true,\"isWritable\":true,\"typeEnum\":1024,\"typeString\":\"Vec3f\",\"value\":\"[0.8, 0.48, 0]\",\"variantType\":\"Vec3f\"}}";

            var result = JObject.Parse(response);
            result.Should().NotBeNull();

            bool enableCCD = stelSut.ParseEnableCCD(result);
            enableCCD.Should().Be(true);

            double angle = stelSut.ParseRotationAngle(result);
            angle.Should().Be(246d);
        }
    }
}