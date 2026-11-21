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
using NINA.Profile;
using NUnit.Framework;
using System.IO;
using System.Runtime.Serialization;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class AstrometrySettingsHorizonTest {
        private const string Namespace = "http://schemas.datacontract.org/2004/07/NINA.Profile";

        private static AstrometrySettings Deserialize(string innerXml) {
            var xml = $"<AstrometrySettings xmlns=\"{Namespace}\">{innerXml}</AstrometrySettings>";
            var serializer = new DataContractSerializer(typeof(AstrometrySettings));
            using (var reader = System.Xml.XmlReader.Create(new StringReader(xml))) {
                return (AstrometrySettings)serializer.ReadObject(reader)!;
            }
        }

        private static string CreateHorizonFile(double altitude, double? upper = null) {
            var path = Path.Combine(Path.GetTempPath(), $"nina_test_{System.Guid.NewGuid():N}.hrz");
            var value = altitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var upperValue = upper.HasValue ? " " + upper.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
            File.WriteAllText(path, $"0 {value}{upperValue}\n180 {value}{upperValue}\n360 {value}{upperValue}");
            return path;
        }

        [Test]
        public void ProfileWithoutUpperHorizonSettings_LoadsWithDefaults() {
            var sut = Deserialize("<Latitude>12.5</Latitude>");

            sut.Latitude.Should().Be(12.5);
            sut.SkyWindow.Upper.Should().BeNull();
            sut.SkyWindow.HasLower.Should().BeFalse();
            sut.SkyWindow.HasUpper.Should().BeFalse();
        }

        [Test]
        public void ProfileWithOldLowerHorizonOnly_StillWorksAsBefore() {
            var file = CreateHorizonFile(25);
            try {
                var sut = Deserialize($"<HorizonFilePath>{file}</HorizonFilePath>");

                sut.Horizon.Should().NotBeNull();
                sut.SkyWindow.Upper.Should().BeNull();
                sut.SkyWindow.HasLower.Should().BeTrue();
                sut.SkyWindow.HasUpper.Should().BeFalse();
                sut.SkyWindow.GetLower(90).Should().Be(25);
                sut.SkyWindow.GetUpper(90).Should().Be(90);
            } finally {
                File.Delete(file);
            }
        }

        [Test]
        public void UpperValuesInThirdColumn_AreLoadedFromTheHorizonFile() {
            var file = CreateHorizonFile(10, 70);
            try {
                var sut = Deserialize($"<HorizonFilePath>{file}</HorizonFilePath>");

                sut.SkyWindow.Upper.Should().NotBeNull();
                sut.SkyWindow.HasLower.Should().BeTrue();
                sut.SkyWindow.HasUpper.Should().BeTrue();
                sut.SkyWindow.GetLower(45).Should().Be(10);
                sut.SkyWindow.GetUpper(45).Should().Be(70);
            } finally {
                File.Delete(file);
            }
        }

        [Test]
        public void SerializationRoundTrip_KeepsHorizonFile() {
            var file = CreateHorizonFile(10, 70);
            try {
                var original = Deserialize($"<HorizonFilePath>{file}</HorizonFilePath>");
                var serializer = new DataContractSerializer(typeof(AstrometrySettings));
                using (var ms = new MemoryStream()) {
                    serializer.WriteObject(ms, original);
                    ms.Position = 0;
                    var copy = (AstrometrySettings)serializer.ReadObject(ms)!;

                    copy.HorizonFilePath.Should().Be(file);
                    copy.SkyWindow.Upper.Should().NotBeNull();
                }
            } finally {
                File.Delete(file);
            }
        }

    }
}
