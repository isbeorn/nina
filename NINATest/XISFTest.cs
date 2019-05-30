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

        #endregion "XISFHeader"
    }
}