#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Serialization;
using NUnit.Framework;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace NINA.Test.Sequencer.Serialization {
    [TestFixture, NonParallelizable]
    public class SequenceJsonFileTest {
        [TestCase(false)]
        [TestCase(true)]
        public void DeserializeFromFile_PreservesEncodingAndSourceContext(bool utf16) {
            var converter = CreateConverter();
            string json = converter.Serialize(new SourceAwareContainer { Name = "Target \u03b1 \u661f", IsExpanded = false });
            WithFile(json, utf16 ? Encoding.Unicode : new UTF8Encoding(true), path => {
                var result = converter.DeserializeFromFile(path).Should().BeOfType<SourceAwareContainer>().Subject;

                result.Name.Should().Be("Target \u03b1 \u661f");
                result.IsExpanded.Should().BeFalse();
                result.SourcePath.Should().Be(path);
            });
        }

        [Test]
        public void DeserializeFromFile_RejectsAdditionalContentAndReleasesFile() {
            var converter = CreateConverter();
            string json = converter.Serialize(new SourceAwareContainer()) + " {}";
            WithFile(json, Encoding.UTF8, path => {
                Action load = () => converter.DeserializeFromFile(path);

                load.Should().Throw<JsonException>();
                using var reopened = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                reopened.Length.Should().BeGreaterThan(0);
            });
        }

        [Test]
        public void DeserializeFromFile_HonorsDefaultConvertersLikeStringDeserialization() {
            var converter = CreateConverter();
            string json = converter.Serialize(new SourceAwareContainer { Name = "target" });
            var previous = JsonConvert.DefaultSettings;
            try {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
                    Converters = { new PrefixStringConverter() }
                };
                WithFile(json, Encoding.UTF8, path => {
                    var result = converter.DeserializeFromFile(path);

                    result.Name.Should().Be("settings:target");
                    result.Name.Should().Be(converter.Deserialize(json).Name);
                });
            } finally {
                JsonConvert.DefaultSettings = previous;
            }
        }

        [Test]
        public void DeserializeFromFile_PreservesExplicitlyPermissiveDefaultSettings() {
            var converter = CreateConverter();
            string json = converter.Serialize(new SourceAwareContainer { Name = "target" }) + " {}";
            var previous = JsonConvert.DefaultSettings;
            int settingsCalls = 0;
            try {
                JsonConvert.DefaultSettings = () => {
                    settingsCalls++;
                    return new JsonSerializerSettings { CheckAdditionalContent = false };
                };
                WithFile(json, Encoding.UTF8, path => {
                    var result = converter.DeserializeFromFile(path);

                    result.Name.Should().Be("target");
                    settingsCalls.Should().Be(1);
                    result.Name.Should().Be(converter.Deserialize(json).Name);
                });
            } finally { JsonConvert.DefaultSettings = previous; }
        }

        private static SequenceJsonConverter CreateConverter() {
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SourceAwareContainer>()).Returns(() => new SourceAwareContainer());
            return new SequenceJsonConverter(factory.Object);
        }

        private static void WithFile(string json, Encoding encoding, Action<string> test) {
            string path = Path.Combine(Path.GetTempPath(), "NINA-sequence-reader-" + Guid.NewGuid().ToString("N") + ".json");
            try {
                File.WriteAllText(path, json, encoding);
                test(path);
            } finally { File.Delete(path); }
        }

        public sealed class SourceAwareContainer : SequentialContainer {
            [JsonIgnore]
            public string? SourcePath { get; private set; }

            [OnDeserialized]
            private void ObserveSourcePath(StreamingContext context) {
                SourcePath = context.Context as string;
            }
        }

        private sealed class PrefixStringConverter : JsonConverter<string> {
            public override bool CanWrite => false;

            public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer) {
                return reader.Value is string value ? "settings:" + value : null;
            }

            public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer) => throw new NotSupportedException();
        }
    }
}
