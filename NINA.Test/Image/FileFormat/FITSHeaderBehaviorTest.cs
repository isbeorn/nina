using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Image.FileFormat.FITS;
using NINA.Image.ImageData;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NINA.Test.Image.FileFormat {

    [TestFixture]
    public class FITSHeaderBehaviorTest {

        /// <summary>
        /// Verifies FITS string-card encoding with embedded apostrophes, fixed 80-byte card length, and reversible original values.
        /// </summary>
        [Test]
        public void FITSHeaderCard_StringValueEscapesQuotesAndEncodesExactlyOneCard() {
            var card = new FITSHeaderCard("OBJECT", "Barnard's Galaxy", "Target name");

            string headerString = card.GetHeaderString();
            byte[] encoded = card.Encode();

            card.OriginalValue.Should().Be("Barnard's Galaxy");
            headerString.Should().HaveLength(FITS.HEADERCARDSIZE);
            headerString.Should().Contain("'Barnard''s Galaxy'");
            encoded.Should().HaveCount(FITS.HEADERCARDSIZE);
            Encoding.GetEncoding("iso-8859-1").GetString(encoded).Should().Be(headerString);
        }

        /// <summary>
        /// Verifies FITS header writing pads to a complete 2880-byte FITS block and preserves first-writer-wins keyword semantics.
        /// </summary>
        [Test]
        public void Write_PadsToFitsBlockAndIgnoresDuplicateKeywords() {
            var header = new FITSHeader(width: 17, height: 11);
            header.Add("OBJECT", "First target", "Original value");
            header.Add("OBJECT", "Second target", "Should be ignored");

            using var stream = new MemoryStream();
            header.Write(stream);

            string writtenHeader = Encoding.ASCII.GetString(stream.ToArray());
            int endOffset = header.HeaderCards.Count * FITS.HEADERCARDSIZE;

            stream.Length.Should().Be(FITS.BLOCKSIZE);
            header.HeaderCards.Single(x => x.Key == "OBJECT").OriginalValue.Should().Be("First target");
            writtenHeader.Substring(endOffset, 3).Should().Be("END");
            writtenHeader.Substring(endOffset + 3, FITS.HEADERCARDSIZE - 3).Should().Be(new string(' ', FITS.HEADERCARDSIZE - 3));
        }

        /// <summary>
        /// Verifies FITS image writing uses signed 16-bit big-endian storage with BZERO semantics and pads image data to a full FITS block.
        /// </summary>
        [Test]
        public void FITSWrite_EncodesUnsignedPixelsAsBigEndianSignedDataAndPadsToBlock() {
            ushort[] pixels = { 0, 32768, ushort.MaxValue };
            var fits = new FITS(pixels, width: 3, height: 1);

            using var stream = new MemoryStream();
            fits.Write(stream);

            byte[] bytes = stream.ToArray();

            stream.Length.Should().Be(FITS.BLOCKSIZE * 2);
            bytes[FITS.BLOCKSIZE + 0].Should().Be(0x80);
            bytes[FITS.BLOCKSIZE + 1].Should().Be(0x00);
            bytes[FITS.BLOCKSIZE + 2].Should().Be(0x00);
            bytes[FITS.BLOCKSIZE + 3].Should().Be(0x00);
            bytes[FITS.BLOCKSIZE + 4].Should().Be(0x7F);
            bytes[FITS.BLOCKSIZE + 5].Should().Be(0xFF);
            bytes.Skip(FITS.BLOCKSIZE + 6).Should().OnlyContain(x => x == 0);
        }

        /// <summary>
        /// Verifies observer latitude, longitude, and site are emitted independently instead of depending on unrelated elevation/name fields.
        /// </summary>
        [Test]
        public void PopulateFromMetaData_WritesObserverLocationFieldsIndependently() {
            var metadata = new ImageMetaData();
            metadata.Observer.Latitude = 47.1234;
            metadata.Observer.Longitude = 11.5678;
            metadata.Observer.Site = "High Meadow";

            var header = new FITSHeader(width: 2, height: 2);
            header.PopulateFromMetaData(metadata);

            ImageMetaData extracted = header.ExtractMetaData();

            header.HeaderCards.Single(x => x.Key == "SITELAT").OriginalValue.Should().Be("47.1234");
            header.HeaderCards.Single(x => x.Key == "SITELONG").OriginalValue.Should().Be("11.5678");
            header.HeaderCards.Single(x => x.Key == "SITENAME").OriginalValue.Should().Be("High Meadow");
            extracted.Observer.Latitude.Should().BeApproximately(47.1234, 1e-8);
            extracted.Observer.Longitude.Should().BeApproximately(11.5678, 1e-8);
            extracted.Observer.Site.Should().Be("High Meadow");
        }

        /// <summary>
        /// Verifies a representative FITS compatibility header extracts camera, mount, target, weather, and WCS metadata accurately.
        /// </summary>
        [Test]
        public void ExtractMetaData_ParsesCompatibilityKeywordsAndWorldCoordinateSystem() {
            var header = new FITSHeader(width: 300, height: 200);
            header.Add("IMAGETYP", "LIGHT", "Type of exposure");
            header.Add("EXPTIME", 123.45, "Exposure duration");
            header.Add("XBINNING", 2, "X bin");
            header.Add("YBINNING", 3, "Y bin");
            header.Add("GAIN", 120, "Gain");
            header.Add("OFFSET", 8, "Offset");
            header.Add("EGAIN", 0.47, "Electrons per ADU");
            header.Add("XPIXSZ", 7.52, "Physical pixel size after binning");
            header.Add("INSTRUME", "ASI2600MM Pro", "Camera");
            header.Add("CAMERAID", "CAM-123", "Camera id");
            header.Add("SET-TEMP", -10.0, "Set point");
            header.Add("CCD-TEMP", -9.8, "Sensor temperature");
            header.Add("READOUTM", "High Gain", "Readout mode");
            header.Add("BAYERPAT", " RGGB ", "Bayer pattern");
            header.Add("XBAYROFF", 1, "Bayer x offset");
            header.Add("YBAYROFF", 0, "Bayer y offset");
            header.Add("USBLIMIT", 55, "USB limit");
            header.Add("TELESCOP", "RC8", "Telescope");
            header.Add("FOCALLEN", 1624.0, "Focal length");
            header.Add("FOCRATIO", 8.0, "Focal ratio");
            header.Add("PIERSIDE", "West", "Pier side");
            header.Add("RA", "12 30 00", "Telescope RA");
            header.Add("DEC", "-22 15 00", "Telescope Dec");
            header.Add("CENTALT", 61.2, "Altitude");
            header.Add("CENTAZ", 180.5, "Azimuth");
            header.Add("SITEELEV", 1420.0, "Elevation");
            header.Add("SITELAT", "-31 16 24", "Latitude");
            header.Add("SITELONG", "149 03 54", "Longitude");
            header.Add("OBSERVER", "Ada", "Observer");
            header.Add("OBSERVAT", "Siding Spring", "Observatory");
            header.Add("SITENAME", "Dome A", "Site");
            header.Add("FWHEEL", "EFW", "Filter wheel");
            header.Add("FILTER", "Ha", "Filter");
            header.Add("OBJECT", "NGC 3372", "Target");
            header.Add("OBJCTRA", "10 45 03.6", "Target RA");
            header.Add("OBJCTDEC", "-59 41 04.0", "Target Dec");
            header.Add("OBJCTROT", 123.4, "Target rotation");
            header.Add("AIRMASS", 1.23, "Airmass");
            header.Add("FOCNAME", "EAF", "Focuser");
            header.Add("FOCUSPOS", 42123, "Focuser position");
            header.Add("FOCUSSZ", 2.5, "Focuser step size");
            header.Add("FOCUSTEM", 4.2, "Focuser temperature");
            header.Add("ROTNAME", "Falcon", "Rotator");
            header.Add("ROTATANG", 88.9, "Mechanical angle");
            header.Add("ROTSTPSZ", 0.1, "Step size");
            header.Add("CLOUDCVR", 12.0, "Cloud cover");
            header.Add("DEWPOINT", -3.0, "Dew point");
            header.Add("HUMIDITY", 45.0, "Humidity");
            header.Add("PRESSURE", 1012.5, "Pressure");
            header.Add("SKYBRGHT", 0.02, "Sky brightness");
            header.Add("MPSAS", 21.3, "Sky quality");
            header.Add("SKYTEMP", -15.0, "Sky temperature");
            header.Add("STARFWHM", 3.1, "Star FWHM");
            header.Add("AMBTEMP", 2.0, "Ambient temperature");
            header.Add("WINDDIR", 270.0, "Wind direction");
            header.Add("WINDGUST", 36.0, "Wind gust kph");
            header.Add("WINDSPD", 18.0, "Wind speed kph");
            header.Add("CTYPE1", "RA---TAN", "WCS projection");
            header.Add("CTYPE2", "DEC--TAN", "WCS projection");
            header.Add("CRPIX1", 150.0, "Reference pixel X");
            header.Add("CRPIX2", 100.0, "Reference pixel Y");
            header.Add("CRVAL1", 201.5, "Reference RA");
            header.Add("CRVAL2", -43.25, "Reference Dec");
            header.Add("CD1_1", -0.00027, "CD matrix");
            header.Add("CD1_2", 0.0, "CD matrix");
            header.Add("CD2_1", 0.0, "CD matrix");
            header.Add("CD2_2", 0.00027, "CD matrix");

            ImageMetaData metadata = header.ExtractMetaData();

            metadata.Image.ImageType.Should().Be("LIGHT");
            metadata.Image.ExposureTime.Should().BeApproximately(123.45, 1e-8);
            metadata.Camera.BinX.Should().Be(2);
            metadata.Camera.BinY.Should().Be(3);
            metadata.Camera.Gain.Should().Be(120);
            metadata.Camera.Offset.Should().Be(8);
            metadata.Camera.ElectronsPerADU.Should().BeApproximately(0.47, 1e-8);
            metadata.Camera.PixelSize.Should().BeApproximately(3.76, 1e-8);
            metadata.Camera.Name.Should().Be("ASI2600MM Pro");
            metadata.Camera.Id.Should().Be("CAM-123");
            metadata.Camera.SetPoint.Should().BeApproximately(-10.0, 1e-8);
            metadata.Camera.Temperature.Should().BeApproximately(-9.8, 1e-8);
            metadata.Camera.ReadoutModeName.Should().Be("High Gain");
            metadata.Camera.SensorType.Should().Be(SensorType.RGGB);
            metadata.Camera.BayerOffsetX.Should().Be(1);
            metadata.Camera.BayerOffsetY.Should().Be(0);
            metadata.Camera.USBLimit.Should().Be(55);
            metadata.Telescope.Name.Should().Be("RC8");
            metadata.Telescope.FocalLength.Should().BeApproximately(1624.0, 1e-8);
            metadata.Telescope.FocalRatio.Should().BeApproximately(8.0, 1e-8);
            metadata.Telescope.SideOfPier.Should().Be(PierSide.pierWest);
            metadata.Telescope.Coordinates.RADegrees.Should().BeApproximately(187.5, 1e-8);
            metadata.Telescope.Coordinates.Dec.Should().BeApproximately(-22.25, 1e-8);
            metadata.Telescope.Altitude.Should().BeApproximately(61.2, 1e-8);
            metadata.Telescope.Azimuth.Should().BeApproximately(180.5, 1e-8);
            metadata.Telescope.Airmass.Should().BeApproximately(1.23, 1e-8);
            metadata.Observer.Elevation.Should().BeApproximately(1420.0, 1e-8);
            metadata.Observer.Latitude.Should().BeApproximately(-31.273333333333333, 1e-8);
            metadata.Observer.Longitude.Should().BeApproximately(149.065, 1e-8);
            metadata.Observer.Name.Should().Be("Ada");
            metadata.Observer.Observatory.Should().Be("Siding Spring");
            metadata.Observer.Site.Should().Be("Dome A");
            metadata.FilterWheel.Name.Should().Be("EFW");
            metadata.FilterWheel.Filter.Should().Be("Ha");
            metadata.Target.Name.Should().Be("NGC 3372");
            metadata.Target.Coordinates.RADegrees.Should().BeApproximately(AstroUtil.HMSToDegrees("10 45 03.6"), 1e-8);
            metadata.Target.Coordinates.Dec.Should().BeApproximately(AstroUtil.DMSToDegrees("-59 41 04.0"), 1e-8);
            metadata.Target.PositionAngle.Should().BeApproximately(123.4, 1e-8);
            metadata.Focuser.Name.Should().Be("EAF");
            metadata.Focuser.Position.Should().Be(42123);
            metadata.Focuser.StepSize.Should().BeApproximately(2.5, 1e-8);
            metadata.Focuser.Temperature.Should().BeApproximately(4.2, 1e-8);
            metadata.Rotator.Name.Should().Be("Falcon");
            metadata.Rotator.MechanicalPosition.Should().BeApproximately(88.9, 1e-8);
            metadata.Rotator.StepSize.Should().BeApproximately(0.1, 1e-8);
            metadata.WeatherData.CloudCover.Should().BeApproximately(12.0, 1e-8);
            metadata.WeatherData.DewPoint.Should().BeApproximately(-3.0, 1e-8);
            metadata.WeatherData.Humidity.Should().BeApproximately(45.0, 1e-8);
            metadata.WeatherData.Pressure.Should().BeApproximately(1012.5, 1e-8);
            metadata.WeatherData.SkyBrightness.Should().BeApproximately(0.02, 1e-8);
            metadata.WeatherData.SkyQuality.Should().BeApproximately(21.3, 1e-8);
            metadata.WeatherData.SkyTemperature.Should().BeApproximately(-15.0, 1e-8);
            metadata.WeatherData.StarFWHM.Should().BeApproximately(3.1, 1e-8);
            metadata.WeatherData.Temperature.Should().BeApproximately(2.0, 1e-8);
            metadata.WeatherData.WindDirection.Should().BeApproximately(270.0, 1e-8);
            metadata.WeatherData.WindGust.Should().BeApproximately(10.0, 1e-8);
            metadata.WeatherData.WindSpeed.Should().BeApproximately(5.0, 1e-8);
            metadata.WorldCoordinateSystem.Should().NotBeNull();
            metadata.WorldCoordinateSystem.Point.X.Should().BeApproximately(150.0, 1e-8);
            metadata.WorldCoordinateSystem.Point.Y.Should().BeApproximately(100.0, 1e-8);
            metadata.WorldCoordinateSystem.Coordinates.RADegrees.Should().BeApproximately(201.5, 1e-8);
            metadata.WorldCoordinateSystem.Coordinates.Dec.Should().BeApproximately(-43.25, 1e-8);
            metadata.WorldCoordinateSystem.PixelScaleX.Should().BeApproximately(0.972, 1e-8);
            metadata.WorldCoordinateSystem.PixelScaleY.Should().BeApproximately(0.972, 1e-8);
        }

        /// <summary>
        /// Verifies invalid numeric FITS values fail closed to NaN or default integers rather than throwing during metadata extraction.
        /// </summary>
        [Test]
        public void ExtractMetaData_InvalidNumericValuesUseDefaultsWithoutThrowing() {
            var header = new FITSHeader(width: 4, height: 4);
            header.Add("EXPTIME", "not-a-number", "Invalid exposure");
            header.Add("GAIN", "invalid", "Invalid gain");
            header.Add("XPIXSZ", "invalid", "Invalid pixel size");

            ImageMetaData metadata = header.ExtractMetaData();

            metadata.Image.ExposureTime.Should().Be(double.NaN);
            metadata.Camera.Gain.Should().Be(0);
            metadata.Camera.PixelSize.Should().Be(double.NaN);
        }
    }
}
