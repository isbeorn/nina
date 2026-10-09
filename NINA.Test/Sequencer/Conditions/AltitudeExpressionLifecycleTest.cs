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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.CustomControlLibrary;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Serialization;
using NINA.Test.Sequencer.Editing;
using System.Globalization;
using System.Windows.Controls;

namespace NINA.Test.Sequencer.Conditions {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class AltitudeExpressionLifecycleTest {
        private Mock<IProfileService> profile = null!;
        private Mock<ISymbolBroker> broker = null!;
        private SequenceJsonConverter converter = null!;
        private readonly List<SequenceContainer> roots = new();

        [SetUp]
        public void SetUp() {
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
            profile = new Mock<IProfileService>();
            profile.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profile.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
            broker = new Mock<ISymbolBroker>();
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new List<ISequenceEntityUpgrader>());
            // Register base types before derived types because Moq matches assignable generic arguments.
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => new SequentialContainer());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => new SequenceRootContainer());
            factory.Setup(x => x.GetItem<Variable>()).Returns(() => (Variable)new Variable { SymbolBroker = broker.Object }.Clone());
            factory.Setup(x => x.GetItem<GlobalVariable>()).Returns(() => (GlobalVariable)new GlobalVariable { SymbolBroker = broker.Object }.Clone());
            factory.Setup(x => x.GetItem<GlobalConstant>()).Returns(() => (GlobalConstant)new GlobalConstant { SymbolBroker = broker.Object }.Clone());
            factory.Setup(x => x.GetCondition<LoopWhile>()).Returns(() => Prepare((LoopWhile)new LoopWhile().Clone()));
            factory.Setup(x => x.GetCondition<SunAltitudeCondition>()).Returns(() => Prepare((SunAltitudeCondition)new SunAltitudeCondition(profile.Object).Clone()));
            factory.Setup(x => x.GetCondition<MoonAltitudeCondition>()).Returns(() => Prepare((MoonAltitudeCondition)new MoonAltitudeCondition(profile.Object).Clone()));
            converter = new SequenceJsonConverter(factory.Object);
        }

        [TearDown]
        public void TearDown() {
            foreach (SequenceContainer root in roots) {
                foreach (var item in root.GetItemsSnapshot()) item.Detach();
                foreach (var condition in root.GetConditionsSnapshot()) condition.Detach();
            }
            roots.Clear();
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
        }

        [TestCase("dc", false, -12)]
        [TestCase("dc", true, -12)]
        [TestCase("dv", false, -15)]
        [TestCase("dv", true, -15)]
        [TestCase("dsv", false, -18)]
        [TestCase("dsv", true, -18)]
        public async Task DeserializeAndCheck_ResolvesSymbols(string symbol, bool moon, double expected) {
            SequenceContainer root = LoadFixture();
            root.Validate().Should().BeTrue("unexecuted variables are warnings during preflight");
            LoopForSunMoonAltitudeBase condition = FindCondition(root, symbol, moon);
            if (symbol == "dc") {
                AssertAltitude(condition, expected);
            } else {
                OffsetExpression(condition).Error.Should().NotBeNullOrEmpty();
                Expression.JustWarnings(OffsetExpression(condition).Error).Should().BeTrue();
                condition.Issues.Should().BeEmpty();
            }
            await InitializeVariables(root);

            ((SequenceContainer)condition.Parent).Conditions.OfType<LoopWhile>().Single().Check(null, null).Should().BeTrue();
            condition.Check(null, null);

            AssertAltitude(condition, expected);
        }

        [TestCase("dv", false)]
        [TestCase("dv", true)]
        [TestCase("dsv", false)]
        [TestCase("dsv", true)]
        public async Task SetVariableAndReset_RefreshesAltitude(string symbol, bool moon) {
            SequenceContainer root = LoadFixture();
            await InitializeVariables(root);
            LoopForSunMoonAltitudeBase condition = FindCondition(root, symbol, moon);
            condition.Check(null, null);
            Variable variable = Containers(root).SelectMany(c => c.Items).OfType<Variable>().Single(v => v.Identifier == symbol);
            var setVariable = new ResetVariable { SymbolBroker = broker.Object, Variable = symbol };
            condition.Parent.Add(setVariable);
            setVariable.Expr.Definition = "-25";
            await setVariable.Execute(null, CancellationToken.None);

            condition.Check(null, null);
            AssertAltitude(condition, -25);

            root.ResetProgress();
            variable.OriginalDefinition = "-22";
            await variable.Execute(null, CancellationToken.None);
            condition.Check(null, null);
            AssertAltitude(condition, -22);
        }

        [TestCase("dc", false, -12)]
        [TestCase("dc", true, -12)]
        [TestCase("dv", false, -15)]
        [TestCase("dv", true, -15)]
        [TestCase("dsv", false, -18)]
        [TestCase("dsv", true, -18)]
        public async Task CloneAndReload_PreservesSymbolDefinitions(string symbol, bool moon, double expected) {
            SequenceContainer root = LoadFixture();
            await InitializeVariables(root);
            LoopForSunMoonAltitudeBase original = FindCondition(root, symbol, moon);
            original.Check(null, null);
            var clone = Prepare((LoopForSunMoonAltitudeBase)original.Clone());
            ((SequenceContainer)original.Parent).Add(clone);
            clone.Check(null, null);
            AssertAltitude(clone, expected);
            OffsetExpression(clone).Should().NotBeSameAs(OffsetExpression(original));
            OffsetExpression(clone).Definition.Should().Be(symbol);
            clone.Detach();

            string json = converter.Serialize(root);
            foreach (var item in root.GetItemsSnapshot()) item.Detach();
            UserSymbol.ClearUserSymbols();
            SequenceContainer reloaded = Load(json);
            await InitializeVariables(reloaded);
            LoopForSunMoonAltitudeBase condition = FindCondition(reloaded, symbol, moon);
            condition.Check(null, null);
            AssertAltitude(condition, expected);
            OffsetExpression(condition).Definition.Should().Be(symbol);
        }

        [TestCase(typeof(SunAltitudeCondition))]
        [TestCase(typeof(MoonAltitudeCondition))]
        [TestCase(typeof(AltitudeCondition))]
        [TestCase(typeof(AboveHorizonCondition))]
        public void ContainerValidation_RejectsErrorsAndRecovers(Type type) {
            LoopForAltitudeBase condition = CreateCondition(type);
            SequenceRootContainer root = CreateRoot();
            root.Add(condition);
            Expression expression = OffsetExpression(condition);
            foreach (string invalid in new[] { "1 +", "missingAltitude", "-91", "91" }) {
                expression.Definition = invalid;
                root.Validate().Should().BeFalse(invalid);
                root.Validate().Should().BeFalse(invalid);
                condition.Issues.Should().ContainSingle();
                condition.RunCheck(null, null).Should().BeFalse();
                condition.Status.Should().Be(SequenceEntityStatus.FAILED);

                expression.Definition = "-12";
                root.Validate().Should().BeTrue();
                condition.Issues.Should().BeEmpty();
                condition.ResetProgress();
                condition.RunCheck(null, null);
                condition.Status.Should().NotBe(SequenceEntityStatus.FAILED);
            }
        }

        [TestCase(typeof(SunAltitudeCondition))]
        [TestCase(typeof(MoonAltitudeCondition))]
        [TestCase(typeof(AltitudeCondition))]
        [TestCase(typeof(AboveHorizonCondition))]
        public void CalculateExpectedTime_RefreshesLiveSymbolWithoutBackgroundValidation(Type type) {
            double altitude = -12;
            broker.Setup(b => b.TryGetValue("liveAltitude", out It.Ref<object>.IsAny))
                .Returns((string name, out object value) => { value = altitude; return true; });
            LoopForAltitudeBase condition = CreateCondition(type);
            CreateRoot().Add(condition);
            OffsetExpression(condition).SymbolBroker = broker.Object;
            OffsetExpression(condition).Definition = "liveAltitude";
            condition.CalculateExpectedTime();
            condition.Data.Offset.Should().Be(-12);

            foreach (double next in new[] { -18.0, -10.0 }) {
                altitude = next;
                condition.CalculateExpectedTime();
                condition.Data.Offset.Should().Be(next);
            }
        }

        [TestCase(false, -90)]
        [TestCase(false, -12)]
        [TestCase(false, 0)]
        [TestCase(false, 90)]
        [TestCase(true, -90)]
        [TestCase(true, -12)]
        [TestCase(true, 0)]
        [TestCase(true, 90)]
        public void Comparisons_PreserveBoundariesAndApparentHorizon(bool moon, double target) {
            var condition = (LoopForSunMoonAltitudeBase)CreateCondition(moon ? typeof(MoonAltitudeCondition) : typeof(SunAltitudeCondition));
            OffsetExpression(condition).Definition = target.ToString(CultureInfo.InvariantCulture);
            SequenceRootContainer root = CreateRoot();
            root.Add(condition);
            root.Validate().Should().BeTrue();
            double effectiveTarget = target != 0 ? target : -(moon ? AstroUtil.MoonUpperLimbApparentHorizonAltitude : AstroUtil.SunUpperLimbApparentHorizonAltitude);

            foreach (double current in new[] { effectiveTarget - 0.01, effectiveTarget, effectiveTarget + 0.01 }) {
                condition.Data.CurrentAltitude = current;
                condition.Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;
                condition.Check(null, null, true).Should().Be(condition.Data.CurrentAltitude <= effectiveTarget);
                condition.Data.Comparator = ComparisonOperatorEnum.LESS_THAN;
                condition.Check(null, null, true).Should().Be(condition.Data.CurrentAltitude > effectiveTarget);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WaitInstructions_AlreadyParticipateInContainerValidation(bool moon) {
            WaitForAltitudeBase item = moon ? new WaitForMoonAltitude(profile.Object) : new WaitForSunAltitude(profile.Object);
            CreateRoot().Add(item);
            Expression expression = moon ? ((WaitForMoonAltitude)item).OffsetExpression : ((WaitForSunAltitude)item).OffsetExpression;
            expression.Definition = "1 +";
            ((SequenceContainer)item.Parent).Validate().Should().BeFalse();
            item.Issues.Should().Contain(expression.Error);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompiledTemplate_DisplaysResolvedValueAndSymbolTooltip(bool moon) {
            using var scope = new CoreEditorTestScope();
            var constant = new GlobalConstant { SymbolBroker = broker.Object };
            constant.Expr = new Expression("-12", constant);
            scope.Root.Add(constant);
            constant.Identifier = "dc";
            LoopForAltitudeBase condition = CreateCondition(moon ? typeof(MoonAltitudeCondition) : typeof(SunAltitudeCondition));
            OffsetExpression(condition).Definition = "dc";

            scope.Show(condition);

            ExprControl control = CoreEditorTestScope.Descendants<ExprControl>(scope.Host).Single();
            control.GetValue(ExprControl.ExpProperty).Should().BeSameAs(OffsetExpression(condition));
            HintTextBox text = CoreEditorTestScope.Descendants<HintTextBox>(control).Single();
            text.Text.Should().Be("dc");
            CoreEditorTestScope.Descendants<TextBlock>(control).Should().Contain(t => t.Text == "{-12}");
            UserSymbol.ShowSymbols(text);
            text.ToolTip.Should().BeOfType<string>().Which.Should().Contain("dc").And.Contain("-12").And.NotContain("not yet defined");
        }

        [TestCase(typeof(AltitudeCondition))]
        [TestCase(typeof(AboveHorizonCondition))]
        [TestCase(typeof(WaitForAltitude))]
        [TestCase(typeof(WaitUntilAboveHorizon))]
        public void CompiledTemplate_RoundsAltitudeOnlyForDisplay(Type type) {
            using var scope = new CoreEditorTestScope();
            var entity = (ISequenceEntity)Activator.CreateInstance(type, profile.Object)!;
            scope.Show(entity);
            var data = entity is LoopForAltitudeBase condition
                ? condition.Data
                : ((NINA.Sequencer.SequenceItem.Telescope.CoordinatesInstruction)entity).Data;
            data.CurrentAltitude = 29.996;
            data.TargetAltitude = 30.004;
            CoreEditorTestScope.Drain();

            var altitudeText = CoreEditorTestScope.Descendants<TextBlock>(scope.Host)
                .Single(t => System.Windows.Data.BindingOperations.GetBinding(t, TextBlock.TextProperty)?.Path.Path == "CurrentAltitude");
            altitudeText.Text.Should().Be("30");
            var targetText = CoreEditorTestScope.Descendants<TextBlock>(scope.Host)
                .Single(t => System.Windows.Data.BindingOperations.GetBinding(t, TextBlock.TextProperty)?.Path.Path == "TargetAltitude");
            targetText.Text.Should().Be(30d.ToString(entity is LoopForAltitudeBase ? "0.##" : "N2", targetText.Language.GetSpecificCulture()));
            data.CurrentAltitude.Should().Be(29.996);
            data.TargetAltitude.Should().Be(30.004);
        }

        private T Prepare<T>(T condition) where T : SequenceCondition {
            condition.SymbolBroker = broker.Object;
            condition.ConditionWatchdog = Mock.Of<IConditionWatchdog>();
            return condition;
        }

        private LoopForAltitudeBase CreateCondition(Type type) => Prepare((LoopForAltitudeBase)Activator.CreateInstance(type, profile.Object)!);

        private SequenceRootContainer CreateRoot() {
            var root = new SequenceRootContainer();
            roots.Add(root);
            return root;
        }

        private SequenceContainer LoadFixture() => Load(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Sequencer", "Serialization", "LegacySequences", "AltitudeSymbols.json")));

        private SequenceContainer Load(string json) {
            var root = (SequenceContainer)converter.Deserialize(json);
            roots.Add(root);
            return root;
        }

        private static async Task InitializeVariables(SequenceContainer root) {
            foreach (Variable variable in Containers(root).SelectMany(c => c.Items).OfType<Variable>()) {
                await variable.Execute(null, CancellationToken.None);
            }
        }

        private static LoopForSunMoonAltitudeBase FindCondition(SequenceContainer root, string symbol, bool moon) =>
            Containers(root).SelectMany(c => c.Conditions).OfType<LoopForSunMoonAltitudeBase>()
                .Single(c => (c is MoonAltitudeCondition) == moon && OffsetExpression(c).Definition == symbol);

        private static Expression OffsetExpression(LoopForAltitudeBase condition) => condition switch {
            SunAltitudeCondition sun => sun.OffsetExpression,
            MoonAltitudeCondition moon => moon.OffsetExpression,
            AltitudeCondition altitude => altitude.OffsetExpression,
            AboveHorizonCondition horizon => horizon.OffsetExpression,
            _ => throw new ArgumentException(nameof(condition))
        };

        private static void AssertAltitude(LoopForSunMoonAltitudeBase condition, double expected) {
            OffsetExpression(condition).Error.Should().BeNull();
            OffsetExpression(condition).Value.Should().Be(expected);
            OffsetExpression(condition).ValueString.Should().Be(expected.ToString(CultureInfo.InvariantCulture));
            condition.Data.Offset.Should().Be(expected);
            condition.Data.TargetAltitude.Should().Be(expected);
        }

        private static IEnumerable<SequenceContainer> Containers(SequenceContainer root) {
            yield return root;
            foreach (SequenceContainer item in root.Items.OfType<SequenceContainer>()) {
                foreach (SequenceContainer child in Containers(item)) yield return child;
            }
        }
    }
}
