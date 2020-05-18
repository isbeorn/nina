#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Moq;
using NINA.Model.ImageData;
using System.Globalization;
using NINA.Utility.Astrometry;
using System.IO;
using System.Threading.Tasks;
using NINA.Utility.FileFormat.FITS;
using NINA.Utility.FileFormat.XISF;

namespace NINATest {
    [TestFixture]
    public class XISFTest {

        #region "XISF"

        [Test]
        public void XISFConstructorTest() {
            var header = new XISFHeader();

            var sut = new XISF(header);

            sut.Header.Should().Equals(header);
            sut.PaddedBlockSize.Should().Be(1024);
        }

        [Test]
        public void XISFAddAttachedImageNoImageTest() {
            var header = new XISFHeader();
            var sut = new XISF(header);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = "TestFile",
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF
            };

            Action act = () => sut.AddAttachedImage(new ushort[] { }, fileSaveInfo);
            act.Should().Throw<InvalidOperationException>().WithMessage("No Image Header Information available for attaching image. Add Image Header first!");
        }

        [Test]
        public void XISFAddAttachedImageTest() {
            var props = new ImageProperties(width: 3, height: 3, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            var data = new ushort[] {
                1,1,1,
                2,3,4,
                1,1,1
            };
            var length = data.Length * sizeof(ushort);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = "TestFile",
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF
            };

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("location", $"attachment:{sut.PaddedBlockSize}:{length}");

            ushort[] outarray = new ushort[sut.Data.Data.Length / 2];
            Buffer.BlockCopy(sut.Data.Data, 0, outarray, 0, sut.Data.Data.Length);
            outarray.Should().Equal(data);
        }

        [Test]
        [TestCase("000000000000000000000000", "7168")]
        [TestCase("00000000000000000000000", "6144")]
        public async Task XISFAddAttachedImage_Special_Test(string value, string expectedAttachmentLocation) {
            var props = new ImageProperties(width: 3, height: 3, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            var data = new ushort[] {
                1,1,1,
                2,3,4,
                1,1,1
            };
            var length = data.Length * sizeof(ushort);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = "TestFile",
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF
            };

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            for (int i = 0; i < 50; i++) {
                header.AddImageFITSKeyword("test", "00000000000000000000000000000000000000000000000000");
            }
            header.AddImageFITSKeyword("t", value);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            var file = Path.Combine(TestContext.CurrentContext.TestDirectory, "test.xisf");

            using (var s = new System.IO.FileStream(file, System.IO.FileMode.Create)) {
                sut.Save(s);
            }

            var x = await XISF.Load(new Uri(file), false);

            sut.Header.Image.Attribute("location").Value.Split(':')[1].Should().Be(expectedAttachmentLocation);
            x.Data.FlatArray.Should().BeEquivalentTo(data);
            File.Delete(file);
        }

        [Test]
        public void XISFCompressLZ4Test() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            var length = data.Length * sizeof(ushort);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFCompressionType = NINA.Utility.Enum.XISFCompressionTypeEnum.LZ4
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("compression", $"lz4:{length}");
            sut.Header.Image.Should().HaveAttribute("location", $"attachment:{sut.PaddedBlockSize}:{sut.Data.Data.Length}");
        }

        [Test]
        public void XISFCompressLZ4ShuffledTest() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            var length = data.Length * sizeof(ushort);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFCompressionType = NINA.Utility.Enum.XISFCompressionTypeEnum.LZ4,
                XISFByteShuffling = true
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("compression", $"lz4+sh:{length}:{sizeof(ushort)}");
            sut.Header.Image.Should().HaveAttribute("location", $"attachment:{sut.PaddedBlockSize}:{sut.Data.Data.Length}");
        }

        [Test]
        public void XISFCompressLZ4HCTest() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            var length = data.Length * sizeof(ushort);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFCompressionType = NINA.Utility.Enum.XISFCompressionTypeEnum.LZ4HC
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("compression", $"lz4hc:{length}");
            sut.Header.Image.Should().HaveAttribute("location", $"attachment:{sut.PaddedBlockSize}:{sut.Data.Data.Length}");
        }

        [Test]
        public void XISFCompressLZ4HCShuffledTest() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            var length = data.Length * sizeof(ushort);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFCompressionType = NINA.Utility.Enum.XISFCompressionTypeEnum.LZ4HC,
                XISFByteShuffling = true
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("compression", $"lz4hc+sh:{length}:{sizeof(ushort)}");
            sut.Header.Image.Should().HaveAttribute("location", $"attachment:{sut.PaddedBlockSize}:{sut.Data.Data.Length}");
        }

        [Test]
        public void XISFCompressZLibTest() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            var length = data.Length * sizeof(ushort);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFCompressionType = NINA.Utility.Enum.XISFCompressionTypeEnum.ZLIB
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("compression", $"zlib:{length}");
            sut.Header.Image.Should().HaveAttribute("location", $"attachment:{sut.PaddedBlockSize}:{sut.Data.Data.Length}");
        }

        [Test]
        public void XISFCompressZLibShuffledTest() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            var length = data.Length * sizeof(ushort);

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFCompressionType = NINA.Utility.Enum.XISFCompressionTypeEnum.ZLIB,
                XISFByteShuffling = true
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("compression", $"zlib+sh:{length}:{sizeof(ushort)}");
            sut.Header.Image.Should().HaveAttribute("location", $"attachment:{sut.PaddedBlockSize}:{sut.Data.Data.Length}");
        }

        [Test]
        public void XISFChecksumSHA1Test() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            string cksum = "ca711c69165e1fa5be72993b9a7870ef6d485249";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFChecksumType = NINA.Utility.Enum.XISFChecksumTypeEnum.SHA1
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("checksum", $"sha-1:{cksum}");
        }

        [Test]
        public void XISFChecksumSHA256Test() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            string cksum = "2d864c0b789a43214eee8524d3182075125e5ca2cd527f3582ec87ffd94076bc";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFChecksumType = NINA.Utility.Enum.XISFChecksumTypeEnum.SHA256
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("checksum", $"sha-256:{cksum}");
        }

        [Test]
        public void XISFChecksumSHA512Test() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            string cksum = "b0dbd95e5dbe70819049ae5f10340a2c29fa630ac3afd6b3cbf97865cea418dbecf718ea6e15a596c7e8a40b9372b85ac82f602092438570247afc418650db0b";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFChecksumType = NINA.Utility.Enum.XISFChecksumTypeEnum.SHA512
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("checksum", $"sha-512:{cksum}");
        }

        [Test]
        public void XISFChecksumSHA3_256Test() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            string cksum = "1454fca9a69b7c15209d52a7474b3b80cfc4b80c5e1720d24c13a24d9d832c0e";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFChecksumType = NINA.Utility.Enum.XISFChecksumTypeEnum.SHA3_256
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("checksum", $"sha3-256:{cksum}");
        }

        [Test]
        public void XISFChecksumSHA3_512Test() {
            int imgsize = 128;
            var props = new ImageProperties(width: imgsize, height: imgsize, bitDepth: 16, isBayered: false);
            var imageType = "LIGHT";
            ushort[] data = new ushort[imgsize * imgsize];
            string cksum = "9934ce6c44048d54302b025f71ddbb44ad49da730600b60821798892c1f51b19a91b0dc9c578ed4baa4b9e7506e966100532f9b70e264aaef6ee76eda074ab57";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = string.Empty,
                FilePattern = string.Empty,
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF,
                XISFChecksumType = NINA.Utility.Enum.XISFChecksumTypeEnum.SHA3_512
            };

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = ushort.MaxValue;
            }

            var header = new XISFHeader();
            header.AddImageMetaData(props, imageType);
            var sut = new XISF(header);
            sut.AddAttachedImage(data, fileSaveInfo);

            sut.Header.Image.Should().HaveAttribute("checksum", $"sha3-512:{cksum}");
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

            sut.MetaData.Should().HaveElement(ns + "Property")
                .Which.Should().BeOfType<XElement>();

            sut.MetaData.Elements(ns + "Property").First(x => x.Attribute("id").Value == "XISF:CreatorApplication")
                .Should().HaveAttribute("type", "String")
                .And.HaveAttribute("comment", "")
                .And.HaveValue("N.I.N.A. - Nighttime Imaging 'N' Astronomy");

            sut.MetaData.Elements(ns + "Property").First(x => x.Attribute("id").Value == "XISF:CreationTime")
                .Should().HaveAttribute("type", "TimePoint")
                .And.HaveAttribute("comment", "");

            sut.Image.Should().BeNull();

            sut.ByteCount.Should().Be(472);
        }

        [Test]
        public void XISFHeaderAddImageMetaDataTest() {
            var props = new ImageProperties(width: 200, height: 100, bitDepth: 16, isBayered: false);
            var imageType = "TestType";
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var sut = new XISFHeader();
            sut.AddImageMetaData(props, imageType);

            sut.Image.Should().HaveAttribute("geometry", "200:100:1")
                .And.HaveAttribute("sampleFormat", "UInt16")
                .And.HaveAttribute("imageType", imageType)
                .And.HaveAttribute("colorSpace", "Gray")

                .And.HaveElement(ns + "FITSKeyword")
                    .Which.Should().HaveAttribute("name", "IMAGETYP")
                    .And.HaveAttribute("value", imageType)
                    .And.HaveAttribute("comment", "Type of exposure");
        }

        [Test]
        public void XISFHeaderAddImageMetaDataSNAPTest() {
            var props = new ImageProperties(width: 200, height: 100, bitDepth: 16, isBayered: false);
            var imageType = "SNAPSHOT";
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var sut = new XISFHeader();
            sut.AddImageMetaData(props, imageType);

            sut.Image.Should().HaveAttribute("geometry", "200:100:1")
                .And.HaveAttribute("sampleFormat", "UInt16")
                .And.HaveAttribute("imageType", "LIGHT")
                .And.HaveAttribute("colorSpace", "Gray")

                .And.HaveElement(ns + "FITSKeyword")
                    .Which.Should().HaveAttribute("name", "IMAGETYP")
                    .And.HaveAttribute("value", "LIGHT")
                    .And.HaveAttribute("comment", "Type of exposure");
        }

        [Test]
        public void XISFHeaderAddMetaDataPropertyTest() {
            var id = "TestId";
            var type = "TestType";
            var value = "TestValue";
            var comment = "TestComment";
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var sut = new XISFHeader();
            sut.AddMetaDataProperty(id, type, value, comment);

            sut.MetaData.Elements(ns + "Property").First(x => x.Attribute("id").Value == id)
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
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var sut = new XISFHeader();
            sut.AddMetaDataProperty(id, type, value, comment);

            sut.MetaData.Elements(ns + "Property").First(x => x.Attribute("id").Value == id)
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
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var sut = new XISFHeader();
            sut.AddMetaDataProperty(new string[] { id, type }, value, comment);

            sut.MetaData.Elements(ns + "Property").First(x => x.Attribute("id").Value == id)
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
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var sut = new XISFHeader();
            sut.AddMetaDataProperty(new string[] { id, type }, value, comment);

            sut.MetaData.Elements(ns + "Property").First(x => x.Attribute("id").Value == id)
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
            var props = new ImageProperties(width: 200, height: 100, bitDepth: 16, isBayered: false);
            var imageType = "TestType";
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var id = "TestId";
            var type = "String";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddImageMetaData(props, imageType);
            sut.AddImageProperty(new string[] { id, type }, value, comment, true);

            sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveValue(value);

            sut.Image.Elements(ns + "FITSKeyword").Where(x => x.Attribute("name").Value != "IMAGETYP").Should().BeEmpty();
        }

        [Test]
        public void XISFHeaderAddImagePropertyNoAutoFITSTest() {
            var props = new ImageProperties(width: 200, height: 100, bitDepth: 16, isBayered: false);
            var imageType = "TestType";
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var id = "TestId";
            var type = "String";
            var name = "FITSName";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddImageMetaData(props, imageType);
            sut.AddImageProperty(new string[] { id, type, name }, value, comment, false);

            sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveValue(value);

            sut.Image.Elements(ns + "FITSKeyword").Where(x => x.Attribute("name").Value != "IMAGETYP").Should().BeEmpty();
        }

        [Test]
        public void XISFHeaderAddImagePropertyAutoFITSTest() {
            var props = new ImageProperties(width: 200, height: 100, bitDepth: 16, isBayered: false);
            var imageType = "TestType";
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var id = "TestId";
            var type = "String";
            var name = "FITSName";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddImageMetaData(props, imageType);
            sut.AddImageProperty(new string[] { id, type, name }, value, comment);

            sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == id)
                .Should().HaveAttribute("type", type)
                .And.HaveAttribute("comment", comment)
                .And.HaveValue(value);

            sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == name)
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
            var props = new ImageProperties(width: 200, height: 100, bitDepth: 16, isBayered: false);
            var imageType = "TestType";
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var name = "FITSName";
            var value = "TestValue";
            var comment = "TestComment";

            var sut = new XISFHeader();
            sut.AddImageMetaData(props, imageType);
            sut.AddImageFITSKeyword(name, value, comment);

            sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == name)
                .Should().HaveAttribute("value", value)
                .And.HaveAttribute("comment", comment);
        }

        [Test]
        public void XISFDefaultMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            XNamespace ns = "http://www.pixinsight.com/xisf";

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("XBINNING",1, "X axis binning factor"),
                new FITSHeaderCard("YBINNING",1, "Y axis binning factor"),
                new FITSHeaderCard("SWCREATE",string.Format("N.I.N.A. {0} ({1})", Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file"),
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), "LIGHT");
            sut.Populate(metaData);

            //Assert
            sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == "Instrument:Camera:XBinning")
                .Should().HaveAttribute("type", "Int32")
                .And.HaveAttribute("comment", "X axis binning factor")
                .And.HaveAttribute("value", "1");

            sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == "XBINNING")
                .Should().HaveAttribute("name", "XBINNING")
                .And.HaveAttribute("value", "1")
                .And.HaveAttribute("comment", "X axis binning factor");

            sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == "Instrument:Camera:YBinning")
                .Should().HaveAttribute("type", "Int32")
                .And.HaveAttribute("comment", "Y axis binning factor")
                .And.HaveAttribute("value", "1");

            sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == "YBINNING")
                .Should().HaveAttribute("name", "YBINNING")
                .And.HaveAttribute("value", "1")
                .And.HaveAttribute("comment", "Y axis binning factor");

            sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == "SWCREATE")
                .Should().HaveAttribute("name", "SWCREATE")
                .And.HaveAttribute("value", string.Format("N.I.N.A. {0} ({1})", Utility.Version, DllLoader.IsX86() ? "x86" : "x64"))
                .And.HaveAttribute("comment", "Software that created this file");
        }

        [Test]
        public void XISFImageMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();
            var now = DateTime.Now;
            XNamespace ns = "http://www.pixinsight.com/xisf";
            metaData.Image.ImageType = "TEST";
            metaData.Image.ExposureStart = now;
            metaData.Image.ExposureTime = 10.23;

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("IMAGETYP", metaData.Image.ImageType, "Type of exposure"),
                new FITSHeaderCard("EXPOSURE", metaData.Image.ExposureTime, "[s] Exposure duration"),
                new FITSHeaderCard("EXPTIME", metaData.Image.ExposureTime, "[s] Exposure duration"),
                new FITSHeaderCard("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime(), "Time of observation (local)"),
                new FITSHeaderCard("DATE-OBS", metaData.Image.ExposureStart.ToUniversalTime(), "Time of observation (UTC)"),
            };

            var expectedProperties = new[] {
                new { Id = "Instrument:ExposureTime", Type = "Float32", Value = $"{metaData.Image.ExposureTime.ToString(CultureInfo.InvariantCulture)}", Comment = "[s] Exposure duration"},
                new { Id = "Observation:Time:Start", Type = "TimePoint", Value = $"{metaData.Image.ExposureStart.ToUniversalTime().ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture)}", Comment = "Time of observation (UTC)"}
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
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
            XNamespace ns = "http://www.pixinsight.com/xisf";

            metaData.Camera.BinX = 2;
            metaData.Camera.BinY = 3;
            metaData.Camera.Gain = 200;
            metaData.Camera.Offset = 22;
            metaData.Camera.ElectronsPerADU = 11;
            metaData.Camera.PixelSize = 12;
            metaData.Camera.SetPoint = -5;
            metaData.Camera.Temperature = -4.454;
            metaData.Camera.ReadoutModeName = "1 Hz";

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("INSTRUME", metaData.Camera.Name, "Imaging instrument name"),
                new FITSHeaderCard("XBINNING", metaData.Camera.BinX, "X axis binning factor"),
                new FITSHeaderCard("YBINNING", metaData.Camera.BinY, "Y axis binning factor"),
                new FITSHeaderCard("GAIN", metaData.Camera.Gain, "Sensor gain"),
                new FITSHeaderCard("OFFSET", metaData.Camera.Offset, "Sensor gain offset"),
                new FITSHeaderCard("EGAIN", metaData.Camera.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit"),
                new FITSHeaderCard("XPIXSZ", metaData.Camera.PixelSize * metaData.Camera.BinX, "[um] Pixel X axis size"),
                new FITSHeaderCard("YPIXSZ", metaData.Camera.PixelSize * metaData.Camera.BinY, "[um] Pixel Y axis size"),
                new FITSHeaderCard("SET-TEMP", metaData.Camera.SetPoint, "[degC] CCD temperature setpoint"),
                new FITSHeaderCard("CCD-TEMP", metaData.Camera.Temperature, "[degC] CCD temperature"),
                new FITSHeaderCard("READOUTM", metaData.Camera.ReadoutModeName, "Sensor readout mode")
            };

            var expectedProperties = new[] {
                new { Id = "Instrument:Camera:Name", Type = "String", Value = metaData.Camera.Name, Comment = "Imaging instrument name"},
                new { Id = "Instrument:Camera:Gain", Type = "Float32", Value = $"{metaData.Camera.ElectronsPerADU.ToString(CultureInfo.InvariantCulture)}", Comment = "[e-/ADU] Electrons per A/D unit"},
                new { Id = "Instrument:Camera:XBinning", Type = "Int32", Value = $"{metaData.Camera.BinX.ToString(CultureInfo.InvariantCulture)}", Comment = "X axis binning factor"},
                new { Id = "Instrument:Camera:YBinning", Type = "Int32", Value = $"{metaData.Camera.BinY.ToString(CultureInfo.InvariantCulture)}", Comment = "Y axis binning factor"},
                new { Id = "Instrument:Sensor:Temperature", Type = "Float32", Value = $"{metaData.Camera.Temperature.ToString(CultureInfo.InvariantCulture)}", Comment = "[degC] CCD temperature"},
                new { Id = "Instrument:Sensor:XPixelSize", Type = "Float32", Value = $"{(metaData.Camera.PixelSize * metaData.Camera.BinX).ToString(CultureInfo.InvariantCulture)}", Comment = "[um] Pixel X axis size"},
                new { Id = "Instrument:Sensor:YPixelSize", Type = "Float32", Value = $"{(metaData.Camera.PixelSize* metaData.Camera.BinY).ToString(CultureInfo.InvariantCulture)}", Comment = "[um] Pixel Y axis size"}
            };

            //Act
            var sut = new XISFHeader();
            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
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
            XNamespace ns = "http://www.pixinsight.com/xisf";

            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
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
                new { Id = "Instrument:Telescope:FocalLength", Type = "Float32", Value = (metaData.Telescope.FocalLength / 1000).ToString(CultureInfo.InvariantCulture), Comment = "[m] Focal Length"},
                new { Id = "Instrument:Telescope:Aperture", Type = "Float32", Value = (metaData.Telescope.FocalLength / metaData.Telescope.FocalRatio / 1000).ToString(CultureInfo.InvariantCulture), Comment = "[m] Aperture"},
                new { Id = "Observation:Center:RA", Type = "Float64", Value = metaData.Telescope.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), Comment = "[deg] RA of telescope"},
                new { Id = "Observation:Center:Dec", Type = "Float64", Value = metaData.Telescope.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), Comment = "[deg] Declination of telescope"},
            };

            //Act
            var sut = new XISFHeader();
            XNamespace ns = "http://www.pixinsight.com/xisf";

            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
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
            XNamespace ns = "http://www.pixinsight.com/xisf";

            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
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
            XNamespace ns = "http://www.pixinsight.com/xisf";

            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
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
                new FITSHeaderCard("FOCTEMP", metaData.Focuser.Temperature, "[degC] Focuser temperature"),
                new FITSHeaderCard("FOCUSTEM", metaData.Focuser.Temperature, "[degC] Focuser temperature"),
            };

            float expectedFocusDistance = (float)((metaData.Focuser.Position * metaData.Focuser.StepSize) / 1000.0);
            var expectedProperties = new[] {
                new { Id = "Instrument:Focuser:Position", Type = "Float32", Value = expectedFocusDistance.ToString(CultureInfo.InvariantCulture), Comment = ""}
            };

            //Act
            var sut = new XISFHeader();
            XNamespace ns = "http://www.pixinsight.com/xisf";

            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
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
            XNamespace ns = "http://www.pixinsight.com/xisf";

            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            //Assert

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        [Test]
        public void XISFWeatherDataMetaDataPopulated() {
            var metaData = new ImageMetaData();
            metaData.WeatherData.CloudCover = 99.11;
            metaData.WeatherData.DewPoint = 18.91;
            metaData.WeatherData.Humidity = 46.52;
            metaData.WeatherData.Pressure = 1010.4;
            metaData.WeatherData.SkyBrightness = 43;
            metaData.WeatherData.SkyQuality = 17.84;
            metaData.WeatherData.SkyTemperature = -42;
            metaData.WeatherData.StarFWHM = 2.34;
            metaData.WeatherData.Temperature = 17.2;
            metaData.WeatherData.WindDirection = 284.23;
            metaData.WeatherData.WindGust = 1.76;
            metaData.WeatherData.WindSpeed = 0.54;

            var expectedFITSKeywords = new List<FITSHeaderCard>() {
                new FITSHeaderCard("CLOUDCVR", metaData.WeatherData.CloudCover, "[percent] Cloud cover"),
                new FITSHeaderCard("DEWPOINT", metaData.WeatherData.DewPoint, "[degC] Dew point"),
                new FITSHeaderCard("HUMIDITY", metaData.WeatherData.Humidity, "[percent] Relative humidity"),
                new FITSHeaderCard("PRESSURE", metaData.WeatherData.Pressure, "[hPa] Air pressure"),
                new FITSHeaderCard("SKYBRGHT", metaData.WeatherData.SkyBrightness, "[lux] Sky brightness"),
                new FITSHeaderCard("MPSAS", metaData.WeatherData.SkyQuality, "[mags/arcsec^2] Sky quality"),
                new FITSHeaderCard("SKYTEMP", metaData.WeatherData.SkyTemperature, "[degC] Sky temperature"),
                new FITSHeaderCard("STARFWHM", metaData.WeatherData.StarFWHM, "Star FWHM"),
                new FITSHeaderCard("AMBTEMP", metaData.WeatherData.Temperature, "[degC] Ambient air temperature"),
                new FITSHeaderCard("WINDDIR", metaData.WeatherData.WindDirection, "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W"),
                new FITSHeaderCard("WINDGUST", metaData.WeatherData.WindGust * 3.6, "[kph] Wind gust"),
                new FITSHeaderCard("WINDSPD", metaData.WeatherData.WindSpeed * 3.6, "[kph] Wind speed"),
            };

            var expectedProperties = new[] {
                new { Id = "Observation:Meteorology:AmbientTemperature", Type = "Float32", Value = metaData.WeatherData.Temperature.ToString(CultureInfo.InvariantCulture), Comment = "[degC] Ambient air temperature"},
                new { Id = "Observation:Meteorology:AtmosphericPressure", Type = "Float32", Value = metaData.WeatherData.Pressure.ToString(CultureInfo.InvariantCulture), Comment = "[hPa] Air pressure"},
                new { Id = "Observation:Meteorology:RelativeHumidity", Type = "Float32", Value = metaData.WeatherData.Humidity.ToString(CultureInfo.InvariantCulture), Comment = "[percent] Relative humidity"},
                new { Id = "Observation:Meteorology:WindDirection", Type = "Float32", Value = metaData.WeatherData.WindDirection.ToString(CultureInfo.InvariantCulture), Comment = "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W"},
                new { Id = "Observation:Meteorology:WindGust", Type = "Float32", Value = (metaData.WeatherData.WindGust * 3.6).ToString(CultureInfo.InvariantCulture), Comment = "[kph] Wind gust"},
                new { Id = "Observation:Meteorology:WindSpeed", Type = "Float32", Value = (metaData.WeatherData.WindSpeed * 3.6).ToString(CultureInfo.InvariantCulture), Comment = "[kph] Wind speed"},
            };

            var sut = new XISFHeader();
            XNamespace ns = "http://www.pixinsight.com/xisf";

            sut.AddImageMetaData(new ImageProperties(2, 2, 16, false), metaData.Image.ImageType);
            sut.Populate(metaData);

            foreach (var property in expectedProperties) {
                if (property.Type != "String") {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveAttribute("value", property.Value);
                } else {
                    sut.Image.Elements(ns + "Property").First(x => x.Attribute("id").Value == property.Id)
                        .Should().HaveAttribute("type", property.Type)
                        .And.HaveAttribute("comment", property.Comment)
                        .And.HaveValue(property.Value);
                }
            }

            foreach (var card in expectedFITSKeywords) {
                sut.Image.Elements(ns + "FITSKeyword").First(x => x.Attribute("name").Value == card.Key)
                .Should().HaveAttribute("name", card.Key)
                .And.HaveAttribute("value", card.Value.Replace("'", "").Trim())
                .And.HaveAttribute("comment", card.Comment);
            }
        }

        #endregion "XISFHeader"
    }
}
