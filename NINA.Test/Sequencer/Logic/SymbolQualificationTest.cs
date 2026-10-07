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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Logic {
    [TestFixture]
    public class SymbolQualificationTest {
        private SymbolBroker broker;

        [SetUp]
        public void SetUp() {
            var profile = new NINA.Profile.Profile { Id = Guid.NewGuid(), Name = "Qualification profile" };
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            broker = new SymbolBroker(profiles.Object,
                Mock.Of<ISwitchMediator>(), Mock.Of<IWeatherDataMediator>(), Mock.Of<ICameraMediator>(),
                Mock.Of<IDomeMediator>(), Mock.Of<IFlatDeviceMediator>(), Mock.Of<IFilterWheelMediator>(),
                Mock.Of<IRotatorMediator>(), Mock.Of<ISafetyMonitorMediator>(), Mock.Of<IFocuserMediator>(),
                Mock.Of<ITelescopeMediator>(), Mock.Of<IGuiderMediator>(), Mock.Of<IImagingMediator>());
        }

        [TearDown]
        public void TearDown() => broker.Dispose();

        [TestCase(".")]
        [TestCase("_")]
        public void QualifiedSymbols_AddUpdateRemove_PreserveEvents(string separator) {
            var provider = broker.RegisterSymbolProvider("Plugin");
            var changes = new List<SymbolChangedEventArgs>();
            provider.SymbolAdded += (_, e) => changes.Add(e);
            provider.SymbolUpdated += (_, e) => changes.Add(e);
            provider.SymbolRemoved += (_, e) => changes.Add(e);
            string name = "Plugin" + separator + "Sensor_Value";

            provider.AddOrUpdateSymbol("Sensor_Value", null);
            broker.TryGetValue(name, out var value).Should().BeTrue();
            value.Should().BeNull();
            provider.AddOrUpdateSymbol("Sensor_Value", 42);
            broker.TryGetSymbol(name, out var symbol).Should().BeTrue();
            symbol.Value.Should().Be(42);
            provider.RemoveSymbol("Sensor_Value").Should().BeTrue();
            broker.TryGetValue(name, out _).Should().BeFalse();
            broker.TryGetSymbol(name, out _).Should().BeFalse();

            changes.Select(x => x.ChangeKind).Should().Equal(SymbolChangeKind.Added, SymbolChangeKind.Updated, SymbolChangeKind.Removed);
            changes.Should().OnlyContain(x => x.QualifiedKey == "Plugin_Sensor_Value");
            SymbolBroker.DELIMITER.Should().Be('_');
        }

        [TestCase(".")]
        [TestCase("_")]
        public void QualifiedHiddenSymbolsAndConstants_AreResolvedAndRemoved(string separator) {
            var provider = broker.RegisterSymbolProvider("Plugin");
            provider.AddOrUpdateHiddenSymbol("State_Value", 1, new[] { new Symbol("State_On", 1) });
            broker.TryGetValue("Plugin" + separator + "State_Value", out var value).Should().BeTrue();
            value.Should().Be(1);
            broker.TryGetValue("Plugin" + separator + "State_On", out value).Should().BeTrue();
            value.Should().Be(1);
            provider.RemoveSymbol("State_Value").Should().BeTrue();
            provider.RemoveSymbol("State_On").Should().BeTrue();
            broker.TryGetValue("Plugin" + separator + "State_Value", out _).Should().BeFalse();
            broker.TryGetValue("Plugin" + separator + "State_On", out _).Should().BeFalse();
        }

        [TestCase("My_Plugin", "Sensor_Value")]
        [TestCase("_Plugin", "_Value")]
        public void DottedNames_PreserveUnderscoresInBothSegments(string providerName, string member) {
            var provider = broker.RegisterSymbolProvider(providerName);
            provider.AddOrUpdateSymbol(member, 7);
            provider.RegisterFunction(Function(providerName, member, 9));
            string name = providerName + "." + member;
            broker.TryGetValue(name, out var value).Should().BeTrue();
            value.Should().Be(7);
            broker.InvokeFunction(name, new EmptyArguments(), out value, out _);
            value.Should().Be(9);
            using var expression = Evaluate(name + " + " + name + "()");
            expression.Value.Should().Be(16);
            expression.References.Should().ContainSingle().Which.Should().Be(name);
        }

        [TestCase("Primary", "MatchValue", true)]
        [TestCase("Match", "MatchValue", true)]
        [TestCase("Missing", "MatchValue", false)]
        [TestCase("primary", "MatchValue", false)]
        [TestCase("Primary", "matchvalue", false)]
        [TestCase("Primary", "Missing", false)]
        [TestCase("Primary", "Shared", true)]
        [TestCase("Missing", "Shared", false)]
        public void SymbolLookup_DotsPreserveLegacyFallbackAndCaseHandling(string source, string key, bool expected) {
            var primary = broker.RegisterSymbolProvider("Primary");
            primary.AddOrUpdateSymbol("MatchValue", 7);
            primary.AddOrUpdateSymbol("Shared", 8);
            broker.RegisterSymbolProvider("Secondary").AddOrUpdateSymbol("Shared", 9);

            bool legacy = broker.TryGetSymbol(source + "_" + key, out var legacySymbol);
            bool dotted = broker.TryGetSymbol(source + "." + key, out var dottedSymbol);
            legacy.Should().Be(expected);
            dotted.Should().Be(legacy);
            dottedSymbol.Should().BeEquivalentTo(legacySymbol);
            broker.TryGetValue(source + "." + key, out var value).Should().Be(legacy);
            if (legacy) value.Should().Be(legacySymbol.Value);
        }

        [TestCase("Primary", "Only", true)]
        [TestCase("Missing", "Only", true)]
        [TestCase("primary", "only", true)]
        [TestCase("Primary", "Missing", false)]
        [TestCase("Primary", "Shared", true)]
        [TestCase("Missing", "Shared", false)]
        [TestCase("primary", "Shared", false)]
        public void FunctionLookup_DotsPreserveLegacyFallbackAndCaseHandling(string source, string key, bool expected) {
            var primary = broker.RegisterSymbolProvider("Primary");
            primary.RegisterFunction(Function("Primary", "Only", 7));
            primary.RegisterFunction(Function("Primary", "Shared", 8));
            broker.RegisterSymbolProvider("Secondary").RegisterFunction(Function("Secondary", "Shared", 9));

            object? legacyValue = null, dottedValue = null;
            Action legacy = () => broker.InvokeFunction(source + "_" + key, new EmptyArguments(), out legacyValue, out _);
            Action dotted = () => broker.InvokeFunction(source + "." + key, new EmptyArguments(), out dottedValue, out _);
            if (expected) {
                legacy.Should().NotThrow();
                dotted.Should().NotThrow();
                dottedValue.Should().Be(legacyValue);
            } else {
                var legacyError = legacy.Should().Throw<ArgumentException>().Which;
                dotted.Should().Throw<ArgumentException>().WithMessage(legacyError.Message);
            }
        }

        [Test]
        public void ExactKeys_TakePriorityOverQualification() {
            var primary = broker.RegisterSymbolProvider("Primary");
            var secondary = broker.RegisterSymbolProvider("Secondary");
            primary.AddOrUpdateSymbol("Value", 7);
            secondary.AddOrUpdateSymbol("Primary_Value", 9);
            primary.RegisterFunction(Function("Primary", "Value", 7));
            secondary.RegisterFunction(Function("Secondary", "Primary_Value", 9));
            secondary.RegisterFunction(Function("Secondary", "Primary.Value", 11));

            broker.TryGetValue("Primary_Value", out var value).Should().BeTrue();
            value.Should().Be(9);
            broker.TryGetValue("Primary.Value", out value).Should().BeTrue();
            value.Should().Be(7);
            broker.InvokeFunction("Primary_Value", new EmptyArguments(), out value, out _);
            value.Should().Be(9);
            broker.InvokeFunction("Primary.Value", new EmptyArguments(), out value, out _);
            value.Should().Be(11);
        }

        [TestCase("Math.Abs(-3) + Math_Abs(-2)", 5)]
        [TestCase("Math.Abs(-1.25) + .5 + 1e-2", 1.76)]
        [TestCase("Math.Abs(-1) in (1, 2)", 1)]
        [TestCase("true ? Math.Abs(-2) : Math.Abs(-3)", 2)]
        [TestCase("-Math.Abs(-2)", -2)]
        public void Expressions_EvaluateDottedFunctionsAndPreserveOtherSyntax(string definition, double expected) {
            using var expression = Evaluate(definition);
            expression.Value.Should().BeApproximately(expected, 1e-10);
        }

        [Test]
        [SetCulture("en-US")]
        public void DateLiterals_WithFractionalSeconds_AreNotRewritten() {
            using var expression = Evaluate("#01/02/2024 03:04:05.123# == #01/02/2024 03:04:05.123# ? Math.Abs(-2) : 0");
            expression.Value.Should().Be(2);
        }

        [Test]
        public void DottedFunctions_PreserveVolatility() {
            using var expression = Evaluate("Time.Now()");
            expression.GlobalVolatile.Should().BeTrue();
            expression.Value.Should().BeGreaterThan(0);
        }

        [TestCase("Logic.If(true, Math.Abs(-3), Plugin.Explode())")]
        [TestCase("Logic.If(false, Plugin.Explode(), Math.Abs(-3))")]
        [TestCase("Logic.Ifs(false, Plugin.Explode(), true, Math.Abs(-3), Plugin.Explode())")]
        public void DottedConditionalFunctions_PreserveLazyArguments(string definition) {
            broker.RegisterSymbolProvider("Plugin").RegisterFunction(new SymbolFunction(
                "Explode", "Plugin", "", "", _ => throw new InvalidOperationException("Unselected branch")));
            using var expression = Evaluate(definition);
            expression.Value.Should().Be(3);
        }

        [TestCase("[NINA.ProfileName]")]
        [TestCase("{NINA.ProfileName}")]
        public void EscapedNames_RemainIntact(string name) {
            using var expression = Evaluate("StrConcat(" + name + ", NINA.ProfileName)");
            expression.StringValue.Should().Be("Qualification profileQualification profile");
        }

        [TestCase("'NINA.ProfileName'", "NINA.ProfileName")]
        [TestCase("\"NINA.ProfileName\"", "NINA.ProfileName")]
        [TestCase("'it\\'s NINA.ProfileName'", "it's NINA.ProfileName")]
        [TestCase("\"say \\\"NINA.ProfileName\\\"\"", "say \"NINA.ProfileName\"")]
        public void QuotedText_IsNotRewritten(string literal, string expected) {
            using var expression = Evaluate("StrConcat(" + literal + ", NINA.ProfileName)");
            expression.StringValue.Should().Be(expected + "Qualification profile");
        }

        [TestCase("Camera..Temperature")]
        [TestCase("Camera.Temperature.More")]
        [TestCase(".Camera.Temperature")]
        [TestCase("Camera.Temperature.")]
        [TestCase("1Camera.Temperature")]
        [TestCase("Camera.")]
        [TestCase("Camera. Temperature")]
        [TestCase("'unterminated NINA.ProfileName")]
        public void MalformedDotsAndLiterals_RemainSyntaxErrors(string definition) {
            using var expression = CreateExpression(definition);
            expression.HasError.Should().BeTrue();
            expression.Error.Should().Be(NINA.Core.Locale.Loc.Instance["LblSyntaxError"]);
        }

        [Test]
        public void DottedDefinitions_PreserveTextThroughCloneSerializationAndExpansion() {
            const string definition = "StrConcat(NINA.ProfileName, StrConcat(' / ', NINA_ProfileName))";
            using var expression = Evaluate(definition);
            using var clone = new Expression(expression, expression.Context);
            clone.SymbolBroker = broker;
            clone.Evaluate(true);
            clone.Definition.Should().Be(definition);
            clone.StringValue.Should().Be(expression.StringValue);
            using var restored = JsonConvert.DeserializeObject<Expression>(JsonConvert.SerializeObject(expression))!;
            restored.Definition.Should().Be(definition);
            restored.Context = expression.Context;
            restored.SymbolBroker = broker;
            restored.Evaluate(true);
            restored.StringValue.Should().Be(expression.StringValue);
            ExpressionExpander.Expand("Profile: {NINA.ProfileName}", broker, new SequenceRootContainer())
                .Should().Be("Profile: Qualification profile");
        }

        [Test]
        public void SeparateExpressions_DoNotShareRewrittenNamesOrCachedValues() {
            var provider = broker.RegisterSymbolProvider("Plugin");
            for (int i = 0; i < 12; i++) provider.AddOrUpdateSymbol("Value" + i, i);
            Parallel.For(0, 12, i => {
                using var expression = Evaluate("Math.Abs(Plugin.Value" + i + ")");
                expression.Value.Should().Be(i);
                expression.References.Should().ContainSingle().Which.Should().Be("Plugin.Value" + i);
                expression.Evaluate(true);
                expression.Value.Should().Be(i);
            });
        }

        [Test]
        public void DottedReferences_RefreshValuesAndKeepOriginalNamesInDiagnostics() {
            var provider = broker.RegisterSymbolProvider("Plugin");
            provider.AddOrUpdateSymbol("Value", 1);
            using var expression = Evaluate("Plugin.Value + Plugin_Value");
            expression.Value.Should().Be(2);
            provider.AddOrUpdateSymbol("Value", 3);
            expression.Evaluate(true);
            expression.Value.Should().Be(6);
            provider.RemoveSymbol("Value");
            expression.Evaluate(true);
            expression.Error.Should().Contain("Plugin.Value").And.Contain("Plugin_Value");
        }

        [TestCase("Shared")]
        [TestCase("Missing.Shared")]
        public void AmbiguitySuggestions_UseDottedNames(string definition) {
            broker.RegisterSymbolProvider("First_Plugin").AddOrUpdateSymbol("Shared", 1);
            broker.RegisterSymbolProvider("Second_Plugin").AddOrUpdateSymbol("Shared", 2);
            using var expression = CreateExpression(definition);
            expression.Evaluate(true);
            expression.Error.Should().Contain("First_Plugin.Shared").And.Contain("Second_Plugin.Shared");
        }

        [Test]
        public void AdapterIdentifiers_DoNotCollideWithUserNames() {
            var provider = broker.RegisterSymbolProvider("Plugin");
            provider.AddOrUpdateSymbol("Value", 1);
            provider.AddOrUpdateSymbol("__nina_qualified_0", 10);
            using var expression = Evaluate("Plugin.Value + __nina_qualified_0 + [__nina_qualified_0]");
            expression.Value.Should().Be(21);
            expression.References.Should().BeEquivalentTo(new[] { "Plugin.Value", "__nina_qualified_0" });
        }

        private Expression CreateExpression(string definition) {
            var expression = new Expression { Context = new SequenceRootContainer(), SymbolBroker = broker };
            expression.Definition = definition;
            return expression;
        }

        private Expression Evaluate(string definition) {
            var expression = CreateExpression(definition);
            expression.Evaluate(true);
            expression.Error.Should().BeNull(definition);
            return expression;
        }

        private static SymbolFunction Function(string category, string key, int value) =>
            new SymbolFunction(key, category, "", "", _ => value);

        private sealed class EmptyArguments : ISymbolFunctionArguments {
            public int Count => 0;
            public object Evaluate(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
