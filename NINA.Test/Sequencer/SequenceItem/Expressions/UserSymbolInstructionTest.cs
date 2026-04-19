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
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SequencerExpression = NINA.Sequencer.Logic.Expression;

namespace NINA.Test.Sequencer.SequenceItem.Expressions {

    [TestFixture]
    public class UserSymbolInstructionTest {
        private Mock<ISymbolBroker> symbolBrokerMock;

        [SetUp]
        public void SetUp() {
            symbolBrokerMock = new Mock<ISymbolBroker>();
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
        }

        [TearDown]
        public void TearDown() {
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
        }

        /// <summary>
        /// Verifies the Variable Execute Evaluates Original Definition And Can Be Reset scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task Variable_Execute_EvaluatesOriginalDefinitionAndCanBeReset() {
            SequenceRootContainer root = CreateRoot();
            Variable variable = CreateVariable("target", "40 + 2", root);

            variable.Validate().Should().BeTrue();

            await variable.Execute(default, CancellationToken.None);

            variable.Executed.Should().BeTrue();
            variable.Expr.Definition.Should().Be("40 + 2");
            variable.Expr.Evaluate(ignoreRoot: true);
            variable.Expr.Value.Should().Be(42);

            variable.ResetProgress();

            variable.Executed.Should().BeFalse();
            variable.Expr.Definition.Should().BeEmpty();
            variable.Expr.IsExpression.Should().BeFalse();
        }

        /// <summary>
        /// Verifies the variable convenience constructor, original definition setter, cloning, and diagnostic text preserve the scoped expression state.
        /// </summary>
        [Test]
        public void Variable_ConstructorCloneAndToString_PreserveScopedExpressionState() {
            SequenceRootContainer root = CreateRoot();
            Variable sut = new Variable("target", "40 + 2", root) {
                SymbolBroker = symbolBrokerMock.Object
            };

            sut.OriginalDefinition = "20 + 22";
            Variable clone = (Variable)sut.Clone();

            sut.Executed.Should().BeTrue();
            sut.Expr.Value.Should().Be(42);
            sut.OriginalDefinition.Should().Be("20 + 22");
            sut.ToString().Should().Contain("target").And.Contain("40 + 2");
            clone.Should().NotBeSameAs(sut);
            clone.Identifier.Should().Be("target");
            clone.Expr.Should().NotBeSameAs(sut.Expr);
            clone.OriginalExpr.Should().NotBeSameAs(sut.OriginalExpr);
        }

        /// <summary>
        /// Verifies executing a global variable updates the attached global cache entry so expressions see the latest evaluated definition.
        /// </summary>
        [Test]
        public async Task GlobalVariable_Execute_UpdatesAttachedGlobalSymbol() {
            SequenceRootContainer root = CreateRoot();
            GlobalVariable sut = new GlobalVariable("globalTarget", "1", root) {
                SymbolBroker = symbolBrokerMock.Object
            };
            sut.OriginalExpr = CreateExpression("40 + 2", sut);
            sut.Executed = false;

            await sut.Execute(default, CancellationToken.None);

            UserSymbol.FindGlobalSymbol("globalTarget").Should().BeSameAs(sut);
            sut.Executed.Should().BeTrue();
            sut.Expr.Definition.Should().Be("40 + 2");
            sut.Expr.Value.Should().Be(42);
            ((GlobalVariable)sut.Clone()).ToString().Should().Contain("globalTarget");
        }

        /// <summary>
        /// Verifies the Variable Validate Rejects Missing Name Invalid Name And Missing Definition scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Variable_Validate_RejectsMissingNameInvalidNameAndMissingDefinition() {
            SequenceRootContainer root = CreateRoot();
            Variable variable = CreateVariable("validName", "", root);

            variable.Validate().Should().BeFalse();
            variable.Issues.Should().Contain(i => i.Contains("initial value"));

            variable.OriginalExpr.Definition = "1";
            variable.Identifier = "1 invalid";

            variable.Validate().Should().BeFalse();
            variable.Issues.Should().Contain(i => i.Contains("alphanumeric"));
        }

        /// <summary>
        /// Verifies constants clone their expression, reject invalid definitions, reject variable references, and remain a no-op during execution.
        /// </summary>
        [Test]
        public async Task Constant_CloneValidateAndExecute_CoverConstantSpecificRules() {
            SequenceRootContainer root = CreateRoot();
            Variable variable = CreateVariable("target", "1", root);
            await variable.Execute(default, CancellationToken.None);
            Constant sut = AddConstant("constantTarget", "target + 1", root);
            sut.Expr.Evaluate();

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().Contain(i => i.Contains("may not include Variables"));

            sut.Expr = CreateExpression("2 + 3", sut);
            sut.Validate().Should().BeTrue();

            sut.Identifier = "1 invalid";
            sut.Validate().Should().BeFalse();
            sut.Issues.Should().Contain(i => i.Contains("alphanumeric"));

            sut.Identifier = "constantTarget";
            sut.Validate().Should().BeTrue();

            Constant clone = (Constant)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Identifier.Should().Be("constantTarget");
            clone.Expr.Should().NotBeSameAs(sut.Expr);
            clone.Expr.Definition.Should().Be("2 + 3");
            sut.ToString().Should().Contain("constantTarget").And.Contain("2 + 3");
            await sut.Execute(default, CancellationToken.None);

            Constant detached = new Constant {
                Identifier = "detached",
                Expr = CreateExpression("1", null)
            };
            detached.Validate().Should().BeTrue();
        }

        /// <summary>
        /// Verifies global constants use the global symbol contract while preserving clone metadata and diagnostic text.
        /// </summary>
        [Test]
        public void GlobalConstant_CloneAndToString_PreserveGlobalExpressionState() {
            SequenceRootContainer root = CreateRoot();
            GlobalConstant sut = new GlobalConstant {
                SymbolBroker = symbolBrokerMock.Object,
                Expr = CreateExpression("10 + 5", null)
            };
            root.Add(sut);
            sut.Identifier = "globalConstant";

            GlobalConstant clone = (GlobalConstant)sut.Clone();

            UserSymbol.FindGlobalSymbol("globalConstant").Should().BeSameAs(sut);
            clone.Should().NotBeSameAs(sut);
            clone.Identifier.Should().Be("globalConstant");
            clone.Expr.Should().NotBeSameAs(sut.Expr);
            clone.Expr.Definition.Should().Be("10 + 5");
            sut.ToString().Should().Contain("Global Constant").And.Contain("globalConstant");
        }

        /// <summary>
        /// Verifies the Reset Variable Execute Updates Executed Variable And Marks Consumers Dirty scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ResetVariable_Execute_UpdatesExecutedVariableAndMarksConsumersDirty() {
            SequenceRootContainer root = CreateRoot();
            Variable variable = CreateVariable("target", "1", root);
            await variable.Execute(default, CancellationToken.None);

            SequencerExpression consumer = CreateExpression("target + 1", root);
            consumer.Evaluate();
            variable.AddConsumer(consumer);

            ResetVariable sut = CreateResetVariable("target", "20 + 22", root);

            sut.Validate().Should().BeTrue();
            await sut.Execute(default, CancellationToken.None);

            variable.Expr.Definition.Should().Be("42");
            consumer.Dirty.Should().BeTrue();
        }

        /// <summary>
        /// Verifies the Reset Variable Execute Preserves String Result As Quoted Definition scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ResetVariable_Execute_PreservesStringResultAsQuotedDefinition() {
            SequenceRootContainer root = CreateRoot();
            Variable variable = CreateVariable("filterName", "\"L\"", root);
            await variable.Execute(default, CancellationToken.None);

            ResetVariable sut = CreateResetVariable("filterName", "\"Ha\"", root);

            sut.Validate().Should().BeTrue();
            await sut.Execute(default, CancellationToken.None);

            variable.Expr.Definition.Should().Be("'Ha'");
        }

        /// <summary>
        /// Verifies the Reset Variable Validate Rejects Constant Targets And Missing Targets scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ResetVariable_Validate_RejectsConstantTargetsAndMissingTargets() {
            SequenceRootContainer root = CreateRoot();
            AddConstant("constantTarget", "1", root);

            ResetVariable constantTarget = CreateResetVariable("constantTarget", "2", root);

            constantTarget.Validate().Should().BeFalse();
            constantTarget.Issues.Should().Contain(i => i.Contains("Constant"));

            ResetVariable missingTarget = CreateResetVariable("missingTarget", "2", root);

            missingTarget.Validate().Should().BeFalse();
            missingTarget.Issues.Should().Contain(i => i.Contains("not in scope"));
        }

        /// <summary>
        /// Verifies the Reset Variable Execute Throws When Variable Has Not Executed scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ResetVariable_Execute_ThrowsWhenVariableHasNotExecuted() {
            SequenceRootContainer root = CreateRoot();
            CreateVariable("target", "1", root);
            ResetVariable sut = CreateResetVariable("target", "2", root);

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceEntityFailedException>()
                .WithMessage("*has not been executed*");
        }

        /// <summary>
        /// Verifies the Reset Variable To Date Execute Uses Fixed Provider Time With Offset scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ResetVariableToDate_Execute_UsesFixedProviderTimeWithOffset() {
            SequenceRootContainer root = CreateRoot();
            Variable variable = CreateVariable("targetTime", "0", root);
            await variable.Execute(default, CancellationToken.None);

            DateTime providerTime = new DateTime(2026, 4, 16, 1, 2, 3, DateTimeKind.Local);
            Mock<IDateTimeProvider> provider = CreateDateTimeProvider(providerTime);
            ResetVariableToDate sut = new ResetVariableToDate(new List<IDateTimeProvider> { provider.Object }) {
                Variable = "targetTime",
                MinutesOffset = 15
            };
            root.Add(sut);

            sut.Validate().Should().BeTrue();
            await sut.Execute(default, CancellationToken.None);

            DateTime expectedTime = providerTime.AddMinutes(15);
            long expectedUnixTime = ((DateTimeOffset)expectedTime).ToUnixTimeSeconds();
            variable.Expr.Definition.Should().Be(expectedUnixTime.ToString());
        }

        /// <summary>
        /// Verifies the Reset Variable To Date Validate Reports Invalid Fixed Provider scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ResetVariableToDate_Validate_ReportsInvalidFixedProvider() {
            SequenceRootContainer root = CreateRoot();
            CreateVariable("targetTime", "0", root).Executed = true;

            Mock<IDateTimeProvider> provider = new Mock<IDateTimeProvider>();
            provider.SetupGet(x => x.Name).Returns("Broken Provider");
            provider.Setup(x => x.GetDateTime(It.IsAny<ISequenceEntity>())).Throws(new InvalidOperationException("invalid time"));

            ResetVariableToDate sut = new ResetVariableToDate(new List<IDateTimeProvider> { provider.Object }) {
                Variable = "targetTime"
            };
            root.Add(sut);

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();
        }

        /// <summary>
        /// Verifies manual ResetVariableToDate validation computes the selected time, rejects illegal variables, and blocks execution while invalid.
        /// </summary>
        [Test]
        public async Task ResetVariableToDate_ManualTimeValidation_ComputesUnixTimeAndBlocksInvalidExecution() {
            SequenceRootContainer root = CreateRoot();
            ResetVariableToDate sut = new ResetVariableToDate(new List<IDateTimeProvider>()) {
                Variable = "invalid name",
                Hours = 1,
                Minutes = 2,
                Seconds = 3
            };
            root.Add(sut);

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().Contain(i => i.Contains("not a legal Variable name"));
            sut.TimeString.Should().NotBe("Not Set");
            sut.ToString().Should().Contain(nameof(ResetVariable));

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceEntityFailedException>()
                .WithMessage("*invalid*");
        }

        /// <summary>
        /// Verifies the Clone Copies Reset Instructions Without Sharing Expression Instances scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesResetInstructionsWithoutSharingExpressionInstances() {
            ResetVariable resetVariable = new ResetVariable {
                Variable = "target",
                Expr = CreateExpression("1 + 2", null)
            };
            ResetVariable resetVariableClone = (ResetVariable)resetVariable.Clone();

            resetVariableClone.Should().NotBeSameAs(resetVariable);
            resetVariableClone.Variable.Should().Be(resetVariable.Variable);
            resetVariableClone.Expr.Should().NotBeSameAs(resetVariable.Expr);
            resetVariableClone.Expr.Definition.Should().Be(resetVariable.Expr.Definition);

            Mock<IDateTimeProvider> provider = CreateDateTimeProvider(new DateTime(2026, 4, 16, 1, 2, 3, DateTimeKind.Local));
            ResetVariableToDate resetVariableToDate = new ResetVariableToDate(new List<IDateTimeProvider> { provider.Object }) {
                Variable = "target",
                Hours = 1,
                Minutes = 2,
                Seconds = 3,
                MinutesOffset = 4
            };
            ResetVariableToDate resetVariableToDateClone = (ResetVariableToDate)resetVariableToDate.Clone();

            resetVariableToDateClone.Should().NotBeSameAs(resetVariableToDate);
            resetVariableToDateClone.Variable.Should().Be(resetVariableToDate.Variable);
            resetVariableToDateClone.Expr.Should().NotBeSameAs(resetVariableToDate.Expr);
            resetVariableToDateClone.Expr.Definition.Should().Be(resetVariableToDate.Expr.Definition);
            resetVariableToDateClone.Hours.Should().Be(resetVariableToDate.Hours);
            resetVariableToDateClone.Minutes.Should().Be(resetVariableToDate.Minutes);
            resetVariableToDateClone.Seconds.Should().Be(resetVariableToDate.Seconds);
            resetVariableToDateClone.MinutesOffset.Should().Be(resetVariableToDate.MinutesOffset);
        }

        private SequenceRootContainer CreateRoot() {
            return new SequenceRootContainer {
                SymbolBroker = symbolBrokerMock.Object
            };
        }

        private Variable CreateVariable(string identifier, string definition, ISequenceContainer parent) {
            Variable variable = new Variable {
                SymbolBroker = symbolBrokerMock.Object
            };
            variable.Expr = CreateExpression("", variable);
            variable.OriginalExpr = CreateExpression(definition, variable);
            parent.Add(variable);
            variable.Identifier = identifier;
            return variable;
        }

        private Constant AddConstant(string identifier, string definition, ISequenceContainer parent) {
            Constant constant = new Constant {
                SymbolBroker = symbolBrokerMock.Object
            };
            constant.Expr = CreateExpression(definition, constant);
            parent.Add(constant);
            constant.Identifier = identifier;
            return constant;
        }

        private ResetVariable CreateResetVariable(string variable, string definition, ISequenceContainer parent) {
            ResetVariable resetVariable = new ResetVariable {
                SymbolBroker = symbolBrokerMock.Object,
                Variable = variable,
                Expr = CreateExpression(definition, null)
            };
            parent.Add(resetVariable);
            resetVariable.Expr.SymbolBroker = symbolBrokerMock.Object;
            return resetVariable;
        }

        private SequencerExpression CreateExpression(string definition, ISequenceEntity context) {
            return new SequencerExpression(definition, context) {
                SymbolBroker = symbolBrokerMock.Object,
                IsExpression = true
            };
        }

        private static Mock<IDateTimeProvider> CreateDateTimeProvider(DateTime dateTime) {
            Mock<IDateTimeProvider> provider = new Mock<IDateTimeProvider>();
            provider.SetupGet(x => x.Name).Returns("Fixed Provider");
            provider.Setup(x => x.GetDateTime(It.IsAny<ISequenceEntity>())).Returns(dateTime);
            return provider;
        }
    }
}
