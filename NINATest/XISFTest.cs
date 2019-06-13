using NINA.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Moq;
using NINA.Model.ImageData;
using System.IO;
using System.Globalization;
using NINA.Utility.Astrometry;

namespace NINATest {

    [TestFixture]
    public class XISFTest {

        #region "XISF"

        [Test]
        public void XISFConstructorTest() {
            var header = new XISFHeader();

            var sut = new XISF(header);

            sut.Header.Should().Equals(header);
            sut.PaddedBlockSize.Should().Be(4096);
        }

        [Test]
        public void XISFAddAttachedImageNoImageTest() {
            var header = new XISFHeader();
            var sut = new XISF(header);
            Action act = () => sut.AddAttachedImage(new ushort[] { }, "");
            act.Should().Throw<InvalidOperationException>().WithMessage("No Image Header Information available for attaching image. Add Image Header first!");
        }

        [Test]
        public void XISFAddAttachedImageTest() {
            var stats = new Mock<IImageStatistics>();
            stats.SetupGet(x => x.Width).Returns(3);
            stats.SetupGet(x => x.Height).Returns(3);
            var imageType = "LIGHT";
            var data = new ushort[] {
                1,1,1,
                2,3,4,
                1,1,1
            };
            var length = data.Length * sizeof(ushort);

            var header = new XISFHeader();
            header.AddImageMetaData(stats.Object, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, imageType);

            sut.Header.Image.Should().HaveAttribute("location", $"attachment:4096:{length}");

            sut.Data.Data.Should().Equal(data);
        }

        #endregion "XISF"

        #region "XISFHeader"

        [Test]
        public void XISFHeaderConstructorTest() {
            var sut = new XISFHeader();

            XNamespace ns = "http://www.pixinsight.com/xisf";
            XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

            sut.Content
                .Should().HaveRoot(ns + "xisf")
                .Which.Should().HaveAttribute("version", "1.0")
                .And.HaveAttribute("xmlns", "http://www.pixinsight.com/xisf")
                .And.HaveAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance")
                .And.HaveAttribute(xsi + "schemaLocation", "http://www.pixinsight.com/xisf http://pixinsight.com/xisf/xisf-1.0.xsd");

            sut.MetaData.Should().HaveElement("Property")
                .Which.Should().BeOfType<XElement>();

            sut.MetaData.Elements("Property").First(x => x.Attribute("id").Value == "XISF:CreatorApplication")
                .Should().HaveAttribute("type", "String")
                .And.HaveAttribute("comment", "")
                .And.HaveValue("N.I.N.A. - Nighttime Imaging 'N' Astronomy");

            sut.MetaData.Elements("Property").First(x => x.Attribute("id").Value == "XISF:CreationTime")
                .Should().HaveAttribute("type", "TimePoint")
                .And.HaveAttribute("comment", "");

            sut.Image.Should().BeNull();

            sut.ByteCount.Should().Be(481);
        }

        [Test]
        public void XISFHeaderAddImageMetaDataTest() {
            var stats = new Mock<IImageStatistics>();
            stats.SetupGet(x => x.Width).Returns(200);
            stats.SetupGet(x => x.Height).Returns(100);
            var imageType = "TestType";

            var sut = new XISFHeader();
            sut.AddImageMetaData(stats.Object, imageType);

            sut.Image.Should().HaveAttribute("geometry", "200:100:1")
                .And.HaveAttribute("sampleFormat", "UInt16")
                .And.HaveAttribute("imageType", imageType)
                .And.HaveAttribute("colorSpace", "Gray")

                .And.HaveElement("FITSKeyword")
                    .Which.Should().HaveAttribute("name", "IMAGETYP")
                    .And.HaveAttribute("value", imageType)
                    .And.HaveAttribute("comment", "Type of exposure");
        }

        [Test]
        public void XISFHeaderAddMetaDataPropertyTest() {
            var id = "TestId";
            var type = "TestType";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddMetaDataProperty(id, type, value, comment);

            sut.MetaData.Elements("Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveAttribute("value", value);
        }

        [Test]
        public void XISFHeaderAddMetaDataPropertyStringTest() {
            var id = "TestId";
            var type = "String";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddMetaDataProperty(id, type, value, comment);

            sut.MetaData.Elements("Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveValue(value);
        }

        [Test]
        public void XISFHeaderAddMetaDataProperty2Test() {
            var id = "TestId";
            var type = "TestType";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddMetaDataProperty(new string[] { id, type }, value, comment);

            sut.MetaData.Elements("Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveAttribute("value", value);
        }

        [Test]
        public void XISFHeaderAddMetaDataProperty2StringTest() {
            var id = "TestId";
            var type = "String";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddMetaDataProperty(new string[] { id, type }, value, comment);

            sut.MetaData.Elements("Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveValue(value);
        }

        [Test]
        public void XISFHeaderAddImagePropertyNoImageComponentTest() {
            var id = "TestId";
            var type = "String";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            Action act = () => sut.AddImageProperty(new string[] { id, type }, value, comment, true);
            act.Should().Throw<InvalidOperationException>().WithMessage("No Image component available to add property!");
        }

        [Test]
        public void XISFHeaderAddImagePropertyNoFITSTest() {
            var stats = new Mock<IImageStatistics>();
            stats.SetupGet(x => x.Width).Returns(200);
            stats.SetupGet(x => x.Height).Returns(100);
            var imageType = "TestType";

            var id = "TestId";
            var type = "String";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddImageMetaData(stats.Object, imageType);
            sut.AddImageProperty(new string[] { id, type }, value, comment, true);

            sut.Image.Elements("Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveValue(value);

            sut.Image.Elements("FITSKeyword").Where(x => x.Attribute("name").Value != "IMAGETYP").Should().BeEmpty();
        }

        [Test]
        public void XISFHeaderAddImagePropertyNoAutoFITSTest() {
            var stats = new Mock<IImageStatistics>();
            stats.SetupGet(x => x.Width).Returns(200);
            stats.SetupGet(x => x.Height).Returns(100);
            var imageType = "TestType";

            var id = "TestId";
            var type = "String";
            var name = "FITSName";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddImageMetaData(stats.Object, imageType);
            sut.AddImageProperty(new string[] { id, type, name }, value, comment, false);

            sut.Image.Elements("Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveValue(value);

            sut.Image.Elements("FITSKeyword").Where(x => x.Attribute("name").Value != "IMAGETYP").Should().BeEmpty();
        }

        [Test]
        public void XISFHeaderAddImagePropertyAutoFITSTest() {
            var stats = new Mock<IImageStatistics>();
            stats.SetupGet(x => x.Width).Returns(200);
            stats.SetupGet(x => x.Height).Returns(100);
            var imageType = "TestType";

            var id = "TestId";
            var type = "String";
            var name = "FITSName";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddImageMetaData(stats.Object, imageType);
            sut.AddImageProperty(new string[] { id, type, name }, value, comment);

            sut.Image.Elements("Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveValue(value);

            sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == name)
                .Should().HaveAttribute("value", value)
                .And.HaveAttribute("comment", comment);
        }

        [Test]
        public void XISFHeaderAddImageFITSKeywordNoImageTest() {
            var name = "TestName";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            Action act = () => sut.AddImageFITSKeyword(name, value, comment);
            act.Should().Throw<InvalidOperationException>().WithMessage("No Image component available to add FITS Keyword!");
        }

        [Test]
        public void XISFHeaderAddImageFITSKeywordTest() {
            var stats = new Mock<IImageStatistics>();
            stats.SetupGet(x => x.Width).Returns(200);
            stats.SetupGet(x => x.Height).Returns(100);
            var imageType = "TestType";

            var name = "FITSName";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddImageMetaData(stats.Object, imageType);
            sut.AddImageFITSKeyword(name, value, comment);

            sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == name)
                .Should().HaveAttribute("value", value)
                .And.HaveAttribute("comment", comment);
        }

        [Test]
        public void XISFHeaderAddEmbeddedImageTest() {
            var stats = new Mock<IImageStatistics>();
            stats.SetupGet(x => x.Width).Returns(200);
            stats.SetupGet(x => x.Height).Returns(100);

            var array = new Mock<IImageArray>();
            array.SetupGet(x => x.FlatArray).Returns(new ushort[] { 1, 1, 1, 1, 3, 3, 5, 6, 1 });

            var data = new Mock<IImageData>();
            data.SetupGet(x => x.Statistics).Returns(stats.Object);
            data.SetupGet(x => x.Data).Returns(array.Object);

            var imageType = "TestType";

            var sut = new XISFHeader();
            sut.AddEmbeddedImage(data.Object, imageType);

            sut.Image.Should().HaveAttribute("geometry", "200:100:1")
                .And.HaveAttribute("sampleFormat", "UInt16")
                .And.HaveAttribute("imageType", imageType)
                .And.HaveAttribute("colorSpace", "Gray")
                .And.HaveAttribute("location", "embedded")

                .And.HaveElement("Data")
                    .Which.Should().HaveAttribute("encoding", "base64")
                    .And.HaveValue("AQABAAEAAQADAAMABQAGAAEA");

            sut.Image.Should().HaveElement("FITSKeyword")
                    .Which.Should().HaveAttribute("name", "IMAGETYP")
                    .And.HaveAttribute("value", imageType)
                    .And.HaveAttribute("comment", "Type of exposure");
        }

        [Test]
        public void XISFDefaultMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("XBINNING",1, "X axis binning factor"),
                new FITSHeaderCard("YBINNING",1, "Y axis binning factor"),
                new FITSHeaderCard("SWCREATE",string.Format("N.I.N.A. {0} ({1})", Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file"),
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), "LIGHT");
            sut.Populate(metaData);

            //Assert
            sut.Image.Elements("Property").First(x => x.Attribute("id").Value == "Instrument:Camera:XBinning")
                .Should().HaveAttribute("type", "Int32")
                .And.HaveAttribute("comment", "X axis binning factor")
                .And.HaveAttribute("value", "1");

            sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == "XBINNING")
                .Should().HaveAttribute("name", "XBINNING")
                .And.HaveAttribute("value", "1")
                .And.HaveAttribute("comment", "X axis binning factor");

            sut.Image.Elements("Property").First(x => x.Attribute("id").Value == "Instrument:Camera:YBinning")
                .Should().HaveAttribute("type", "Int32")
                .And.HaveAttribute("comment", "Y axis binning factor")
                .And.HaveAttribute("value", "1");

            sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == "YBINNING")
                .Should().HaveAttribute("name", "YBINNING")
                .And.HaveAttribute("value", "1")
                .And.HaveAttribute("comment", "Y axis binning factor");

            sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == "SWCREATE")
                .Should().HaveAttribute("name", "SWCREATE")
                .And.HaveAttribute("value", string.Format("N.I.N.A. {0} ({1})", Utility.Version, DllLoader.IsX86() ? "x86" : "x64"))
                .And.HaveAttribute("comment", "Software that created this file");
        }

        [Test]
        public void XISFImageMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            var now = DateTime.Now;
            metaData.Image.ImageType = "TEST";
            metaData.Image.ExposureStart = now;
            metaData.Image.ExposureTime = 10.23;

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("IMAGETYP", metaData.Image.ImageType, "Type of exposure"),
                new FITSHeaderCard("EXPOSURE", metaData.Image.ExposureTime, "[s] Exposure duration"),
                new FITSHeaderCard("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime(), "Time of observation (local)"),
                new FITSHeaderCard("DATE-OBS", metaData.Image.ExposureStart.ToUniversalTime(), "Time of observation (UTC)"),
            };

            var expectedProperties = new[] {
                new { Id = "Instrument:ExposureTime", Type = "Float32", Value = $"{metaData.Image.ExposureTime.ToString(CultureInfo.InvariantCulture)}", Comment = "[s] Exposure duration"},
                new { Id = "Observation:Time:Start", Type = "TimePoint", Value = $"{metaData.Image.ExposureStart.ToUniversalTime().ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture)}", Comment = "Time of observation (UTC)"}
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        [Test]
        public void XISFCameraMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            metaData.Camera.Name = "TEST";

            metaData.Camera.BinX = 2;
            metaData.Camera.BinY = 3;
            metaData.Camera.Gain = 200;
            metaData.Camera.Offset = 22;
            metaData.Camera.ElectronsPerADU = 11;
            metaData.Camera.PixelSize = 12;
            metaData.Camera.SetPoint = -5;
            metaData.Camera.Temperature = -4.454;

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("INSTRUME", metaData.Camera.Name, "Imaging instrument name"),
                new FITSHeaderCard("XBINNING", metaData.Camera.BinX, "X axis binning factor"),
                new FITSHeaderCard("YBINNING", metaData.Camera.BinY, "Y axis binning factor"),
                new FITSHeaderCard("GAIN", metaData.Camera.Gain, "Sensor gain"),
                new FITSHeaderCard("OFFSET", metaData.Camera.Offset, "Sensor gain offset"),
                new FITSHeaderCard("EGAIN", metaData.Camera.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit"),
                new FITSHeaderCard("XPIXSZ", metaData.Camera.PixelSize, "[um] Pixel X axis size"),
                new FITSHeaderCard("YPIXSZ", metaData.Camera.PixelSize, "[um] Pixel Y axis size"),
                new FITSHeaderCard("SET-TEMP", metaData.Camera.SetPoint, "[C] CCD temperature setpoint"),
                new FITSHeaderCard("CCD-TEMP", metaData.Camera.Temperature, "[C] CCD temperature"),
            };

            var expectedProperties = new[] {
                new { Id = "Instrument:Camera:Name", Type = "String", Value = metaData.Camera.Name, Comment = "Imaging instrument name"},
                new { Id = "Instrument:Camera:Gain", Type = "Float32", Value = $"{metaData.Camera.ElectronsPerADU.ToString(CultureInfo.InvariantCulture)}", Comment = "[e-/ADU] Electrons per A/D unit"},
                new { Id = "Instrument:Camera:XBinning", Type = "Int32", Value = $"{metaData.Camera.BinX.ToString(CultureInfo.InvariantCulture)}", Comment = "X axis binning factor"},
                new { Id = "Instrument:Camera:YBinning", Type = "Int32", Value = $"{metaData.Camera.BinY.ToString(CultureInfo.InvariantCulture)}", Comment = "Y axis binning factor"},
                new { Id = "Instrument:Sensor:Temperature", Type = "Float32", Value = $"{metaData.Camera.Temperature.ToString(CultureInfo.InvariantCulture)}", Comment = "[C] CCD temperature"},
                new { Id = "Instrument:Sensor:XPixelSize", Type = "Float32", Value = $"{metaData.Camera.PixelSize.ToString(CultureInfo.InvariantCulture)}", Comment = "[um] Pixel X axis size"},
                new { Id = "Instrument:Sensor:YPixelSize", Type = "Float32", Value = $"{metaData.Camera.PixelSize.ToString(CultureInfo.InvariantCulture)}", Comment = "[um] Pixel Y axis size"}
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        [Test]
        public void XISFObserverMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            metaData.Observer.Latitude = 10;
            metaData.Observer.Longitude = 20;
            metaData.Observer.Elevation = 30;

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("SITEELEV", metaData.Observer.Elevation, "[m] Observation site elevation"),
                new FITSHeaderCard("SITELAT", metaData.Observer.Latitude, "[deg] Observation site latitude"),
                new FITSHeaderCard("SITELONG", metaData.Observer.Longitude, "[deg] Observation site longitude")
            };

            var expectedProperties = new[] {
                new { Id = "Observation:Location:Latitude", Type = "Float64", Value = metaData.Observer.Latitude.ToString(CultureInfo.InvariantCulture), Comment = "[deg] Observation site latitude"},
                new { Id = "Observation:Location:Longitude", Type = "Float64", Value = metaData.Observer.Longitude.ToString(CultureInfo.InvariantCulture), Comment = "[deg] Observation site longitude"},
                new { Id = "Observation:Location:Elevation", Type = "Float64", Value = metaData.Observer.Elevation.ToString(CultureInfo.InvariantCulture), Comment = "[m] Observation site elevation"},
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        [Test]
        public void XISFTelescopeMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            metaData.Telescope.Name = "TEST";
            metaData.Telescope.FocalLength = 200;
            metaData.Telescope.FocalRatio = 5;
            metaData.Telescope.Coordinates = new NINA.Utility.Astrometry.Coordinates(Angle.ByHours(2.125), Angle.ByDegree(10.154), Epoch.J2000);

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("TELESCOP", metaData.Telescope.Name, "Name of telescope"),
                new FITSHeaderCard("FOCALLEN", metaData.Telescope.FocalLength, "[mm] Focal length"),
                new FITSHeaderCard("FOCRATIO", metaData.Telescope.FocalRatio, "Focal ratio"),
                new FITSHeaderCard("RA", metaData.Telescope.Coordinates.RADegrees, "[deg] RA of telescope"),
                new FITSHeaderCard("DEC", metaData.Telescope.Coordinates.Dec, "[deg] Declination of telescope")
            };

            var expectedProperties = new[] {
                new { Id = "Instrument:Telescope:Name", Type = "String", Value = metaData.Telescope.Name, Comment = "Name of telescope"},
                new { Id = "Instrument:Telescope:FocalLength", Type = "Float32", Value = metaData.Telescope.FocalLength.ToString(CultureInfo.InvariantCulture), Comment = "[mm] Focal length"},
                new { Id = "Instrument:Telescope:Aperture", Type = "Float32", Value = (metaData.Telescope.FocalLength / metaData.Telescope.FocalRatio).ToString(CultureInfo.InvariantCulture), Comment = "[mm] Aperture"},
                new { Id = "Observation:Center:RA", Type = "Float64", Value = metaData.Telescope.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), Comment = "[deg] RA of telescope"},
                new { Id = "Observation:Center:Dec", Type = "Float64", Value = metaData.Telescope.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), Comment = "[deg] Declination of telescope"},
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        [Test]
        public void XISFFilterMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            metaData.FilterWheel.Name = "TEST";
            metaData.FilterWheel.Filter = "FILTERTEST";

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("FWHEEL", metaData.FilterWheel.Name, "Filter Wheel name"),
                new FITSHeaderCard("FILTER", metaData.FilterWheel.Filter, "Active filter name")
            };

            var expectedProperties = new[] {
                new { Id = "Instrument:Filter:Name", Type = "String", Value = metaData.FilterWheel.Filter, Comment = "Active filter name"}
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        [Test]
        public void XISFTargetMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            metaData.Target.Name = "TEST";
            metaData.Target.Coordinates = new NINA.Utility.Astrometry.Coordinates(Angle.ByHours(2.125), Angle.ByDegree(10.154), Epoch.J2000);

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("OBJECT", metaData.Target.Name, "Name of the object of interest"),
                new FITSHeaderCard("OBJCTRA", Astrometry.HoursToFitsHMS(metaData.Target.Coordinates.RA), "[H M S] RA of imaged object"),
                new FITSHeaderCard("OBJCTDEC", Astrometry.DegreesToFitsDMS(metaData.Target.Coordinates.Dec), "[D M S] Declination of imaged object"),
            };

            var expectedProperties = new[] {
                new { Id = "Observation:Object:Name", Type = "String", Value = metaData.Target.Name, Comment = "Name of the object of interest"},
                new { Id = "Observation:Object:RA", Type = "Float64", Value = metaData.Target.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), Comment = "[deg] RA of imaged object"},
                new { Id = "Observation:Object:Dec", Type = "Float64", Value = metaData.Target.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), Comment = "[deg] Declination of imaged object"},
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        [Test]
        public void XISFFocuserMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            metaData.Focuser.Name = "TEST";
            metaData.Focuser.Position = 123.11;
            metaData.Focuser.StepSize = 10.23;
            metaData.Focuser.Temperature = 125.12;

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("FOCNAME", metaData.Focuser.Name, "Focusing equipment name"),
                new FITSHeaderCard("FOCPOS", metaData.Focuser.Position, "[step] Focuser position"),
                new FITSHeaderCard("FOCUSPOS", metaData.Focuser.Position, "[step] Focuser position"),
                new FITSHeaderCard("FOCUSSZ", metaData.Focuser.StepSize, "[um] Focuser step size"),
                new FITSHeaderCard("FOCTEMP", metaData.Focuser.Temperature, "[C] Focuser temperature"),
                new FITSHeaderCard("FOCUSTEM", metaData.Focuser.Temperature, "[C] Focuser temperature"),
            };

            float expectedFocusDistance = (float)((metaData.Focuser.Position * metaData.Focuser.StepSize) / 1000.0);
            var expectedProperties = new[] {
                new { Id = "Instrument:Focuser:Position", Type = "Float32", Value = expectedFocusDistance.ToString(CultureInfo.InvariantCulture), Comment = ""}
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements("Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        [Test]
        public void XISFRotatorMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            metaData.Rotator.Name = "TEST";
            metaData.Rotator.Position = 123.11;
            metaData.Rotator.StepSize = 10.23;

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("ROTNAME", metaData.Rotator.Name, "Rotator equipment name"),
                new FITSHeaderCard("ROTATOR", metaData.Rotator.Position, "[deg] Rotator angle"),
                new FITSHeaderCard("ROTATANG", metaData.Rotator.Position, "[deg] Rotator angle"),
                new FITSHeaderCard("ROTSTPSZ", metaData.Rotator.StepSize, "[deg] Rotator step size"),
            };

            float expectedFocusDistance = (float)((metaData.Focuser.Position * metaData.Focuser.StepSize) / 1000.0);

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageStatistics(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements("FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        #endregion "XISFHeader"
    }
}