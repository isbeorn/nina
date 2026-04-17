using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageData;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NINA.Test.Image.FileFormat {

    [TestFixture]
    public class XISFHeaderBehaviorTest {

        /// <summary>
        /// Verifies image-level XISF mutation APIs guard against use before an Image element has been created.
        /// </summary>
        [Test]
        public void ImageMutators_WithoutImageMetaDataThrowInvalidOperationException() {
            var header = new XISFHeader();

            Assert.Multiple(() => {
                Assert.Throws<InvalidOperationException>(() => header.AddImageFITSKeyword("OBJECT", "M42"));
                Assert.Throws<InvalidOperationException>(() => header.AddImageProperty(XISFImageProperty.Observation.Object.Name, "M42"));
                Assert.Throws<InvalidOperationException>(() => header.AddCfaAttribute("RGGB", 2, 2));
            });
        }

        /// <summary>
        /// Verifies XISF metadata serialization normalizes snapshot image type, emits deterministic geometry, and omits duplicate FITS keywords.
        /// </summary>
        [Test]
        public void AddImageMetaData_NormalizesSnapshotAndPreventsDuplicateFitsKeywords() {
            var header = new XISFHeader();

            header.AddImageMetaData(
                new ImageProperties(width: 17, height: 9, bitDepth: 16, isBayered: true, gain: 100, offset: 20),
                imageType: "SNAPSHOT");
            header.AddImageFITSKeyword("IMAGETYP", "BIAS", "Duplicate should be ignored");

            XElement image = header.Image;
            var imageTypeKeyword = image.Elements().Single(x => x.Name.LocalName == "FITSKeyword" && x.Attribute("name")?.Value == "IMAGETYP");

            image.Attribute("geometry")?.Value.Should().Be("17:9:1");
            image.Attribute("sampleFormat")?.Value.Should().Be("UInt16");
            image.Attribute("imageType")?.Value.Should().Be("LIGHT");
            imageTypeKeyword.Attribute("value")?.Value.Should().Be("'LIGHT'");
        }

        /// <summary>
        /// Verifies a rich XISF metadata round trip preserves coordinates, Bayer offsets, pier side, wind unit conversion, and WCS values.
        /// </summary>
        [Test]
        public void PopulateAndExtract_PreservesScientificMetadataAndCompatibilityKeywords() {
            ImageMetaData source = CreateRichMetaData();
            var header = new XISFHeader();
            header.AddImageMetaData(
                new ImageProperties(width: 300, height: 200, bitDepth: 16, isBayered: true, gain: source.Camera.Gain, offset: source.Camera.Offset),
                source.Image.ImageType);

            header.Populate(source);

            ImageMetaData extracted = header.ExtractMetaData();

            extracted.Image.ExposureStart.Should().Be(source.Image.ExposureStart.ToUniversalTime());
            extracted.Image.ExposureMidPoint.Should().Be(source.Image.ExposureMidPoint);
            extracted.Image.ExposureTime.Should().BeApproximately(source.Image.ExposureTime, 1e-8);
            extracted.Camera.Name.Should().Be(source.Camera.Name);
            extracted.Camera.Id.Should().Be(source.Camera.Id);
            extracted.Camera.Gain.Should().Be(source.Camera.Gain);
            extracted.Camera.Offset.Should().Be(source.Camera.Offset);
            extracted.Camera.ElectronsPerADU.Should().BeApproximately(source.Camera.ElectronsPerADU, 1e-8);
            extracted.Camera.BinX.Should().Be(source.Camera.BinX);
            extracted.Camera.BinY.Should().Be(source.Camera.BinY);
            extracted.Camera.Temperature.Should().BeApproximately(source.Camera.Temperature, 1e-8);
            extracted.Camera.SetPoint.Should().BeApproximately(source.Camera.SetPoint, 1e-8);
            extracted.Camera.PixelSize.Should().BeApproximately(source.Camera.PixelSize, 1e-8);
            extracted.Camera.ReadoutModeName.Should().Be(source.Camera.ReadoutModeName);
            extracted.Camera.SensorType.Should().Be(source.Camera.SensorType);
            extracted.Camera.BayerOffsetX.Should().Be(source.Camera.BayerOffsetX);
            extracted.Camera.BayerOffsetY.Should().Be(source.Camera.BayerOffsetY);
            extracted.Camera.USBLimit.Should().Be(source.Camera.USBLimit);
            extracted.Observer.Elevation.Should().BeApproximately(source.Observer.Elevation, 1e-8);
            extracted.Observer.Latitude.Should().BeApproximately(source.Observer.Latitude, 1e-8);
            extracted.Observer.Longitude.Should().BeApproximately(source.Observer.Longitude, 1e-8);
            extracted.Observer.Site.Should().Be(source.Observer.Site);
            extracted.Observer.Observatory.Should().Be(source.Observer.Observatory);
            extracted.Observer.Name.Should().Be(source.Observer.Name);
            extracted.Telescope.Name.Should().Be(source.Telescope.Name);
            extracted.Telescope.FocalLength.Should().BeApproximately(source.Telescope.FocalLength, 1e-8);
            extracted.Telescope.FocalRatio.Should().BeApproximately(source.Telescope.FocalRatio, 1e-8);
            extracted.Telescope.Coordinates.RADegrees.Should().BeApproximately(source.Telescope.Coordinates.RADegrees, 1e-8);
            extracted.Telescope.Coordinates.Dec.Should().BeApproximately(source.Telescope.Coordinates.Dec, 1e-8);
            extracted.Telescope.SideOfPier.Should().Be(source.Telescope.SideOfPier);
            extracted.Telescope.Altitude.Should().BeApproximately(source.Telescope.Altitude, 1e-8);
            extracted.Telescope.Azimuth.Should().BeApproximately(source.Telescope.Azimuth, 1e-8);
            extracted.Target.Name.Should().Be(source.Target.Name);
            extracted.Target.Coordinates.RADegrees.Should().BeApproximately(source.Target.Coordinates.RADegrees, 1e-8);
            extracted.Target.Coordinates.Dec.Should().BeApproximately(source.Target.Coordinates.Dec, 1e-8);
            extracted.Target.PositionAngle.Should().BeApproximately(source.Target.PositionAngle, 1e-8);
            extracted.Focuser.Name.Should().Be(source.Focuser.Name);
            extracted.FilterWheel.Filter.Should().Be(source.FilterWheel.Filter);
            extracted.WeatherData.Humidity.Should().BeApproximately(source.WeatherData.Humidity, 1e-8);
            extracted.WeatherData.Pressure.Should().BeApproximately(source.WeatherData.Pressure, 1e-8);
            extracted.WeatherData.Temperature.Should().BeApproximately(source.WeatherData.Temperature, 1e-8);
            extracted.WeatherData.WindDirection.Should().BeApproximately(source.WeatherData.WindDirection, 1e-8);
            extracted.WeatherData.WindGust.Should().BeApproximately(source.WeatherData.WindGust, 1e-8);
            extracted.WeatherData.WindSpeed.Should().BeApproximately(source.WeatherData.WindSpeed, 1e-8);
            extracted.WorldCoordinateSystem.Should().NotBeNull();
            extracted.WorldCoordinateSystem.Point.X.Should().BeApproximately(150.0, 1e-8);
            extracted.WorldCoordinateSystem.Point.Y.Should().BeApproximately(100.0, 1e-8);
            extracted.WorldCoordinateSystem.Coordinates.RADegrees.Should().BeApproximately(201.5, 1e-8);
            extracted.WorldCoordinateSystem.Coordinates.Dec.Should().BeApproximately(-43.25, 1e-8);
            extracted.WorldCoordinateSystem.PixelScaleX.Should().BeApproximately(0.972, 1e-8);
            extracted.WorldCoordinateSystem.PixelScaleY.Should().BeApproximately(0.972, 1e-8);
        }

        /// <summary>
        /// Verifies XISF XML saving produces a parseable UTF-8 XML document and reports the same byte count as the saved stream.
        /// </summary>
        [Test]
        public void Save_WritesParseableXmlAndByteCountMatchesStreamLength() {
            var header = new XISFHeader();
            header.AddImageMetaData(new ImageProperties(width: 4, height: 3, bitDepth: 16, isBayered: false, gain: 0, offset: 0), "LIGHT");
            header.AddImageProperty(XISFImageProperty.Observation.Object.Name, "NGC\u0001 7000", "Invalid XML control char is replaced");

            using var stream = new MemoryStream();
            header.Save(stream);
            stream.Position = 0;

            XDocument parsed = XDocument.Load(stream);

            header.ByteCount.Should().Be((int)stream.Length);
            parsed.Root.Should().NotBeNull();
            parsed.Descendants().Single(x => x.Name.LocalName == "Property" && x.Attribute("id")?.Value == "Observation:Object:Name")
                .Value.Should().Be("NGC\uFFFD 7000");
        }

        private static ImageMetaData CreateRichMetaData() {
            var metadata = new ImageMetaData();
            metadata.Image.ImageType = "LIGHT";
            metadata.Image.ExposureStart = new DateTime(2025, 3, 4, 22, 10, 30, DateTimeKind.Utc);
            metadata.Image.ExposureMidPoint = new DateTime(2025, 3, 4, 22, 11, 30, DateTimeKind.Utc);
            metadata.Image.ExposureTime = 120.0;
            metadata.Camera.Name = "ASI2600MC Pro";
            metadata.Camera.Id = "CAM-XISF-1";
            metadata.Camera.Gain = 101;
            metadata.Camera.Offset = 50;
            metadata.Camera.ElectronsPerADU = 0.71;
            metadata.Camera.BinX = 2;
            metadata.Camera.BinY = 2;
            metadata.Camera.Temperature = -8.1;
            metadata.Camera.SetPoint = -10.0;
            metadata.Camera.PixelSize = 3.76;
            metadata.Camera.ReadoutModeName = "Low Noise";
            metadata.Camera.SensorType = SensorType.RGGB;
            metadata.Camera.BayerPattern = BayerPatternEnum.RGGB;
            metadata.Camera.BayerOffsetX = 1;
            metadata.Camera.BayerOffsetY = 2;
            metadata.Camera.USBLimit = 80;
            metadata.Observer.Elevation = 1420.0;
            metadata.Observer.Latitude = -31.2733;
            metadata.Observer.Longitude = 149.065;
            metadata.Observer.Site = "Dome A";
            metadata.Observer.Observatory = "Siding Spring";
            metadata.Observer.Name = "Ada";
            metadata.Telescope.Name = "RC8";
            metadata.Telescope.FocalLength = 1624.0;
            metadata.Telescope.FocalRatio = 8.0;
            metadata.Telescope.Coordinates = new Coordinates(Angle.ByDegree(187.5), Angle.ByDegree(-22.25), Epoch.J2000);
            metadata.Telescope.SideOfPier = PierSide.pierEast;
            metadata.Telescope.Altitude = 61.2;
            metadata.Telescope.Azimuth = 180.5;
            metadata.Target.Name = "NGC 3372";
            metadata.Target.Coordinates = new Coordinates(Angle.ByDegree(AstroUtil.HMSToDegrees("10 45 03.6")), Angle.ByDegree(AstroUtil.DMSToDegrees("-59 41 04.0")), Epoch.J2000);
            metadata.Target.PositionAngle = 123.4;
            metadata.Focuser.Name = "EAF";
            metadata.FilterWheel.Filter = "Ha";
            metadata.WeatherData.Humidity = 45.0;
            metadata.WeatherData.Pressure = 1012.5;
            metadata.WeatherData.Temperature = 2.0;
            metadata.WeatherData.WindDirection = 270.0;
            metadata.WeatherData.WindGust = 10.0;
            metadata.WeatherData.WindSpeed = 5.0;
            metadata.GenericHeaders.Add(new StringMetaDataHeader("CTYPE1", "RA---TAN", "WCS projection"));
            metadata.GenericHeaders.Add(new StringMetaDataHeader("CTYPE2", "DEC--TAN", "WCS projection"));
            metadata.GenericHeaders.Add(new DoubleMetaDataHeader("CRPIX1", 150.0, "Reference pixel X"));
            metadata.GenericHeaders.Add(new DoubleMetaDataHeader("CRPIX2", 100.0, "Reference pixel Y"));
            metadata.GenericHeaders.Add(new DoubleMetaDataHeader("CRVAL1", 201.5, "Reference RA"));
            metadata.GenericHeaders.Add(new DoubleMetaDataHeader("CRVAL2", -43.25, "Reference Dec"));
            metadata.GenericHeaders.Add(new DoubleMetaDataHeader("CD1_1", -0.00027, "CD matrix"));
            metadata.GenericHeaders.Add(new DoubleMetaDataHeader("CD1_2", 0.0, "CD matrix"));
            metadata.GenericHeaders.Add(new DoubleMetaDataHeader("CD2_1", 0.0, "CD matrix"));
            metadata.GenericHeaders.Add(new DoubleMetaDataHeader("CD2_2", 0.00027, "CD matrix"));
            return metadata;
        }
    }
}
