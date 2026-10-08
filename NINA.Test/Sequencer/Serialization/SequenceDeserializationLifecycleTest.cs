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
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Core.Locale;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.ComponentModel;
using System.IO;

namespace NINA.Test.Sequencer.Serialization {
    [TestFixture, NonParallelizable]
    public class SequenceDeserializationLifecycleTest {
        private string directory = null!;
        private Mock<IProfileService> profile = null!;
        private Mock<ICameraMediator> camera = null!;
        private Mock<ISymbolBroker> broker = null!;
        private SequenceJsonConverter converter = null!;
        private Mock<ISequencerFactory> factory = null!;
        private readonly List<SequenceRootContainer> roots = new();
        private readonly List<TakeExposure> exposures = new();
        private readonly List<SunAltitudeCondition> conditions = new();
        private readonly Dictionary<ISequenceEntity, int> validationPasses = new();
        private readonly List<ActivationSnapshot> activations = new();

        [SetUp]
        public void SetUp() {
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
            directory = Path.Combine(Path.GetTempPath(), "nina-deserialization-lifecycle-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            roots.Clear();
            exposures.Clear();
            conditions.Clear();
            validationPasses.Clear();
            activations.Clear();
            profile = new Mock<IProfileService>();
            profile.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(directory);
            profile.SetupGet(x => x.ActiveProfile.AstrometrySettings).Returns(new NINA.Profile.AstrometrySettings());
            camera = new Mock<ICameraMediator>();
            camera.Setup(x => x.GetInfo()).Returns(new CameraInfo { Connected = true, DefaultGain = 100 });
            broker = new Mock<ISymbolBroker>();
            factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new List<ISequenceEntityUpgrader>());
            // Moq matches assignable generic arguments, so register derived containers last.
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => new SequentialContainer());
            factory.Setup(x => x.GetContainer<TargetAreaContainer>()).Returns(() => new TargetAreaContainer());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(CreateRoot);
            factory.Setup(x => x.GetItem<Constant>()).Returns(() => new Constant { SymbolBroker = broker.Object });
            factory.Setup(x => x.GetItem<TakeExposure>()).Returns(CreateExposure);
            factory.Setup(x => x.GetCondition<SunAltitudeCondition>()).Returns(CreateCondition);
            converter = new SequenceJsonConverter(factory.Object);
        }

        [TearDown]
        public void TearDown() {
            foreach (var root in roots) {
                foreach (var item in root.GetItemsSnapshot()) item.Detach();
                foreach (var condition in root.GetConditionsSnapshot()) condition.Detach();
                foreach (var trigger in root.GetTriggersSnapshot()) trigger.Detach();
            }
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
            foreach (string file in Directory.GetFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Deserialize_CompletesForwardReferenceValidationBeforeReturning(bool fromFile, bool definitionFirst) {
            var root = Load(ForwardReferenceJson(definitionFirst), fromFile);
            var area = root.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetAreaContainer>().Subject;
            var exposure = area.Items.OfType<TakeExposure>().Single();
            var symbol = area.Items.OfType<Constant>().Single();
            var condition = area.Conditions.Should().ContainSingle().Subject.Should().BeOfType<SunAltitudeCondition>().Subject;

            area.Parent.Should().BeSameAs(root);
            exposure.Parent.Should().BeSameAs(area);
            symbol.Parent.Should().BeSameAs(area);
            condition.Parent.Should().BeSameAs(area);
            exposure.Issues.Should().BeEmpty();
            condition.Issues.Should().BeEmpty();
            symbol.Issues.Should().BeEmpty();
            exposure.ExposureTimeExpression.Value.Should().Be(10);
            exposure.ExposureTimeExpression.Error.Should().BeNull();
            exposure.ExposureTimeExpression.Resolved["laterValue"].Should().BeSameAs(symbol);
            condition.OffsetExpression.Value.Should().Be(-5);
            condition.OffsetExpression.Error.Should().BeNull();
            condition.Data.Offset.Should().Be(-5);
            symbol.Consumers.Should().ContainKey(exposure.ExposureTimeExpression);
            symbol.Consumers.Should().ContainKey(condition.OffsetExpression);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Deserialize_ActivatesWatchdogOnlyAfterCompleteGraphValidation(bool fromFile) {
            Load(ForwardReferenceJson(definitionFirst: false), fromFile);

            activations.Should().ContainSingle("one completed root attachment activates its watchdog once");
            var activation = activations.Single();
            activation.GraphComplete.Should().BeTrue();
            activation.ValidatedExposures.Should().Be(1);
            activation.ConditionValidations.Should().BeGreaterThan(0);
            activation.Value.Should().Be(-5);
            activation.Proxy.Should().Be(-5);
            activation.Error.Should().BeNull();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Deserialize_BoundsCoreLeafValidationRegardlessOfContainerDepth(bool fromFile) {
            foreach (int depth in new[] { 1, 8, 16 }) {
                int firstExposure = exposures.Count;
                Load(ExposureTreeJson(depth, count: 6), fromFile);
                var loaded = exposures.Skip(firstExposure).ToArray();

                loaded.Should().HaveCount(6);
                loaded.Select(item => validationPasses[item]).Should().OnlyContain(count => count > 0 && count <= 2,
                    $"depth {depth} must not repeatedly validate the same leaf through its ancestors");
                loaded.Should().OnlyContain(item => item.Issues.Count == 0);
            }
        }

        [Test]
        public void Deserialize_FailedDocumentDoesNotActivateDeferredWatchdogsOrAffectNextLoad() {
            Action load = () => converter.Deserialize(ForwardReferenceJson(false) + " invalid");

            load.Should().Throw<JsonReaderException>();
            activations.Should().BeEmpty("a failed load must not start background work");

            Load(ForwardReferenceJson(false), fromFile: false);
            activations.Should().ContainSingle();
            exposures.Last().ExposureTimeExpression.Value.Should().Be(10);
            exposures.Last().Issues.Should().BeEmpty();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Deserialize_DiscardsOnlyTheWatchdogFromAnUnknownBranch(bool fromFile) {
            JObject Condition(string parent) => new() {
                ["$type"] = TypeName(typeof(SunAltitudeCondition)),
                ["OffsetExpression"] = new JObject { ["Definition"] = "-5" },
                ["Parent"] = Reference(parent)
            };
            var failedArea = ContainerJson(typeof(TargetAreaContainer), "failed", "root",
                new JArray(ExposureJson("failed", "10"), 42), new JArray(Condition("failed")));
            var survivingArea = ContainerJson(typeof(TargetAreaContainer), "surviving", "root",
                new JArray(), new JArray(Condition("surviving")));
            var json = ContainerJson(typeof(SequenceRootContainer), "root", null, new JArray(failedArea, survivingArea)).ToString();

            var root = Load(json, fromFile);

            root.Items.Should().HaveCount(2);
            root.Items[0].Should().BeOfType<UnknownSequenceItem>();
            root.Items[1].Should().BeOfType<TargetAreaContainer>();
            conditions.Should().HaveCount(2);
            conditions[0].Parent.Parent.Should().BeSameAs(root, "a discarded branch can retain its old parent reference");
            Mock.Get(conditions[0].ConditionWatchdog).Verify(x => x.Start(), Times.Never);
            Mock.Get(conditions[0].ConditionWatchdog).Verify(x => x.Cancel(), Times.AtLeastOnce);
            Mock.Get(conditions[1].ConditionWatchdog).Verify(x => x.Start(), Times.Once);
            activations.Should().ContainSingle();
            conditions[1].Issues.Should().BeEmpty();
            conditions[1].OffsetExpression.Value.Should().Be(-5);
            conditions[1].Data.Offset.Should().Be(-5);
        }

        [TestCase(false, "Items", false)]
        [TestCase(false, "Conditions", false)]
        [TestCase(false, "Triggers", false)]
        [TestCase(true, "Items", false)]
        [TestCase(true, "Conditions", false)]
        [TestCase(true, "Triggers", false)]
        [TestCase(false, "Items", true)]
        [TestCase(false, "Conditions", true)]
        [TestCase(false, "Triggers", true)]
        [TestCase(true, "Items", true)]
        [TestCase(true, "Conditions", true)]
        [TestCase(true, "Triggers", true)]
        public void Deserialize_MalformedChildCollectionPreservesUnknownFallbackAndValidSibling(bool fromFile, string collection, bool nullEntry) {
            var malformedArea = ContainerJson(typeof(TargetAreaContainer), "malformed", "root", new JArray());
            malformedArea[collection] = nullEntry
                ? new JObject { ["$values"] = new JArray(JValue.CreateNull()) }
                : JValue.CreateNull();
            var survivingArea = ContainerJson(typeof(TargetAreaContainer), "surviving", "root",
                new JArray(ExposureJson("surviving", "10")));
            var json = ContainerJson(typeof(SequenceRootContainer), "root", null, new JArray(malformedArea, survivingArea)).ToString();

            var root = Load(json, fromFile);

            root.Items.Should().HaveCount(2);
            root.Items[0].Should().BeOfType<UnknownSequenceItem>();
            var sibling = root.Items[1].Should().BeOfType<TargetAreaContainer>().Subject;
            sibling.Parent.Should().BeSameAs(root);
            var exposure = sibling.Items.Should().ContainSingle().Subject.Should().BeOfType<TakeExposure>().Subject;
            exposure.Parent.Should().BeSameAs(sibling);
            exposure.Issues.Should().BeEmpty();
            exposure.ExposureTimeExpression.Value.Should().Be(10);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Deserialize_DefaultSettingsProviderKeepsParentCallbacksEager(bool fromFile) {
            var json = ExposureTreeJson(depth: 1, count: 1);
            var previous = JsonConvert.DefaultSettings;
            var observed = new List<bool>();
            try {
                JsonConvert.DefaultSettings = () => {
                    observed.Add(RefreshIndependentCoreContainer());
                    return new JsonSerializerSettings();
                };

                Load(json, fromFile);

                observed.Should().Equal(new[] { true }, "the settings provider must observe immediately refreshed Issues during loading");
                exposures.Single().Issues.Should().BeEmpty();
            } finally {
                JsonConvert.DefaultSettings = previous;
            }
        }

        [Test]
        public void ExplicitValidate_RefreshesDeviceAndExpressionIssuesOnEveryCall() {
            Load(ExposureTreeJson(depth: 1, count: 1), fromFile: false);
            var exposure = exposures.Single();
            int initialPasses = validationPasses[exposure];
            camera.Setup(x => x.GetInfo()).Returns(new CameraInfo { Connected = false });

            exposure.Validate().Should().BeFalse();
            exposure.Issues.Should().Contain(Loc.Instance["LblCameraNotConnected"]);
            camera.Setup(x => x.GetInfo()).Returns(new CameraInfo { Connected = true, DefaultGain = 200 });
            exposure.Validate().Should().BeTrue();
            exposure.Issues.Should().BeEmpty();
            exposure.GainExpression.Default.Should().Be(200);
            exposure.ExposureTimeExpression.Definition = "1 +";
            exposure.Validate().Should().BeFalse();
            exposure.Issues.Should().Contain(Loc.Instance["LblSyntaxError"]);
            exposure.ExposureTimeExpression.Definition = "15";
            exposure.Validate().Should().BeTrue();
            exposure.ExposureTimeExpression.Value.Should().Be(15);
            validationPasses[exposure].Should().Be(initialPasses + 4);
        }

        [Test]
        public void NestedDeserialize_CompletesInnerGraphBeforeOuterFactoryResumes() {
            bool innerCompleted = false;
            double innerValue = double.NaN;
            int innerActivations = 0;
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => {
                Load(ForwardReferenceJson(false), fromFile: false);
                innerValue = exposures.Single().ExposureTimeExpression.Value;
                innerActivations = activations.Count;
                innerCompleted = conditions.Single().Data.Offset == -5 && exposures.Single().Issues.Count == 0;
                return new SequentialContainer();
            });
            factory.Setup(x => x.GetContainer<TargetAreaContainer>()).Returns(() => new TargetAreaContainer());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(CreateRoot);

            var outer = Load(ExposureTreeJson(depth: 1, count: 1), fromFile: false);

            innerCompleted.Should().BeTrue();
            innerValue.Should().Be(10);
            innerActivations.Should().Be(1);
            outer.Items.Should().ContainSingle();
            exposures.Should().HaveCount(2);
            exposures.Last().ExposureTimeExpression.Value.Should().Be(10);
            validationPasses[exposures.Last()].Should().BeInRange(1, 2);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Deserialize_ExternalContainerBaseHookKeepsCoreChildrenImmediatelyValidated(bool fromFile) {
            factory.Setup(x => x.GetContainer<LegacyContainer>()).Returns(() => new LegacyContainer());
            var plugin = ContainerJson(typeof(LegacyContainer), "plugin", "area", new JArray(ExposureJson("plugin", "1 +")));
            var area = ContainerJson(typeof(TargetAreaContainer), "area", "root", new JArray(plugin));
            var json = ContainerJson(typeof(SequenceRootContainer), "root", null, new JArray(area)).ToString();

            var loaded = Load(json, fromFile);

            var actual = loaded.Items.Single().Should().BeOfType<TargetAreaContainer>().Subject.Items.Single()
                .Should().BeOfType<LegacyContainer>().Subject;
            actual.ParentChanges.Should().BeGreaterThan(0);
            actual.PreparedAfterBase.Should().OnlyContain(ready => ready).And.NotBeEmpty();
            actual.CoreCallbacksPrepared.Should().HaveCount(actual.ParentChanges * 2).And.OnlyContain(ready => ready);
            actual.Validations.Should().BeGreaterThan(0);
            actual.Issues.Should().Contain("Plugin validation issue");
            actual.Validate().Should().BeFalse();
            actual.Items.Single().Should().BeOfType<TakeExposure>().Subject.Issues.Should().Contain(Loc.Instance["LblSyntaxError"]);
        }

        private static bool RefreshIndependentCoreContainer() {
            var container = new SequentialContainer();
            var condition = new LoopCondition();
            condition.IterationsExpression.Definition = "1 +";
            container.Conditions.Add(condition);
            condition.AttachNewParent(container);
            container.AfterParentChanged();
            return condition.Issues.Contains(Loc.Instance["LblSyntaxError"]);
        }

        private SequenceRootContainer CreateRoot() {
            var root = new SequenceRootContainer();
            roots.Add(root);
            return root;
        }

        private TakeExposure CreateExposure() {
            var exposure = new TakeExposure(profile.Object, camera.Object, Mock.Of<IImagingMediator>(),
                Mock.Of<IImageSaveMediator>(), Mock.Of<IImageHistoryVM>()) { SymbolBroker = broker.Object };
            exposures.Add(exposure);
            ObserveValidation(exposure);
            return exposure;
        }

        private SunAltitudeCondition CreateCondition() => ConfigureCondition(new SunAltitudeCondition(profile.Object));

        private SunAltitudeCondition ConfigureCondition(SunAltitudeCondition condition) {
            condition.SymbolBroker = broker.Object;
            conditions.Add(condition);
            ObserveValidation(condition);
            var watchdog = new Mock<IConditionWatchdog>();
            watchdog.Setup(x => x.Start()).Callback(() => {
                var root = roots.LastOrDefault();
                var parent = condition.Parent;
                activations.Add(new ActivationSnapshot(
                    root != null && parent != null && root.Items.Count == 1 && parent.Items.Count == 2 && ReferenceEquals(parent.Parent, root)
                        && ReferenceEquals(root.Items[0], parent),
                    exposures.Count(item => validationPasses[item] > 0), validationPasses[condition],
                    condition.OffsetExpression.Value, condition.Data.Offset, condition.OffsetExpression.Error));
            }).Returns(Task.CompletedTask);
            condition.ConditionWatchdog = watchdog.Object;
            return condition;
        }

        private void ObserveValidation(ISequenceEntity entity) {
            validationPasses.Add(entity, 0);
            ((INotifyPropertyChanged)entity).PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(IValidatable.Issues)) validationPasses[entity]++;
            };
        }

        private SequenceRootContainer Load(string json, bool fromFile) {
            if (!fromFile) return (SequenceRootContainer)converter.Deserialize(json);
            string path = Path.Combine(directory, "sequence.json");
            File.WriteAllText(path, json);
            return (SequenceRootContainer)converter.DeserializeFromFile(path);
        }

        private static string ForwardReferenceJson(bool definitionFirst) {
            var exposure = ExposureJson("area", "laterValue * 2");
            var constant = new JObject {
                ["$type"] = TypeName(typeof(Constant)),
                ["Expr"] = new JObject { ["Definition"] = "5" },
                ["Identifier"] = "laterValue",
                ["Parent"] = Reference("area")
            };
            var condition = new JObject {
                ["$type"] = TypeName(typeof(SunAltitudeCondition)),
                ["OffsetExpression"] = new JObject { ["Definition"] = "-laterValue" },
                ["Data"] = new JObject { ["Offset"] = 0 },
                ["Parent"] = Reference("area")
            };
            var items = definitionFirst ? new JArray(constant, exposure) : new JArray(exposure, constant);
            var area = ContainerJson(typeof(TargetAreaContainer), "area", "root", items, new JArray(condition));
            return ContainerJson(typeof(SequenceRootContainer), "root", null, new JArray(area)).ToString();
        }

        private static string ExposureTreeJson(int depth, int count) {
            JObject Level(int level) {
                string id = "level" + level;
                var items = level == depth ? new JArray(Enumerable.Range(0, count).Select(_ => ExposureJson(id, "10")))
                    : new JArray(Level(level + 1));
                return ContainerJson(typeof(SequentialContainer), id, level == 1 ? "area" : "level" + (level - 1), items);
            }
            var area = ContainerJson(typeof(TargetAreaContainer), "area", "root", new JArray(Level(1)));
            return ContainerJson(typeof(SequenceRootContainer), "root", null, new JArray(area)).ToString();
        }

        private static JObject ExposureJson(string parent, string definition) => new() {
            ["$type"] = TypeName(typeof(TakeExposure)),
            ["ExposureTimeExpression"] = new JObject { ["Definition"] = definition },
            ["Parent"] = Reference(parent)
        };

        private static JObject ContainerJson(Type type, string id, string? parent, JArray items, JArray? conditions = null) => new() {
            ["$id"] = id,
            ["$type"] = TypeName(type),
            ["Strategy"] = new JObject { ["$type"] = TypeName(typeof(SequentialStrategy)) },
            ["Parent"] = parent == null ? JValue.CreateNull() : Reference(parent),
            ["Conditions"] = new JObject { ["$values"] = conditions ?? new JArray() },
            ["Items"] = new JObject { ["$values"] = items }
        };

        private static JObject Reference(string id) => new() { ["$ref"] = id };
        private static string TypeName(Type type) => type.FullName + ", " + type.Assembly.GetName().Name;
        public class LegacyContainer : SequentialContainer {
            public int ParentChanges { get; private set; }
            public int Validations { get; private set; }
            public List<bool> PreparedAfterBase { get; } = new();
            public List<bool> CoreCallbacksPrepared { get; } = new();
            public override void AfterParentChanged() {
                CoreCallbacksPrepared.Add(RefreshIndependentCoreContainer());
                base.AfterParentChanged();
                CoreCallbacksPrepared.Add(RefreshIndependentCoreContainer());
                ParentChanges++;
                PreparedAfterBase.Add(Items.Count == 1 && Items.OfType<IValidatable>().All(item => item.Issues.Contains(Loc.Instance["LblSyntaxError"])));
            }
            public override bool Validate() {
                base.Validate();
                Validations++;
                Issues = new List<string> { "Plugin validation issue" };
                return false;
            }
        }

        private sealed record ActivationSnapshot(bool GraphComplete, int ValidatedExposures, int ConditionValidations,
            double Value, double Proxy, string? Error);
    }
}