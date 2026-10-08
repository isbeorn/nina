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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Serialization;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Text;

namespace NINA.Test.Sequencer.Serialization {
    [TestFixture, NonParallelizable]
    public class SequenceFileStreamingTest {
        [Test]
        public void DeserializeFromFile_DoesNotBufferAllTargetsBeforeCreatingTheFirstItem() {
            long started = 0;
            long firstItemAllocation = 0;
            var converter = CreateConverter(() => {
                if (firstItemAllocation == 0) firstItemAllocation = GC.GetAllocatedBytesForCurrentThread() - started;
            });
            string json = converter.Serialize(CreateSequence(4000));
            WithFile(converter.Serialize(CreateSequence(1)), Encoding.UTF8, path => converter.DeserializeFromFile(path));
            converter.Deserialize(converter.Serialize(CreateSequence(1)));

            WithFile(json, Encoding.UTF8, path => {
                firstItemAllocation = 0;
                started = GC.GetAllocatedBytesForCurrentThread();
                var buffered = converter.Deserialize(json);
                long bufferedAllocation = firstItemAllocation;
                buffered.Items.Single().Should().BeOfType<TargetAreaContainer>().Which.Items.Should().HaveCount(4000);

                firstItemAllocation = 0;
                started = GC.GetAllocatedBytesForCurrentThread();
                var streamed = converter.DeserializeFromFile(path);
                long fileAllocation = firstItemAllocation;
                streamed.Items.Single().Should().BeOfType<TargetAreaContainer>().Which.Items.Should().HaveCount(4000);

                TestContext.Progress.WriteLine($"Before first item: string DOM {bufferedAllocation:N0} bytes, file {fileAllocation:N0} bytes");
                fileAllocation.Should().BeLessThan(bufferedAllocation * 2 / 3,
                    "the file reader should validate without retaining a DOM for every target");
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeserializeFromFile_PreservesRootCallbacksReferencesAndEncoding(bool utf16) {
            var converter = CreateConverter();
            var source = CreateSequence(2);
            var area = (TargetAreaContainer)source.Items.Single();
            ((StreamingItem)area.Items[0]).Peer = area.Items[1];
            ((StreamingItem)area.Items[1]).Peer = area.Items[0];
            source.Name = "Sequence \u03b1 \u661f";
            area.Name = "Targets \u03b2 \u661f";
            WithFile(converter.Serialize(source), utf16 ? Encoding.Unicode : new UTF8Encoding(true), path => {
                var result = converter.DeserializeFromFile(path).Should().BeOfType<SequenceRootContainer>().Subject;
                result.Name.Should().Be(source.Name);
                var actualArea = result.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetAreaContainer>().Subject;
                actualArea.Name.Should().Be(area.Name);
                actualArea.Parent.Should().BeSameAs(result);
                actualArea.Items.Should().HaveCount(2);
                actualArea.Items.Should().OnlyContain(item => ReferenceEquals(item.Parent, actualArea));
                var first = actualArea.Items[0].Should().BeOfType<StreamingItem>().Subject;
                var second = actualArea.Items[1].Should().BeOfType<StreamingItem>().Subject;
                first.Peer.Should().BeSameAs(second);
                second.Peer.Should().BeSameAs(first);
                first.OwnerWhenDeserialized.Should().BeSameAs(actualArea);
            });
        }

        [TestCase("duplicate")]
        [TestCase("late-type")]
        [TestCase("late-ref")]
        public void DeserializeFromFile_PreservesNoncanonicalEnvelopeSemantics(string variant) {
            var converter = CreateConverter();
            var document = JObject.Parse(converter.Serialize(CreateSequence(2)));
            string json;
            if (variant == "duplicate") {
                json = document.ToString(Formatting.None);
                json = json.Insert(json.Length - 1, ",\"Name\":\"last name wins\",\"Items\":[]");
            } else if (variant == "late-type") {
                var type = document.Property("$type")!;
                type.Remove();
                document.Add(type);
                json = document.ToString();
            } else {
                document.Add("$ref", "missing");
                json = document.ToString();
            }
            var expected = converter.Deserialize(json);
            WithFile(json, Encoding.UTF8, path => {
                var actual = converter.DeserializeFromFile(path);
                if (expected == null) actual.Should().BeNull();
                else {
                    actual.GetType().Should().Be(expected.GetType());
                    actual.Name.Should().Be(expected.Name);
                    actual.Items.Count.Should().Be(expected.Items.Count);
                }
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeserializeFromFile_PreservesNoncanonicalTargetAreaAndCollectionSemantics(bool duplicateCollectionValues) {
            var converter = CreateConverter();
            var document = JObject.Parse(converter.Serialize(CreateSequence(2)));
            var area = (JObject)document["Items"]!["$values"]![0]!;
            string json;
            if (duplicateCollectionValues) {
                string oldItems = area["Items"]!.ToString(Formatting.None);
                string replacement = oldItems.Insert(oldItems.Length - 1, ",\"$values\":[]");
                json = document.ToString(Formatting.None).Replace(oldItems, replacement);
            } else {
                var type = area.Property("$type")!;
                type.Remove();
                area.Add(type);
                json = document.ToString();
            }
            AssertFileMatchesString(converter, json);
        }

        [Test]
        public void DeserializeFromFile_PreservesDomScalarConversionsInBothEnvelopeTypes() {
            var converter = CreateConverter();
            foreach (bool inArea in new[] { false, true }) {
                foreach ((string name, JToken value) in new[] {
                    ("Name", (JToken)new JValue("2026-10-08T12:34:56.7890000+02:00")),
                    ("name", (JToken)new JValue("2026-10-08T12:34:56.7890000+02:00")),
                    ("Name", (JToken)new JValue(1.0)),
                    ("Attempts", (JToken)new JValue(2.5))
                }) {
                    var document = JObject.Parse(converter.Serialize(CreateSequence(1)));
                    var envelope = inArea ? (JObject)document["Items"]!["$values"]![0]! : document;
                    envelope[name] = value;
                    AssertFileMatchesString(converter, document.ToString());
                }
            }
        }

        [Test]
        public void DeserializeFromFile_DrainsFailedAreaBeforeReadingTheFollowingSibling() {
            var converter = CreateConverter();
            var document = JObject.Parse(converter.Serialize(CreateSequence(1)));
            var areas = (JArray)document["Items"]!["$values"]!;
            var area = (JObject)areas[0]!;
            ((JArray)area["Items"]!["$values"]!).Add(42);
            var following = JObject.Parse(converter.Serialize(CreateSequence(0)));
            var sibling = (JObject)following["Items"]!["$values"]![0]!;
            sibling["$id"] = "following-area";
            sibling["Items"] = new JArray();
            sibling["Conditions"] = new JArray();
            sibling["Triggers"] = new JArray();
            sibling["Name"] = "following area";
            areas.Add(sibling);
            string json = document.ToString();
            WithFile(json, Encoding.UTF8, path => {
                var result = converter.DeserializeFromFile(path);
                result.Items.Should().HaveCount(2);
                result.Items[0].Should().BeOfType<global::NINA.Sequencer.SequenceItem.UnknownSequenceItem>();
                result.Items[1].Should().BeOfType<TargetAreaContainer>().Which.Name.Should().Be("following area");
                result.Items[1].Parent.Should().BeSameAs(result);
                converter.Serialize(result).Should().Be(converter.Serialize(converter.Deserialize(json)));
            });
        }

        private static void AssertFileMatchesString(SequenceJsonConverter converter, string json) {
            string expected = converter.Serialize(converter.Deserialize(json));
            WithFile(json, Encoding.UTF8, path => converter.Serialize(converter.DeserializeFromFile(path)).Should().Be(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Deserialize_PreservesCustomFactorySubtypeJsonIsolation(bool fromFile) {
            int creations = 0;
            var converter = CreateConverter(createArea: () => { creations++; return new SnapshotArea(); });
            var document = JObject.Parse(converter.Serialize(CreateSequence(0)));
            var area = (JObject)document["Items"]!["$values"]![0]!;
            ((JArray)area["Items"]!["$values"]!).Add(new JObject {
                ["$type"] = typeof(LinkedTemplateContainer).AssemblyQualifiedName,
                ["Strategy"] = new JObject { ["$type"] = typeof(global::NINA.Sequencer.Container.ExecutionStrategy.SequentialStrategy).AssemblyQualifiedName },
                ["Items"] = new JArray(new JObject { ["$type"] = typeof(StreamingItem).AssemblyQualifiedName, ["Payload"] = "legacy runtime item" })
            });
            RetainingNameConverter.Snapshot = null;
            try {
                WithFile(document.ToString(), Encoding.UTF8, path => {
                    var result = fromFile ? converter.DeserializeFromFile(path) : converter.Deserialize(document.ToString());
                    var actual = result.Items.Single().Should().BeOfType<SnapshotArea>().Subject;
                    actual.Items.Single().Should().BeOfType<LinkedTemplateContainer>().Which.Items.Should().BeEmpty();
                    creations.Should().Be(1);
                    RetainingNameConverter.Snapshot.Should().NotBeNull();
                    RetainingNameConverter.Snapshot!["$type"]!.Value<string>().Should().Contain(nameof(TargetAreaContainer));
                    var retainedItems = RetainingNameConverter.Snapshot["Items"]!["$values"]![0]!["Items"];
                    retainedItems.Should().NotBeNull("child migrations must not mutate JSON retained by a subtype converter");
                    retainedItems!.Count().Should().Be(1);
                });
            } finally { RetainingNameConverter.Snapshot = null; }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Deserialize_PreservesOrdinaryPluginEntityJsonIsolation(bool fromFile) {
            int creations = 0;
            var converter = CreateConverter(onPlugin: () => creations++);
            var document = JObject.Parse(converter.Serialize(CreateSequence(0)));
            ((JArray)document["Items"]!["$values"]![0]!["Items"]!["$values"]!).Add(new JObject {
                ["$type"] = typeof(SnapshotItem).AssemblyQualifiedName,
                ["Caption"] = "plugin item",
                ["Child"] = new JObject {
                    ["$type"] = typeof(LinkedTemplateContainer).AssemblyQualifiedName,
                    ["Strategy"] = new JObject { ["$type"] = typeof(global::NINA.Sequencer.Container.ExecutionStrategy.SequentialStrategy).AssemblyQualifiedName },
                    ["Items"] = new JArray(new JObject { ["$type"] = typeof(StreamingItem).AssemblyQualifiedName })
                }
            });
            RetainingNameConverter.Snapshot = null;
            try {
                WithFile(document.ToString(), Encoding.UTF8, path => {
                    var result = fromFile ? converter.DeserializeFromFile(path) : converter.Deserialize(document.ToString());
                    var area = result.Items.Single().Should().BeOfType<TargetAreaContainer>().Subject;
                    area.Items.Single().Should().BeOfType<SnapshotItem>().Which.Child
                        .Should().BeOfType<LinkedTemplateContainer>().Which.Items.Should().BeEmpty();
                    creations.Should().Be(1);
                    RetainingNameConverter.Snapshot.Should().NotBeNull();
                    RetainingNameConverter.Snapshot!["$type"]!.Value<string>().Should().Contain(nameof(SnapshotItem));
                    RetainingNameConverter.Snapshot["Child"]!["Items"].Should().NotBeNull();
                    RetainingNameConverter.Snapshot["Child"]!["Items"]!.Count().Should().Be(1);
                });
            } finally { RetainingNameConverter.Snapshot = null; }
        }

        [Test]
        public void DeserializeFromFile_RejectsTruncationBeforeCreatingAnyModel() {
            int created = 0;
            var converter = CreateConverter(onContainer: () => created++);
            string json = converter.Serialize(CreateSequence(2));
            WithFile(json[..^1], Encoding.UTF8, path => {
                Action load = () => converter.DeserializeFromFile(path);
                load.Should().Throw<JsonException>();
                created.Should().Be(0);
                using var reopened = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Deserialize_DefaultPrimitiveConverterKeepsEntityRelativeReaderContext(bool fromFile) {
            var converter = CreateConverter();
            string json = converter.Serialize(CreateSequence(1));
            var previous = JsonConvert.DefaultSettings;
            var paths = new List<string>();
            var roots = new List<string?>();
            int settingsCalls = 0;
            try {
                JsonConvert.DefaultSettings = () => {
                    settingsCalls++;
                    return new JsonSerializerSettings { Converters = { new ObservingStringConverter(paths, roots) } };
                };
                WithFile(json, Encoding.UTF8, path => {
                    var result = fromFile ? converter.DeserializeFromFile(path) : converter.Deserialize(json);
                    result.Items.Single().Should().BeOfType<TargetAreaContainer>();
                    settingsCalls.Should().Be(1);
                    paths.Should().Contain("Name");
                    paths.Should().NotContain(path => path.Contains("Items"));
                    roots.Should().ContainSingle().Which.Should().Be("targets");
                });
            } finally { JsonConvert.DefaultSettings = previous; }
        }

        [TestCase("derived resolver")]
        [TestCase("configured resolver")]
        [TestCase("additional converter")]
        public void ReadJson_CustomSerializerHooksKeepEntityRelativeReaderContext(string hook) {
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => new SequenceRootContainer());
            factory.Setup(x => x.GetContainer<TargetAreaContainer>()).Returns(() => new TargetAreaContainer());
            var containers = new SequenceContainerCreationConverter(factory.Object);
            var roots = new List<string?>();
            string json = CreateConverter().Serialize(CreateSequence(0));

            var settings = new JsonSerializerSettings {
                Converters = { containers, new SequenceItemCreationConverter(factory.Object, containers) }
            };
            if (hook == "derived resolver") settings.ContractResolver = new ObservingResolver(roots);
            else if (hook == "configured resolver") {
                var resolver = new DefaultContractResolver();
                var contract = (JsonObjectContract)resolver.ResolveContract(typeof(TargetAreaContainer));
                contract.Properties["Name"]!.Converter = new ObservingStringConverter(new List<string>(), roots);
                settings.ContractResolver = resolver;
            } else settings.Converters.Add(new ObservingStringConverter(new List<string>(), roots));

            var result = JsonConvert.DeserializeObject<ISequenceContainer>(json, settings);

            result!.Items.Single().Should().BeOfType<TargetAreaContainer>();
            roots.Should().ContainSingle().Which.Should().Be("targets");
        }

        private static SequenceRootContainer CreateSequence(int count) {
            var root = new SequenceRootContainer { Name = "root" };
            var area = new TargetAreaContainer { Name = "targets" };
            root.Items.Add(area);
            area.AttachNewParent(root);
            for (int i = 0; i < count; i++) {
                var item = new StreamingItem { Name = $"item-{i}", Payload = new string('x', 80), Owner = area };
                area.Items.Add(item);
                item.AttachNewParent(area);
            }
            return root;
        }

        private static SequenceJsonConverter CreateConverter(Action? onItem = null, Action? onContainer = null, Func<TargetAreaContainer>? createArea = null, Action? onPlugin = null) {
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => {
                onContainer?.Invoke();
                var root = new SequenceRootContainer();
                root.Items.Add(new TargetAreaContainer { Name = "prototype item must be cleared" });
                return root;
            });
            factory.Setup(x => x.GetContainer<TargetAreaContainer>()).Returns(() => {
                onContainer?.Invoke();
                return createArea?.Invoke() ?? new TargetAreaContainer();
            });
            factory.Setup(x => x.GetContainer<LinkedTemplateContainer>()).Returns(() => new LinkedTemplateContainer());
            factory.Setup(x => x.GetItem<StreamingItem>()).Returns(() => {
                onItem?.Invoke();
                return new StreamingItem();
            });
            factory.Setup(x => x.GetItem<SnapshotItem>()).Returns(() => { onPlugin?.Invoke(); return new SnapshotItem(); });
            return new SequenceJsonConverter(factory.Object);
        }

        private static void WithFile(string json, Encoding encoding, Action<string> test) {
            string path = Path.Combine(Path.GetTempPath(), "NINA-streaming-" + Guid.NewGuid().ToString("N") + ".json");
            try {
                File.WriteAllText(path, json, encoding);
                test(path);
            } finally { File.Delete(path); }
        }

        public sealed class StreamingItem : global::NINA.Sequencer.SequenceItem.SequenceItem {
            [JsonProperty]
            public string? Payload { get; set; }
            [JsonProperty]
            public ISequenceContainer? Owner { get; set; }
            [JsonProperty]
            public global::NINA.Sequencer.SequenceItem.ISequenceItem? Peer { get; set; }
            public ISequenceContainer? OwnerWhenDeserialized { get; private set; }
            [System.Runtime.Serialization.OnDeserialized]
            private void ObserveOwner(System.Runtime.Serialization.StreamingContext context) => OwnerWhenDeserialized = Owner;
            public override object Clone() => new StreamingItem();
            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) => Task.CompletedTask;
        }

        private sealed class SnapshotArea : TargetAreaContainer {
            [JsonProperty, JsonConverter(typeof(RetainingNameConverter))]
            public new string Name { get => base.Name; set => base.Name = value; }
        }

        public sealed class SnapshotItem : global::NINA.Sequencer.SequenceItem.SequenceItem {
            [JsonProperty, JsonConverter(typeof(RetainingNameConverter))]
            public string? Caption { get; set; }
            [JsonProperty]
            public global::NINA.Sequencer.SequenceItem.ISequenceItem? Child { get; set; }
            public override object Clone() => new SnapshotItem();
            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) => Task.CompletedTask;
        }

        private sealed class RetainingNameConverter : JsonConverter<string> {
            public static JObject? Snapshot { get; set; }
            public override bool CanWrite => false;
            public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer) {
                Snapshot = (JObject)((JTokenReader)reader).CurrentToken!.Root;
                return reader.Value as string;
            }
            public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer) => throw new NotSupportedException();
        }

        private sealed class ObservingResolver(List<string?> roots) : DefaultContractResolver {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
                var property = base.CreateProperty(member, memberSerialization);
                if (property.PropertyName == "Name") property.Converter = new ObservingStringConverter(new List<string>(), roots);
                return property;
            }
        }

        private sealed class ObservingStringConverter(List<string> paths, List<string?> roots) : JsonConverter<string> {
            public override bool CanWrite => false;
            public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer) {
                paths.Add(reader.Path);
                if (reader.Path == "Name" && (string?)reader.Value == "targets") {
                    roots.Add((string?)((JTokenReader)reader).CurrentToken!.Root["Name"]);
                }
                return reader.Value as string;
            }
            public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer) => throw new NotSupportedException();
        }
    }
}
