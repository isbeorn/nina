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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Connect;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Test.Sequencer.Serialization {

    [TestFixture]
    public class JsonCreationConverterTest {

        /// <summary>
        /// Verifies the Extract Plugin Name Handles Assembly Qualified And Invalid Type Strings scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ExtractPluginName_HandlesAssemblyQualifiedAndInvalidTypeStrings() {
            JsonCreationConverter<ISequenceItem>.ExtractPluginName("Some.Namespace.Type, Plugin.Assembly")
                .Should().Be("Plugin.Assembly");
            JsonCreationConverter<ISequenceItem>.ExtractPluginName("Some.Namespace.Type")
                .Should().BeEmpty();
            JsonCreationConverter<ISequenceItem>.ExtractPluginName("")
                .Should().BeEmpty();
            JsonCreationConverter<ISequenceItem>.ExtractPluginName(null)
                .Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the Converter Metadata Disables Writing And Matches Assignable Types Only scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ConverterMetadata_DisablesWritingAndMatchesAssignableTypesOnly() {
            TestItemCreationConverter sut = new TestItemCreationConverter(new TestSequencerFactory());

            sut.CanWrite.Should().BeFalse();
            sut.CanConvert(typeof(ISequenceItem)).Should().BeTrue();
            sut.CanConvert(typeof(TestSequenceItem)).Should().BeTrue();
            sut.CanConvert(typeof(ISequenceCondition)).Should().BeFalse();

            Action act = () => sut.WriteJson(new JsonTextWriter(TextWriter.Null), new TestSequenceItem(), JsonSerializer.CreateDefault());

            act.Should().Throw<NotImplementedException>();
        }

        /// <summary>
        /// Verifies the Read Json Creates And Populates Target scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ReadJson_CreatesAndPopulatesTarget() {
            TestItemCreationConverter sut = new TestItemCreationConverter(new TestSequencerFactory());
            string json = $$"""
                {
                  "$type": "{{typeof(TestSequenceItem).AssemblyQualifiedName}}",
                  "SerializedName": "Created Name"
                }
                """;

            ISequenceItem result = JsonConvert.DeserializeObject<ISequenceItem>(json, sut);

            result.Should().BeOfType<TestSequenceItem>();
            ((TestSequenceItem)result).SerializedName.Should().Be("Created Name");
        }

        /// <summary>
        /// Verifies the Read Json Uses Plugin Upgrader Stages Around Create And Populate scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ReadJson_UsesPluginUpgraderStagesAroundCreateAndPopulate() {
            TestSequencerFactory factory = new TestSequencerFactory();
            TrackingUpgrader upgrader = new TrackingUpgrader();
            factory.Upgraders.Add(upgrader);
            TestItemCreationConverter sut = new TestItemCreationConverter(factory);
            string json = $$"""
                {
                  "$type": "Plugin.Item, {{typeof(TrackingUpgrader).Assembly.GetName().Name}}",
                  "SerializedName": "Original Name"
                }
                """;

            ISequenceItem result = JsonConvert.DeserializeObject<ISequenceItem>(json, sut);

            result.Should().BeOfType<TestSequenceItem>();
            ((TestSequenceItem)result).SerializedName.Should().Be("BeforeCreate-AfterPopulate");
            upgrader.SeenStages.Should().Equal(
                SequenceUpgradeStage.BeforeCreate,
                SequenceUpgradeStage.Create,
                SequenceUpgradeStage.AfterCreate,
                SequenceUpgradeStage.AfterPopulate);
        }

        /// <summary>
        /// Verifies the Read Json Returns Unknown Item When Create Fails scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ReadJson_ReturnsUnknownItemWhenCreateFails() {
            TestItemCreationConverter sut = new TestItemCreationConverter(new TestSequencerFactory()) {
                ThrowOnCreate = true
            };
            string json = $$"""
                {
                  "$type": "{{typeof(TestSequenceItem).AssemblyQualifiedName}}"
                }
                """;

            ISequenceItem result = JsonConvert.DeserializeObject<ISequenceItem>(json, sut);

            result.Should().BeOfType<UnknownSequenceItem>();
            result.Name.Should().Contain(typeof(TestSequenceItem).FullName);
        }

        /// <summary>
        /// Verifies the Read Json Attaches Replacement Target To Existing Parent scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ReadJson_AttachesReplacementTargetToExistingParent() {
            TestItemCreationConverter sut = new TestItemCreationConverter(new TestSequencerFactory());
            SequenceRootContainer parent = new SequenceRootContainer();
            TestSequenceItem existing = new TestSequenceItem();
            parent.Add(existing);
            string json = $$"""
                {
                  "$type": "{{typeof(TestSequenceItem).AssemblyQualifiedName}}"
                }
                """;

            using StringReader stringReader = new StringReader(json);
            using JsonTextReader jsonReader = new JsonTextReader(stringReader);
            jsonReader.Read();
            JsonSerializer serializer = JsonSerializer.CreateDefault();

            ISequenceItem result = (ISequenceItem)sut.ReadJson(jsonReader, typeof(ISequenceItem), existing, serializer);

            result.Should().BeOfType<TestSequenceItem>();
            result.Parent.Should().BeSameAs(parent);
        }

        /// <summary>
        /// Verifies the Sequence Item Creation Converter Create Migrates Dark Flat Image Type Before Factory Creation scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceItemCreationConverter_Create_MigratesDarkFlatImageTypeBeforeFactoryCreation() {
            TestSequencerFactory factory = new TestSequencerFactory();
            factory.ItemsByType[typeof(TestSequenceItem)] = new TestSequenceItem();
            SequenceItemCreationConverter sut = new SequenceItemCreationConverter(factory, new SequenceContainerCreationConverter(factory));
            JObject json = JObject.Parse($$"""
                {
                  "$type": "{{typeof(TestSequenceItem).AssemblyQualifiedName}}",
                  "ImageType": "DARKFLAT"
                }
                """);

            ISequenceItem result = sut.Create(typeof(ISequenceItem), json);

            result.Should().BeOfType<TestSequenceItem>();
            json["ImageType"]!.Value<string>().Should().Be("DARK");
        }

        /// <summary>
        /// Verifies the Sequence Item Creation Converter Create Delegates Container Json To Container Converter scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceItemCreationConverter_Create_DelegatesContainerJsonToContainerConverter() {
            TestSequencerFactory factory = new TestSequencerFactory();
            factory.ContainersByType[typeof(SequentialContainer)] = new SequentialContainer();
            SequenceItemCreationConverter sut = new SequenceItemCreationConverter(factory, new SequenceContainerCreationConverter(factory));
            JObject json = JObject.Parse($$"""
                {
                  "$type": "{{typeof(SequentialContainer).AssemblyQualifiedName}}",
                  "Strategy": {
                    "$type": "NINA.Sequencer.Container.ExecutionStrategy.SequentialStrategy, NINA.Sequencer"
                  }
                }
                """);

            ISequenceItem result = sut.Create(typeof(ISequenceItem), json);

            result.Should().BeOfType<SequentialContainer>();
        }

        /// <summary>
        /// Verifies the Sequence Item Creation Converter Create Returns Unknown Item For Missing Or Unresolvable Type scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceItemCreationConverter_Create_ReturnsUnknownItemForMissingOrUnresolvableType() {
            TestSequencerFactory factory = new TestSequencerFactory();
            SequenceItemCreationConverter sut = new SequenceItemCreationConverter(factory, new SequenceContainerCreationConverter(factory));

            sut.Create(typeof(ISequenceItem), new JObject()).Should().BeOfType<UnknownSequenceItem>();

            ISequenceItem migratedUnknown = sut.Create(
                typeof(ISequenceItem),
                JObject.Parse("""
                    {
                      "$type": "NINA.Plugins.Connector.Instructions.ConnectAllEquipment, NINA.Plugins.Connector"
                    }
                    """));

            migratedUnknown.Should().BeOfType<UnknownSequenceItem>();
            migratedUnknown.Name.Should().Contain("ConnectAllEquipment");
        }

        /// <summary>
        /// Verifies the Sequence Json Converter Round Trips Registered Container Through Factory Converters scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceJsonConverter_RoundTripsRegisteredContainerThroughFactoryConverters() {
            TestSequencerFactory factory = new TestSequencerFactory();
            factory.ContainersByType[typeof(SequentialContainer)] = new SequentialContainer();
            SequenceJsonConverter sut = new SequenceJsonConverter(factory);
            SequentialContainer source = new SequentialContainer {
                Name = "Serialized Container",
                IsExpanded = false
            };

            string json = sut.Serialize(source);
            ISequenceContainer result = sut.Deserialize(json, sourcePath: @"C:\sequence.template.json");

            json.Should().Contain("$type").And.Contain(nameof(SequentialContainer));
            result.Should().BeOfType<SequentialContainer>();
            result.Name.Should().Be("Serialized Container");
            result.IsExpanded.Should().BeFalse();
        }

        /// <summary>
        /// Verifies the Sequence Condition Creation Converter Creates Registered Condition Or Unknown Fallbacks scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceConditionCreationConverter_CreatesRegisteredConditionOrUnknownFallbacks() {
            TestSequencerFactory factory = new TestSequencerFactory();
            LoopCondition loopCondition = new LoopCondition();
            factory.ConditionsByType[typeof(LoopCondition)] = loopCondition;
            SequenceConditionCreationConverter sut = new SequenceConditionCreationConverter(factory);

            ISequenceCondition created = sut.Create(
                typeof(ISequenceCondition),
                JObject.Parse($$"""
                    {
                      "$type": "{{typeof(LoopCondition).AssemblyQualifiedName}}"
                    }
                    """));

            created.Should().BeSameAs(loopCondition);
            sut.Create(typeof(ISequenceCondition), new JObject()).Should().BeOfType<UnknownSequenceCondition>();
            sut.Create(
                    typeof(ISequenceCondition),
                    JObject.Parse("""
                        {
                          "$type": "Missing.Condition, Missing.Assembly"
                        }
                        """))
                .Should().BeOfType<UnknownSequenceCondition>();
        }

        /// <summary>
        /// Verifies the Sequence Trigger Creation Converter Creates Registered Trigger Migrates Plugin Names And Falls Back To Unknown scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceTriggerCreationConverter_CreatesRegisteredTriggerMigratesPluginNamesAndFallsBackToUnknown() {
            TestSequencerFactory factory = new TestSequencerFactory();
            TestSequenceTrigger trigger = new TestSequenceTrigger();
            factory.TriggersByType[typeof(TestSequenceTrigger)] = trigger;
            SequenceTriggerCreationConverter sut = new SequenceTriggerCreationConverter(factory);

            ISequenceTrigger created = sut.Create(
                typeof(ISequenceTrigger),
                JObject.Parse($$"""
                    {
                      "$type": "{{typeof(TestSequenceTrigger).AssemblyQualifiedName}}"
                    }
                    """));

            created.Should().BeSameAs(trigger);
            sut.Create(typeof(ISequenceTrigger), new JObject()).Should().BeOfType<UnknownSequenceTrigger>();
            sut.Create(
                    typeof(ISequenceTrigger),
                    JObject.Parse("""
                        {
                          "$type": "NINA.Plugins.Connector.Instructions.ReconnectTrigger, NINA.Plugins.Connector"
                        }
                        """))
                .Should().BeOfType<UnknownSequenceTrigger>()
                .Which.Name.Should().Contain(nameof(ReconnectTrigger));
        }

        private sealed class TestItemCreationConverter : JsonCreationConverter<ISequenceItem> {
            public bool ThrowOnCreate { get; set; }

            public TestItemCreationConverter(ISequencerFactory factory) : base(factory) {
            }

            public override ISequenceItem Create(Type objectType, JObject jObject) {
                if (ThrowOnCreate) {
                    throw new InvalidOperationException("create failed");
                }

                return new TestSequenceItem();
            }
        }

        private sealed class TestSequenceItem : global::NINA.Sequencer.SequenceItem.SequenceItem {
            [JsonProperty]
            public string SerializedName { get; set; }

            public override object Clone() {
                return new TestSequenceItem {
                    Name = Name,
                    SerializedName = SerializedName,
                    Description = Description,
                    Category = Category,
                    Icon = Icon
                };
            }

            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
                return Task.CompletedTask;
            }
        }

        private sealed class TestSequenceTrigger : SequenceTrigger {

            public override object Clone() {
                return new TestSequenceTrigger {
                    Name = Name,
                    Description = Description,
                    Category = Category,
                    Icon = Icon
                };
            }

            public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
                return Task.CompletedTask;
            }

            public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
                return false;
            }
        }

        private sealed class TrackingUpgrader : ISequenceEntityUpgrader {
            public string Name { get; set; } = "Tracking";
            public SequenceUpgradeStage Stages =>
                SequenceUpgradeStage.BeforeCreate |
                SequenceUpgradeStage.Create |
                SequenceUpgradeStage.AfterCreate |
                SequenceUpgradeStage.AfterPopulate;

            public IList<SequenceUpgradeStage> SeenStages { get; } = new List<SequenceUpgradeStage>();

            public object Upgrade(SequenceUpgradeContext context, SequenceUpgradeStage stage, object? current) {
                SeenStages.Add(stage);

                if (stage == SequenceUpgradeStage.BeforeCreate) {
                    JObject json = (JObject)context.Json.DeepClone();
                    json["SerializedName"] = "BeforeCreate";
                    return json;
                }

                if (stage == SequenceUpgradeStage.Create) {
                    return new TestSequenceItem {
                        SerializedName = "Create"
                    };
                }

                if (stage == SequenceUpgradeStage.AfterCreate) {
                    ((TestSequenceItem)current).SerializedName = "AfterCreate";
                    return current;
                }

                ((TestSequenceItem)current).SerializedName = $"{((TestSequenceItem)current).SerializedName}-AfterPopulate";
                return current;
            }
        }

        private sealed class TestSequencerFactory : ISequencerFactory {
            public IDictionary<Type, ISequenceItem> ItemsByType { get; } = new Dictionary<Type, ISequenceItem>();
            public IDictionary<Type, ISequenceContainer> ContainersByType { get; } = new Dictionary<Type, ISequenceContainer>();
            public IDictionary<Type, ISequenceCondition> ConditionsByType { get; } = new Dictionary<Type, ISequenceCondition>();
            public IDictionary<Type, ISequenceTrigger> TriggersByType { get; } = new Dictionary<Type, ISequenceTrigger>();

            public IList<ISequenceCondition> Conditions { get; } = new List<ISequenceCondition>();
            public IList<ISequenceContainer> Container { get; } = new List<ISequenceContainer>();
            public IList<ISequenceItem> Items { get; } = new List<ISequenceItem>();
            public ICollectionView ItemsView => null;
            public ICollectionView InstructionsView => null;
            public ICollectionView ConditionsView => null;
            public ICollectionView TriggersView => null;
            public IList<ISequenceTrigger> Triggers { get; } = new List<ISequenceTrigger>();
            public IList<IDateTimeProvider> DateTimeProviders { get; } = new List<IDateTimeProvider>();
            public IList<ISequenceEntityUpgrader> Upgraders { get; } = new List<ISequenceEntityUpgrader>();
            public string ViewFilter { get; set; }

            public T GetCondition<T>() where T : ISequenceCondition {
                return ConditionsByType.TryGetValue(typeof(T), out ISequenceCondition condition)
                    ? (T)condition
                    : default;
            }

            public T GetContainer<T>() where T : ISequenceContainer {
                return ContainersByType.TryGetValue(typeof(T), out ISequenceContainer container)
                    ? (T)container
                    : default;
            }

            public T GetItem<T>() where T : ISequenceItem {
                return ItemsByType.TryGetValue(typeof(T), out ISequenceItem item)
                    ? (T)item
                    : default;
            }

            public T GetTrigger<T>() where T : ISequenceTrigger {
                return TriggersByType.TryGetValue(typeof(T), out ISequenceTrigger trigger)
                    ? (T)trigger
                    : default;
            }
        }
    }
}
