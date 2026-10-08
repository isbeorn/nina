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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
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

            ISequenceItem? result = JsonConvert.DeserializeObject<ISequenceItem>(json, sut);

            result.Should().BeOfType<TestSequenceItem>().Which.SerializedName.Should().Be("Created Name");
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

            ISequenceItem? result = JsonConvert.DeserializeObject<ISequenceItem>(json, sut);

            result.Should().BeOfType<TestSequenceItem>().Which.SerializedName.Should().Be("BeforeCreate-AfterPopulate");
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

            ISequenceItem? result = JsonConvert.DeserializeObject<ISequenceItem>(json, sut);

            result.Should().BeOfType<UnknownSequenceItem>().Which.Name.Should().Contain(typeof(TestSequenceItem).FullName);
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
        /// Verifies the Sequence Json Converter Round Trips Conditional Container Expression Through Factory Converters scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceJsonConverter_RoundTripsConditionalContainerExpressionThroughFactoryConverters() {
            TestSequencerFactory factory = new TestSequencerFactory();
            factory.ContainersByType[typeof(ConditionalContainer)] = new ConditionalContainer();
            SequenceJsonConverter sut = new SequenceJsonConverter(factory);
            ConditionalContainer source = new ConditionalContainer {
                Name = "Renamed Conditional",
                IsExpanded = false
            };
            source.PredicateExpression.Definition = "1 + 1";

            string json = sut.Serialize(source);
            ISequenceContainer result = sut.Deserialize(json, sourcePath: @"C:\sequence.template.json");

            json.Should().Contain("$type").And.Contain(nameof(ConditionalContainer)).And.Contain(nameof(ConditionalContainer.PredicateDefinition));
            ConditionalContainer conditional = result.Should().BeOfType<ConditionalContainer>().Subject;
            conditional.Name.Should().Be("Renamed Conditional");
            conditional.IsExpanded.Should().BeFalse();
            conditional.PredicateExpression.Definition.Should().Be("1 + 1");
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

        [Test]
        public void NestedContainers_DoNotCopyLeafJsonAtEveryAncestor() {
            var factory = CreateCloningFactory();
            var converter = new SequenceJsonConverter(factory);
            string shallow = NestedDocument(1, 512).ToString(Formatting.None);
            string deep = NestedDocument(8, 512).ToString(Formatting.None);
            converter.Deserialize(shallow);
            converter.Deserialize(deep);

            long before = GC.GetAllocatedBytesForCurrentThread();
            var shallowResult = converter.Deserialize(shallow);
            long shallowBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            var result = converter.Deserialize(deep);
            long deepBytes = GC.GetAllocatedBytesForCurrentThread() - before;

            shallowResult.Items.Should().HaveCount(512);
            for (int i = 1; i < 8; i++) result = (ISequenceContainer)result.Items.Single();
            result.Items.Should().HaveCount(512);
            TestContext.Progress.WriteLine($"Shallow allocation: {shallowBytes}; deep allocation: {deepBytes}");
            deepBytes.Should().BeLessThan(shallowBytes * 2, "adding container levels must not copy every leaf's JSON at each level");
        }

        [Test]
        public void ReadJson_PreservesCallerOwnedTokensWhileMigratingNestedEntities() {
            var factory = CreateCloningFactory();
            var item = ItemDocument("exposure");
            item["ImageType"] = "DARKFLAT";
            var document = ContainerDocument(item, LinkedDocument());
            var original = document.DeepClone();
            var containerConverter = new SequenceContainerCreationConverter(factory);
            var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings {
                Converters = { containerConverter, new SequenceItemCreationConverter(factory, containerConverter) }
            });

            using var reader = document.CreateReader();
            var result = serializer.Deserialize<ISequenceContainer>(reader)!;

            result.Items[0].Should().BeOfType<TestSequenceItem>().Which.ImageType.Should().Be("DARK");
            result.Items[1].Should().BeOfType<LinkedTemplateContainer>().Which.Items.Should().BeEmpty();
            JToken.DeepEquals(document, original).Should().BeTrue("ReadJson must not mutate an external reader's document");
        }

        [Test]
        public void ReadJson_PreservesSiblingsAndParentReferencesAcrossNestedContainers() {
            var converter = new SequenceJsonConverter(CreateCloningFactory());
            var source = new SequentialContainer();
            var nested = new SequentialContainer();
            source.Add(nested);
            var first = new TestSequenceItem { SerializedName = "first" };
            nested.Add(first);
            nested.Add(new TestSequenceItem { SerializedName = "second" });
            nested.Items.Add(first);
            source.Add(new TestSequenceItem { SerializedName = "following sibling" });

            var result = converter.Deserialize(converter.Serialize(source));

            result.Items.Should().HaveCount(2);
            var child = result.Items[0].Should().BeOfType<SequentialContainer>().Subject;
            child.Parent.Should().BeSameAs(result);
            child.Items.Should().HaveCount(3);
            child.Items[0].Parent.Should().BeSameAs(child);
            child.Items[0].Should().BeSameAs(child.Items[2]);
            child.Items[1].Should().BeOfType<TestSequenceItem>().Which.SerializedName.Should().Be("second");
            result.Items[1].Should().BeOfType<TestSequenceItem>().Which.SerializedName.Should().Be("following sibling");
        }

        [Test]
        public void ReadJson_KeepsUpgraderJsonDetachedAndIsolatedFromChildMigrations() {
            var factory = CreateCloningFactory();
            var upgrader = new InspectingUpgrader();
            factory.Upgraders.Add(upgrader);
            var parent = ItemDocument("parent");
            parent["Child"] = LinkedDocument();

            var result = new SequenceJsonConverter(factory).Deserialize(ContainerDocument(parent).ToString());

            var item = result.Items.Single().Should().BeOfType<TestSequenceItem>().Subject;
            item.SerializedName.Should().Be("upgraded");
            item.Child.Should().BeOfType<LinkedTemplateContainer>().Which.Items.Should().BeEmpty();
            upgrader.JsonWasDetached.Should().BeTrue();
            upgrader.JsonAfterPopulate!["Child"]!["Items"]!.Count().Should().Be(1,
                "a child's migration must not modify the parent's upgrade context");
        }

        [Test]
        public void ReadJson_KeepsCustomConverterJsonDetachedAndIsolatedFromChildMigrations() {
            var factory = CreateCloningFactory();
            var containerConverter = new SequenceContainerCreationConverter(factory);
            var customConverter = new InspectingItemCreationConverter(factory, containerConverter);
            var parent = ItemDocument("parent");
            parent["Child"] = LinkedDocument();

            var result = JsonConvert.DeserializeObject<ISequenceContainer>(ContainerDocument(parent).ToString(),
                containerConverter, customConverter)!;

            result.Items.Single().Should().BeOfType<TestSequenceItem>().Which.Child
                .Should().BeOfType<LinkedTemplateContainer>().Which.Items.Should().BeEmpty();
            customConverter.JsonWasDetached.Should().BeTrue();
            customConverter.ParentJson!["Child"]!["Items"]!.Count().Should().Be(1,
                "custom Create implementations may retain their detached JSON through population");
        }

        [Test]
        public void ReadJson_KeepsDelegatedCustomContainerJsonDetachedAndIsolated() {
            var factory = CreateCloningFactory();
            var containerConverter = new SequenceContainerCreationConverter(factory);
            var delegatedConverter = new InspectingContainerCreationConverter(factory);
            var itemConverter = new SequenceItemCreationConverter(factory, delegatedConverter);
            var child = ContainerDocument(LinkedDocument());
            child["Name"] = "custom child";

            var result = JsonConvert.DeserializeObject<ISequenceContainer>(ContainerDocument(child).ToString(),
                containerConverter, itemConverter)!;

            result.Items.Single().Should().BeOfType<SequentialContainer>().Which.Items.Single()
                .Should().BeOfType<LinkedTemplateContainer>().Which.Items.Should().BeEmpty();
            delegatedConverter.JsonWasDetached.Should().BeTrue();
            delegatedConverter.ChildJson!["Items"]![0]!["Items"]!.Count().Should().Be(1,
                "an item converter's custom container delegate also owns its JSON snapshot");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TypeResolution_RepeatedBuiltInNamesAvoidReflectionAllocations(bool legacyName) {
            using var context = AssemblyLoadContext.GetLoadContext(typeof(SequenceJsonConverter).Assembly)!.EnterContextualReflection();
            var sut = new TestItemCreationConverter(new TestSequencerFactory());
            string typeName = legacyName
                ? $"{typeof(SequentialContainer).FullName}, NINA"
                : typeof(SequentialContainer).AssemblyQualifiedName!;
            for (int i = 0; i < 8; i++) sut.ResolveType(typeName);

            Type? result = null;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1024; i++) result = sut.ResolveType(typeName);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            result.Should().Be(typeof(SequentialContainer));
            TestContext.Progress.WriteLine($"Repeated type resolution (legacy={legacyName}): {allocated:N0} bytes.");
            allocated.Should().BeLessThan(16 * 1024, "successful built-in names should reuse their resolved type");
            sut.ResolveType(typeName.Replace(nameof(SequentialContainer), nameof(SequentialContainer).ToLowerInvariant()))
                .Should().BeNull("serialized type names remain case sensitive");
        }

        [Test]
        public void TypeResolution_ConcurrentCurrentAndLegacyNamesReturnCorrectTypes() {
            var sut = new TestItemCreationConverter(new TestSequencerFactory());
            var types = new[] { typeof(SequentialContainer), typeof(SequenceRootContainer), typeof(LoopCondition), typeof(LinkedTemplateContainer) };
            var results = new Type?[256];

            Parallel.For(0, results.Length, i => {
                Type type = types[i % types.Length];
                string typeName = i % 2 == 0 ? type.AssemblyQualifiedName! : $"{type.FullName}, NINA";
                results[i] = sut.ResolveType(typeName);
            });

            for (int i = 0; i < results.Length; i++) results[i].Should().Be(types[i % types.Length]);
        }

        [Test]
        [NonParallelizable]
        public void TypeResolution_MissingAndExternalTypesKeepUsingResolutionHooks() {
            var sut = new TestItemCreationConverter(new TestSequencerFactory());
            string typeName = typeof(TestSequenceItem).FullName!;
            Assembly? availableAssembly = null;
            int resolutions = 0;
            ResolveEventHandler resolver = (_, args) => {
                if (args.Name != typeName) return null;
                resolutions++;
                return availableAssembly;
            };
            AppDomain.CurrentDomain.TypeResolve += resolver;
            try {
                sut.ResolveType(typeName).Should().BeNull();

                availableAssembly = typeof(TestSequenceItem).Assembly;
                sut.ResolveType(typeName).Should().Be(typeof(TestSequenceItem), "an earlier miss must not hide a newly available plugin type");
                int previousResolutions = resolutions;
                sut.ResolveType(typeName).Should().Be(typeof(TestSequenceItem));
                resolutions.Should().BeGreaterThan(previousResolutions, "external type resolution remains observable on every call");

                availableAssembly = null;
                sut.ResolveType(typeName).Should().BeNull("the converter must not retain external resolution results");
            } finally {
                AppDomain.CurrentDomain.TypeResolve -= resolver;
            }
        }

        [Test]
        public async Task TypeResolution_ContextualReflectionDoesNotUseOrPopulateDefaultCache() {
            // A second sequencer assembly would replace WPF's process-wide resource cache.
            if (await IsolatedTestProcess.RunCurrentTest()) return;
            var warmed = new TestItemCreationConverter(new TestSequencerFactory());
            var cold = new TestItemCreationConverter(new TestSequencerFactory());
            string typeName = typeof(SequentialContainer).AssemblyQualifiedName!;
            warmed.ResolveType(typeName).Should().Be(typeof(SequentialContainer));
            var context = new AssemblyLoadContext("sequence-type-resolution", isCollectible: true);
            try {
                var assembly = context.LoadFromAssemblyPath(typeof(SequentialContainer).Assembly.Location);
                var contextualType = assembly.GetType(typeof(SequentialContainer).FullName!)!;
                contextualType.Should().NotBe(typeof(SequentialContainer));

                using (context.EnterContextualReflection()) {
                    warmed.ResolveType(typeName).Should().Be(contextualType);
                    cold.ResolveType(typeName).Should().Be(contextualType);
                }

                warmed.ResolveType(typeName).Should().Be(typeof(SequentialContainer));
                cold.ResolveType(typeName).Should().Be(typeof(SequentialContainer));
            } finally {
                context.Unload();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TypeResolution_AlternateBuiltInSpellingsHaveBoundedRetention(bool concurrent) {
            var sut = new TestItemCreationConverter(new TestSequencerFactory());
            var names = new WeakReference[512];
            if (concurrent) Parallel.For(0, names.Length, i => names[i] = ResolveAlternateTypeName(sut, i));
            else for (int i = 0; i < names.Length; i++) names[i] = ResolveAlternateTypeName(sut, i);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            int retainedNames = names.Count(name => name.IsAlive);
            GC.KeepAlive(sut);

            retainedNames.Should().BeLessThanOrEqualTo(256,
                "a converter reused across files must not retain every alternate spelling of a built-in type");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference ResolveAlternateTypeName(TestItemCreationConverter converter, int spaces) {
            string typeName = $"{typeof(SequentialContainer).FullName},{new string(' ', spaces)}NINA.Sequencer";
            converter.ResolveType(typeName).Should().Be(typeof(SequentialContainer));
            return new WeakReference(typeName);
        }

        private static TestSequencerFactory CreateCloningFactory() {
            var factory = new TestSequencerFactory { CloneEntities = true };
            factory.ContainersByType[typeof(SequentialContainer)] = new SequentialContainer();
            factory.ContainersByType[typeof(LinkedTemplateContainer)] = new LinkedTemplateContainer();
            factory.ItemsByType[typeof(TestSequenceItem)] = new TestSequenceItem();
            return factory;
        }

        private static JObject ItemDocument(string name) => new JObject {
            ["$type"] = typeof(TestSequenceItem).AssemblyQualifiedName,
            ["SerializedName"] = name
        };

        private static JObject ContainerDocument(params JObject[] children) => new JObject {
            ["$type"] = typeof(SequentialContainer).AssemblyQualifiedName,
            ["Strategy"] = new JObject { ["$type"] = typeof(NINA.Sequencer.Container.ExecutionStrategy.SequentialStrategy).AssemblyQualifiedName },
            ["Items"] = new JArray(children)
        };

        private static JObject LinkedDocument() {
            var document = ContainerDocument(ItemDocument("runtime contents"));
            document["$type"] = typeof(LinkedTemplateContainer).AssemblyQualifiedName;
            return document;
        }

        private static JObject NestedDocument(int depth, int leafCount) {
            var document = ContainerDocument(Enumerable.Range(0, leafCount).Select(i => ItemDocument($"leaf-{i}")).ToArray());
            for (int i = 1; i < depth; i++) document = ContainerDocument(document);
            return document;
        }

        private sealed class InspectingUpgrader : ISequenceEntityUpgrader {
            public string Name { get; set; } = "Inspecting";
            public SequenceUpgradeStage Stages => SequenceUpgradeStage.BeforeCreate | SequenceUpgradeStage.AfterPopulate;
            public bool JsonWasDetached { get; private set; }
            public JObject? JsonAfterPopulate { get; private set; }

            public object Upgrade(SequenceUpgradeContext context, SequenceUpgradeStage stage, object? entity) {
                if (stage == SequenceUpgradeStage.BeforeCreate) {
                    JsonWasDetached = context.Json.Parent == null && ReferenceEquals(context.Json.Root, context.Json);
                    context.Json["SerializedName"] = "upgraded";
                } else {
                    JsonAfterPopulate = context.Json;
                }
                return entity!;
            }
        }

        private sealed class InspectingItemCreationConverter : SequenceItemCreationConverter {
            public bool JsonWasDetached { get; private set; }
            public JObject? ParentJson { get; private set; }

            public InspectingItemCreationConverter(ISequencerFactory factory, SequenceContainerCreationConverter containers) : base(factory, containers) { }

            public override ISequenceItem Create(Type objectType, JObject jObject) {
                if ((string?)jObject["SerializedName"] == "parent") {
                    JsonWasDetached = jObject.Parent == null && ReferenceEquals(jObject.Root, jObject);
                    ParentJson = jObject;
                }
                return base.Create(objectType, jObject);
            }
        }

        private sealed class InspectingContainerCreationConverter : SequenceContainerCreationConverter {
            public bool JsonWasDetached { get; private set; }
            public JObject? ChildJson { get; private set; }

            public InspectingContainerCreationConverter(ISequencerFactory factory) : base(factory) { }

            public override ISequenceContainer Create(Type objectType, JObject jObject) {
                if ((string?)jObject["Name"] == "custom child") {
                    JsonWasDetached = jObject.Parent == null && ReferenceEquals(jObject.Root, jObject);
                    ChildJson = jObject;
                }
                return base.Create(objectType, jObject);
            }
        }

        private sealed class TestItemCreationConverter : JsonCreationConverter<ISequenceItem> {
            public bool ThrowOnCreate { get; set; }

            public Type? ResolveType(string typeName) => GetType(typeName);

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
            public string? SerializedName { get; set; }

            [JsonProperty]
            public string? ImageType { get; set; }

            [JsonProperty]
            public ISequenceItem? Child { get; set; }

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

                TestSequenceItem item = current.Should().BeOfType<TestSequenceItem>().Which;
                if (stage == SequenceUpgradeStage.AfterCreate) {
                    item.SerializedName = "AfterCreate";
                    return item;
                }

                item.SerializedName = $"{item.SerializedName}-AfterPopulate";
                return item;
            }
        }

        private sealed class TestSequencerFactory : ISequencerFactory {
            public bool CloneEntities { get; set; }
            public IDictionary<Type, ISequenceItem> ItemsByType { get; } = new Dictionary<Type, ISequenceItem>();
            public IDictionary<Type, ISequenceContainer> ContainersByType { get; } = new Dictionary<Type, ISequenceContainer>();
            public IDictionary<Type, ISequenceCondition> ConditionsByType { get; } = new Dictionary<Type, ISequenceCondition>();
            public IDictionary<Type, ISequenceTrigger> TriggersByType { get; } = new Dictionary<Type, ISequenceTrigger>();

            public IList<ISequenceCondition> Conditions { get; } = new List<ISequenceCondition>();
            public IList<ISequenceContainer> Container { get; } = new List<ISequenceContainer>();
            public IList<ISequenceItem> Items { get; } = new List<ISequenceItem>();
            public ICollectionView? ItemsView => null;
            public ICollectionView? InstructionsView => null;
            public ICollectionView? ConditionsView => null;
            public ICollectionView? TriggersView => null;
            public IList<ISequenceTrigger> Triggers { get; } = new List<ISequenceTrigger>();
            public IList<IDateTimeProvider> DateTimeProviders { get; } = new List<IDateTimeProvider>();
            public IList<ISequenceEntityUpgrader> Upgraders { get; } = new List<ISequenceEntityUpgrader>();
            public string? ViewFilter { get; set; }

            [return: System.Diagnostics.CodeAnalysis.MaybeNull]
            public T GetCondition<T>() where T : ISequenceCondition {
                return ConditionsByType.TryGetValue(typeof(T), out ISequenceCondition? condition)
                    ? (T)condition
                    : default;
            }

            [return: System.Diagnostics.CodeAnalysis.MaybeNull]
            public T GetContainer<T>() where T : ISequenceContainer {
                return ContainersByType.TryGetValue(typeof(T), out ISequenceContainer? container)
                    ? (T)(CloneEntities ? container.Clone() : container)
                    : default;
            }

            [return: System.Diagnostics.CodeAnalysis.MaybeNull]
            public T GetItem<T>() where T : ISequenceItem {
                return ItemsByType.TryGetValue(typeof(T), out ISequenceItem? item)
                    ? (T)(CloneEntities ? item.Clone() : item)
                    : default;
            }

            [return: System.Diagnostics.CodeAnalysis.MaybeNull]
            public T GetTrigger<T>() where T : ISequenceTrigger {
                return TriggersByType.TryGetValue(typeof(T), out ISequenceTrigger? trigger)
                    ? (T)trigger
                    : default;
            }
        }
    }
}
