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
using NINA.Core.Locale;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;

namespace NINA.Test.Sequencer.Logic {
    [TestFixture]
    public class UserSymbolTest {
        private Mock<ISymbolBroker> _symbolBroker;

        [SetUp]
        public void SetUp() {
            _symbolBroker = new Mock<ISymbolBroker>();
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
        }

        [TearDown]
        public void TearDown() {
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
        }

        private Variable CreateVariable(string identifier, string definition = "0", ISequenceContainer parent = null) {
            var variable = new Variable {
                SymbolBroker = _symbolBroker.Object
            };
            variable.Expr = new Expression(definition, variable) { 
                SymbolBroker = _symbolBroker.Object 
            };
            variable.OriginalExpr = new Expression(definition, variable) {
                SymbolBroker = _symbolBroker.Object
            };
            if (parent != null) {
                parent.Add(variable);
            }
            variable.Identifier = identifier;
            return variable;
        }

        private Constant CreateConstant(string identifier, string definition = "0", ISequenceContainer parent = null) {
            var constant = new Constant {
                SymbolBroker = _symbolBroker.Object
            };
            constant.Expr = new Expression(definition, constant) { 
                SymbolBroker = _symbolBroker.Object 
            };
            if (parent != null) {
                parent.Add(constant);
            }
            constant.Identifier = identifier;
            return constant;
        }

        private GlobalVariable CreateGlobalVariable(string identifier, string definition = "0") {
            var globalVar = new GlobalVariable {
                SymbolBroker = _symbolBroker.Object
            };
            globalVar.Expr = new Expression(definition, globalVar) { 
                SymbolBroker = _symbolBroker.Object 
            };
            globalVar.OriginalExpr = new Expression(definition, globalVar) {
                SymbolBroker = _symbolBroker.Object
            };
            globalVar.Identifier = identifier;
            return globalVar;
        }

        private GlobalConstant CreateGlobalConstant(string identifier, string definition = "0") {
            var globalConst = new GlobalConstant {
                SymbolBroker = _symbolBroker.Object
            };
            globalConst.Expr = new Expression(definition, globalConst) { 
                SymbolBroker = _symbolBroker.Object 
            };
            globalConst.Identifier = identifier;
            return globalConst;
        }

        private SequentialContainer CreateContainer(string name = "TestContainer") {
            var container = new SequentialContainer {
                Name = name,
                SymbolBroker = _symbolBroker.Object
            };
            return container;
        }

        [Test]
        public void UserSymbol_SetIdentifier_WithNoParent_ShouldSetValue() {
            // arrange
            var variable = CreateVariable("testVar");

            // act & assert
            variable.Identifier.Should().Be("testVar");
            variable.IsDuplicate.Should().BeFalse();
        }

        [Test]
        public void UserSymbol_SetIdentifier_WithParent_ShouldAddToCache() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("testVar", "0", container);

            // assert
            variable.Identifier.Should().Be("testVar");
            UserSymbol.SymbolCache.Should().ContainKey(container);
            UserSymbol.SymbolCache[container].Should().ContainKey("testVar");
            UserSymbol.SymbolCache[container]["testVar"].Should().BeSameAs(variable);
        }

        [Test]
        public void UserSymbol_SetIdentifier_SameValueTwice_ShouldNotDuplicate() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("testVar", "0", container);

            var initialCacheCount = UserSymbol.SymbolCache[container].Count;

            // act - set same identifier again
            variable.Identifier = "testVar";

            // assert
            UserSymbol.SymbolCache[container].Count.Should().Be(initialCacheCount);
            variable.Identifier.Should().Be("testVar");
        }

        [Test]
        public void UserSymbol_SetIdentifier_ChangingValue_ShouldRemoveOldAndAddNew() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("oldVar", "0", container);

            UserSymbol.SymbolCache[container].Should().ContainKey("oldVar");

            // act
            variable.Identifier = "newVar";

            // assert
            UserSymbol.SymbolCache[container].Should().NotContainKey("oldVar");
            UserSymbol.SymbolCache[container].Should().ContainKey("newVar");
            UserSymbol.SymbolCache[container]["newVar"].Should().BeSameAs(variable);
        }

        [Test]
        public void UserSymbol_SetIdentifier_DuplicateAtSameLevel_ShouldClearIdentifier() {
            // arrange
            var container = CreateContainer();
            var variable1 = CreateVariable("duplicate", "0", container);
            var variable2 = CreateVariable("other", "0", container);

            // act - try to set duplicate identifier
            variable2.Identifier = "duplicate";

            // assert
            variable1.Identifier.Should().Be("duplicate");
            variable2.Identifier.Should().Be(""); // GenId returns empty string for duplicates
        }

        [Test]
        public void UserSymbol_SetIdentifier_EmptyString_ShouldNotAddToCache() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("", "0", container);

            // assert
            variable.Identifier.Should().Be("");
            if (UserSymbol.SymbolCache.ContainsKey(container)) {
                UserSymbol.SymbolCache[container].Should().NotContainKey("");
            }
        }

        [Test]
        public void UserSymbol_FindSymbol_InSameContainer_ShouldReturnSymbol() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("testVar", "0", container);

            // act
            var found = UserSymbol.FindSymbol("testVar", container);

            // assert
            found.Should().BeSameAs(variable);
        }

        [Test]
        public void UserSymbol_FindSymbol_InParentContainer_ShouldReturnSymbol() {
            // arrange
            var parentContainer = CreateContainer("Parent");
            var childContainer = CreateContainer("Child");
            parentContainer.Add(childContainer);
            childContainer.AfterParentChanged();

            var variable = CreateVariable("testVar", "0", parentContainer);

            // act
            var found = UserSymbol.FindSymbol("testVar", childContainer);

            // assert
            found.Should().BeSameAs(variable);
        }

        [Test]
        public void UserSymbol_FindSymbol_NotFound_ShouldReturnNull() {
            // arrange
            var container = CreateContainer();

            // act
            var found = UserSymbol.FindSymbol("nonExistent", container);

            // assert
            found.Should().BeNull();
        }

        [Test]
        public void UserSymbol_FindGlobalSymbol_ShouldReturnGlobalVariable() {
            // arrange
            var globalVar = CreateGlobalVariable("globalVar", "42");

            UserSymbol.GlobalSymbols.Add(globalVar);
            globalVar.AfterParentChanged();

            // act
            var found = UserSymbol.FindGlobalSymbol("globalVar");

            // assert
            found.Should().BeSameAs(globalVar);
        }

        [Test]
        public void UserSymbol_FindGlobalSymbol_ShouldReturnGlobalConstant() {
            // arrange
            var globalConst = CreateGlobalConstant("globalConst", "3.14");

            UserSymbol.GlobalSymbols.Add(globalConst);
            globalConst.AfterParentChanged();

            // act
            var found = UserSymbol.FindGlobalSymbol("globalConst");

            // assert
            found.Should().BeSameAs(globalConst);
        }

        [Test]
        public void UserSymbol_FindGlobalSymbol_LocalVariable_ShouldReturnNull() {
            // arrange
            var container = CreateContainer();
            var localVar = CreateVariable("localVar", "0", container);

            // act
            var found = UserSymbol.FindGlobalSymbol("localVar");

            // assert
            found.Should().BeNull();
        }

        [Test]
        public void UserSymbol_AddConsumer_ShouldTrackExpression() {
            // arrange
            var variable = CreateVariable("testVar");
            var expression = new Expression("testVar + 1", variable);

            // act
            variable.AddConsumer(expression);

            // assert
            variable.Consumers.Should().ContainKey(expression);
        }

        [Test]
        public void UserSymbol_AddConsumer_SameTwice_ShouldOnlyAddOnce() {
            // arrange
            var variable = CreateVariable("testVar");
            var expression = new Expression("testVar + 1", variable);

            // act
            variable.AddConsumer(expression);
            variable.AddConsumer(expression);

            // assert
            variable.Consumers.Keys.Should().HaveCount(1);
            variable.Consumers.Should().ContainKey(expression);
        }

        [Test]
        public void UserSymbol_RemoveConsumer_ShouldRemoveExpression() {
            // arrange
            var variable = CreateVariable("testVar");
            var expression = new Expression("testVar + 1", variable);
            variable.AddConsumer(expression);

            // act
            variable.RemoveConsumer(expression);

            // assert
            variable.Consumers.Should().NotContainKey(expression);
        }

        [Test]
        public void UserSymbol_SymbolDirty_ShouldPropagateToConsumers() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("testVar", "10", container);

            var consumerExpr = new Expression("testVar * 2", variable) {
                SymbolBroker = _symbolBroker.Object
            };
            variable.AddConsumer(consumerExpr);

            // act
            UserSymbol.SymbolDirty(variable);

            // assert
            consumerExpr.Dirty.Should().BeTrue();
        }

        [Test]
        public void UserSymbol_SymbolDirty_WithMultipleConsumers_ShouldMarkAllDirty() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("testVar", "10", container);

            var expr1 = new Expression("testVar * 2", variable) { SymbolBroker = _symbolBroker.Object };
            var expr2 = new Expression("testVar + 5", variable) { SymbolBroker = _symbolBroker.Object };
            var expr3 = new Expression("testVar / 3", variable) { SymbolBroker = _symbolBroker.Object };

            variable.AddConsumer(expr1);
            variable.AddConsumer(expr2);
            variable.AddConsumer(expr3);

            // act
            UserSymbol.SymbolDirty(variable);

            // assert
            expr1.Dirty.Should().BeTrue();
            expr2.Dirty.Should().BeTrue();
            expr3.Dirty.Should().BeTrue();
        }

        [Test]
        public void UserSymbol_SymbolDirty_ChainedDependencies_ShouldPropagate() {
            // arrange
            var container = CreateContainer();

            var var1 = CreateVariable("var1", "10", container);
            var var2 = CreateVariable("var2", "var1 * 2", container);

            var expr1 = new Expression("var1", var2) { SymbolBroker = _symbolBroker.Object, Symbol = var1 };
            var2.Expr.Symbol = var2;

            var1.AddConsumer(var2.Expr);
            var2.AddConsumer(expr1);

            // act
            UserSymbol.SymbolDirty(var1);

            // assert
            var2.Expr.Dirty.Should().BeTrue();
            expr1.Dirty.Should().BeTrue();
        }

        [Test]
        public void UserSymbol_SParent_LocalVariable_ShouldReturnParent() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("localVar");
            container.Add(variable);

            // act
            var sParent = variable.SParent();

            // assert
            sParent.Should().BeSameAs(container);
        }

        [Test]
        public void UserSymbol_SParent_GlobalVariable_ShouldReturnGlobalSymbols() {
            // arrange
            var globalVar = CreateGlobalVariable("globalVar", "42");
            var container = CreateContainer();
            container.Add(globalVar);

            // act
            var sParent = globalVar.SParent();

            // assert
            sParent.Should().BeSameAs(UserSymbol.GlobalSymbols);
        }

        [Test]
        public void UserSymbol_SParent_GlobalConstant_ShouldReturnGlobalSymbols() {
            // arrange
            var globalConst = CreateGlobalConstant("globalConst", "3.14");
            var container = CreateContainer();
            container.Add(globalConst);

            // act
            var sParent = globalConst.SParent();

            // assert
            sParent.Should().BeSameAs(UserSymbol.GlobalSymbols);
        }

        [Test]
        public void UserSymbol_SParent_NoParent_ShouldReturnNull() {
            // arrange
            var variable = CreateVariable("testVar");

            // act
            var sParent = variable.SParent();

            // assert
            sParent.Should().BeNull();
        }

        [Test]
        public void UserSymbol_AfterParentChanged_FirstTime_ShouldAddToCache() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("testVar", "0", container);

            // assert
            UserSymbol.SymbolCache.Should().ContainKey(container);
            UserSymbol.SymbolCache[container].Should().ContainKey("testVar");
        }

        [Test]
        public void UserSymbol_AfterParentChanged_SameParent_ShouldNotReprocess() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("testVar", "0", container);

            var initialCacheCount = UserSymbol.SymbolCache[container].Count;

            // act - call again with same parent
            variable.AfterParentChanged();

            // assert
            UserSymbol.SymbolCache[container].Count.Should().Be(initialCacheCount);
        }

        [Test]
        public void UserSymbol_AfterParentChanged_MovingBetweenUnrootedContainers_ShouldRemoveFromCache() {
            // arrange
            // Note: Containers not attached to SequenceRootContainer are treated as "deleted"
            var container1 = CreateContainer("Container1");
            var container2 = CreateContainer("Container2");
            var variable = CreateVariable("testVar", "0", container1);

            UserSymbol.SymbolCache[container1].Should().ContainKey("testVar");

            // act - move to new unrooted parent (symbol gets removed as it's not attached to root)
            container2.Add(variable);

            // assert
            // When moving between unrooted containers, the symbol is removed from cache entirely
            // because the Parent is not attached to SequenceRootContainer (line 351 of UserSymbol.cs)
            UserSymbol.SymbolCache.TryGetValue(container2, out var cache2).Should().BeFalse();
        }

        [Test]
        public void UserSymbol_MultipleSymbols_InSameContainer_ShouldAllBeInCache() {
            // arrange
            var container = CreateContainer();
            var var1 = CreateVariable("var1", "0", container);
            var var2 = CreateVariable("var2", "0", container);
            var var3 = CreateVariable("var3", "0", container);

            // assert
            UserSymbol.SymbolCache[container].Should().HaveCount(3);
            UserSymbol.SymbolCache[container].Should().ContainKeys("var1", "var2", "var3");
        }

        [Test]
        public void UserSymbol_NestedContainers_FindSymbol_ShouldSearchUpHierarchy() {
            // arrange
            var root = CreateContainer("Root");
            var child = CreateContainer("Child");
            var grandchild = CreateContainer("Grandchild");

            root.Add(child);
            child.AfterParentChanged();
            child.Add(grandchild);
            grandchild.AfterParentChanged();

            var variable = CreateVariable("topVar", "0", root);

            // act
            var found = UserSymbol.FindSymbol("topVar", grandchild);

            // assert
            found.Should().BeSameAs(variable);
        }

        [Test]
        public void UserSymbol_NestedContainers_ShadowedSymbol_ShouldReturnClosest() {
            // arrange
            var parent = CreateContainer("Parent");
            var child = CreateContainer("Child");

            parent.Add(child);
            child.AfterParentChanged();

            var parentVar = CreateVariable("shadowedVar", "10", parent);
            var childVar = CreateVariable("shadowedVar", "20", child);

            // act
            var foundInChild = UserSymbol.FindSymbol("shadowedVar", child);
            var foundInParent = UserSymbol.FindSymbol("shadowedVar", parent);

            // assert
            foundInChild.Should().BeSameAs(childVar);
            foundInParent.Should().BeSameAs(parentVar);
        }

        [Test]
        public void UserSymbol_ClearUserSymbols_ShouldClearGlobalSymbolCache() {
            // arrange
            var globalVar = CreateGlobalVariable("globalVar", "42");

            UserSymbol.GlobalSymbols.Add(globalVar);
            globalVar.AfterParentChanged();

            UserSymbol.SymbolCache[UserSymbol.GlobalSymbols].Should().ContainKey("globalVar");

            // act
            UserSymbol.ClearUserSymbols();

            // assert
            if (UserSymbol.SymbolCache.ContainsKey(UserSymbol.GlobalSymbols)) {
                UserSymbol.SymbolCache[UserSymbol.GlobalSymbols].Should().BeEmpty();
            }
        }

        [Test]
        public void UserSymbol_ToString_WithValidSymbol_ShouldReturnFormattedString() {
            // arrange
            var container = CreateContainer();
            var variable = CreateVariable("testVar", "42", container);
            variable.Expr.Evaluate();

            // act
            var result = variable.ToString();

            // assert
            result.Should().Contain("testVar");
            result.Should().Contain(container.Name);
        }

        [Test]
        public void UserSymbol_CopyConstructor_ShouldCopyIdentifierAndExpression() {
            // arrange
            var original = CreateVariable("originalVar", "123");

            // act
            var copy = new Variable(original);

            // assert
            copy.Identifier.Should().Be("originalVar");
            copy.Expr.Definition.Should().Be("123");
            copy.Should().NotBeSameAs(original);
            copy.Expr.Should().NotBeSameAs(original.Expr);
        }

        [Test]
        public void UserSymbol_MultipleContainers_IndependentCaches_ShouldIsolateSymbols() {
            // arrange
            var container1 = CreateContainer("Container1");
            var container2 = CreateContainer("Container2");

            var var1 = CreateVariable("testVar", "10", container1);
            var var2 = CreateVariable("testVar", "20", container2);

            // assert
            UserSymbol.SymbolCache[container1]["testVar"].Should().BeSameAs(var1);
            UserSymbol.SymbolCache[container2]["testVar"].Should().BeSameAs(var2);
            UserSymbol.FindSymbol("testVar", container1).Should().BeSameAs(var1);
            UserSymbol.FindSymbol("testVar", container2).Should().BeSameAs(var2);
        }

        [Test]
        public void UserSymbol_LocalAndGlobal_SameIdentifier_FindSymbolShouldReturnLocal() {
            // arrange
            var container = CreateContainer();

            var globalVar = CreateGlobalVariable("sameName", "42");
            UserSymbol.GlobalSymbols.Add(globalVar);
            globalVar.AfterParentChanged();

            var localVar = CreateVariable("sameName", "100", container);

            // act
            var found = UserSymbol.FindSymbol("sameName", container);

            // assert
            found.Should().BeSameAs(localVar);
        }

        [Test]
        public void UserSymbol_GlobalVariable_AfterParentChanged_ShouldUseGlobalSymbolsContainer() {
            // arrange
            var container = CreateContainer();
            var globalVar = CreateGlobalVariable("globalVar", "42");

            // act
            container.Add(globalVar);
            globalVar.Identifier = "globalVar";
            globalVar.AfterParentChanged();

            // assert
            UserSymbol.SymbolCache.Should().ContainKey(UserSymbol.GlobalSymbols);
            UserSymbol.SymbolCache[UserSymbol.GlobalSymbols].Should().ContainKey("globalVar");
            UserSymbol.SymbolCache[UserSymbol.GlobalSymbols]["globalVar"].Should().BeSameAs(globalVar);
        }

        [Test]
        public void UserSymbol_Identifier_ValidPattern_ShouldMatch() {
            // Valid identifiers according to VALID_SYMBOL pattern: ^[a-zA-Z_][a-zA-Z0-9_]*$
            var validIdentifiers = new[] {
                "a", "Z", "myVar", "MyVar123", "test_var", "_private",
                "A1", "variable_123", "TEST_CONSTANT", "_"
            };

            foreach (var identifier in validIdentifiers) {
                UserSymbol.ValidSymbolRegex.IsMatch(identifier)
                    .Should().BeTrue($"{identifier} should be valid");
            }
        }

        [Test]
        public void UserSymbol_Identifier_InvalidPattern_ShouldNotMatch() {
            // Invalid identifiers
            var invalidIdentifiers = new[] {
                "1var", "123", "-var", "+var", "my var", "my@var", "my.var",
                "my$var", "", "my#var", "test-var", "test+var"
            };

            foreach (var identifier in invalidIdentifiers) {
                UserSymbol.ValidSymbolRegex.IsMatch(identifier)
                    .Should().BeFalse($"{identifier} should be invalid");
            }
        }

        [Test]
        public void UserSymbol_AfterParentChanged_WithEmptyIdentifier_ShouldNotAddToCache() {
            // arrange
            var container = CreateContainer();
            var variable = new Variable {
                SymbolBroker = _symbolBroker.Object
            };
            variable.Expr = new Expression("0", variable) { 
                SymbolBroker = _symbolBroker.Object 
            };
            variable.OriginalExpr = new Expression("0", variable) {
                SymbolBroker = _symbolBroker.Object
            };

            // act
            container.Add(variable);
            variable.Identifier = ""; // explicitly set to empty

            // assert
            if (UserSymbol.SymbolCache.ContainsKey(container)) {
                UserSymbol.SymbolCache[container].Should().NotContainKey("");
            }
        }

        [Test]
        public void UserSymbol_FindSymbol_WithNullContext_ShouldSearchGlobalOnly() {
            // arrange
            var globalVar = CreateGlobalVariable("globalOnly", "99");

            UserSymbol.GlobalSymbols.Add(globalVar);
            globalVar.AfterParentChanged();

            // act
            var found = UserSymbol.FindSymbol("globalOnly", null);

            // assert
            found.Should().BeSameAs(globalVar);
        }

        [Test]
        public void UserSymbol_SymbolCache_MultipleSymbolsInContainer_ShouldMaintainAll() {
            // arrange
            var container = CreateContainer();
            var variables = new List<Variable>();

            for (int i = 1; i <= 10; i++) {
                var v = CreateVariable($"var{i}", $"{i * 10}", container);
                variables.Add(v);
            }

            // assert
            UserSymbol.SymbolCache[container].Should().HaveCount(10);

            for (int i = 1; i <= 10; i++) {
                UserSymbol.SymbolCache[container].Should().ContainKey($"var{i}");
                var found = UserSymbol.FindSymbol($"var{i}", container);
                found.Should().BeSameAs(variables[i - 1]);
            }
        }

        [Test]
        public void UserSymbol_Consumers_AddAndRemoveMultiple_ShouldTrackCorrectly() {
            // arrange
            var variable = CreateVariable("testVar");
            var expressions = new List<Expression> {
                new Expression("testVar + 1", variable),
                new Expression("testVar * 2", variable),
                new Expression("testVar - 3", variable)
            };

            // act - add all
            foreach (var expr in expressions) {
                variable.AddConsumer(expr);
            }

            // assert - all added
            variable.Consumers.Should().HaveCount(3);

            // act - remove one
            variable.RemoveConsumer(expressions[1]);

            // assert - one removed
            variable.Consumers.Should().HaveCount(2);
            variable.Consumers.Should().ContainKey(expressions[0]);
            variable.Consumers.Should().NotContainKey(expressions[1]);
            variable.Consumers.Should().ContainKey(expressions[2]);
        }

        [Test]
        public void UserSymbol_GlobalSymbols_Property_ShouldBeSharedInstance() {
            // act
            var gs1 = UserSymbol.GlobalSymbols;
            var gs2 = UserSymbol.GlobalSymbols;

            // assert
            gs1.Should().BeSameAs(gs2);
            gs1.Name.Should().Be("Global Symbols");
        }

        [Test]
        public void UserSymbol_SymbolCache_Property_ShouldBeSharedInstance() {
            // act
            var cache1 = UserSymbol.SymbolCache;
            var cache2 = UserSymbol.SymbolCache;

            // assert
            cache1.Should().BeSameAs(cache2);
        }

        /// <summary>
        /// Verifies global symbol lookup ignores a cached global definition that is no longer attached to the active sequence tree.
        /// </summary>
        [Test]
        public void UserSymbol_FindGlobalSymbol_IgnoresOrphanedGlobalDefinitions() {
            var orphanParent = CreateContainer("Orphan");
            var orphan = CreateGlobalVariable("orphanGlobal", "42");
            orphanParent.Add(orphan);
            orphan.AfterParentChanged();

            UserSymbol.FindGlobalSymbol("orphanGlobal").Should().BeNull();
        }

        /// <summary>
        /// Verifies removing a scoped symbol from a rooted sequence clears the cache entry and marks dependent expressions dirty.
        /// </summary>
        [Test]
        public void UserSymbol_AfterParentChanged_RemovesDeletedScopedSymbolAndDirtiesConsumers() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer container = CreateContainer("Child");
            root.Add(container);
            Variable variable = CreateVariable("local", "10", container);
            Expression consumer = new Expression("local + 1", variable) { SymbolBroker = _symbolBroker.Object };
            variable.AddConsumer(consumer);

            variable.AttachNewParent(null);

            UserSymbol.SymbolCache[container].Should().NotContainKey("local");
            consumer.Dirty.Should().BeTrue();
        }

        /// <summary>
        /// Verifies the symbol tooltip helper reports numeric range guidance when an empty expression has range metadata.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void UserSymbol_ShowSymbols_UsesRangeTooltipForEmptyRangedExpression() {
            TextBox textBox = new TextBox();
            Expression expression = new Expression("", Mock.Of<ISequenceEntity>()) {
                Range = new[] { 1.0, 3.0, 0.0 }
            };
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(Expression.Definition)) { Source = expression });

            UserSymbol.ShowSymbols(textBox);

            textBox.ToolTip.Should().Be("Value must be between 1 and 3.");
        }

        /// <summary>
        /// Verifies the symbol tooltip helper explains when an expression has unresolved references and no resolved symbols.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void UserSymbol_ShowSymbols_ReportsNotYetDefinedForSingleUnresolvedReference() {
            TextBox textBox = new TextBox();
            Expression expression = new Expression("missing + 1", Mock.Of<ISequenceEntity>()) {
                SymbolBroker = _symbolBroker.Object
            };
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(Expression.Definition)) { Source = expression });

            UserSymbol.ShowSymbols(textBox);

            textBox.ToolTip.Should().BeOfType<string>().Which.Should().Contain("not yet defined");
        }

        /// <summary>
        /// Verifies the symbol tooltip helper lists resolved user symbols and broker-provided data symbols with their current values.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void UserSymbol_ShowSymbols_ListsResolvedUserAndBrokerSymbols() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer container = CreateContainer("Scope");
            root.Add(container);
            Variable variable = CreateVariable("local", "42", container);
            variable.Expr.Evaluate(ignoreRoot: true);
            object brokerValue = 12.3456;
            Symbol brokerSymbol = new Symbol("brokerValue", brokerValue, "Broker", null, Symbol.SymbolType.SYMBOL_NORMAL);
            _symbolBroker.Setup(b => b.TryGetValue("brokerValue", out brokerValue)).Returns(true);
            _symbolBroker.Setup(b => b.TryGetSymbol("brokerValue", out brokerSymbol)).Returns(true);
            Mock<ISequenceEntity> context = new Mock<ISequenceEntity>();
            context.SetupGet(c => c.Parent).Returns(container);
            Expression expression = new Expression("local + brokerValue", context.Object) {
                SymbolBroker = _symbolBroker.Object
            };
            expression.Evaluate(ignoreRoot: true);
            TextBox textBox = new TextBox();
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(Expression.Definition)) { Source = expression });

            UserSymbol.ShowSymbols(textBox);

            textBox.ToolTip.Should().BeOfType<string>().Which
                .Should().Contain("local").And.Contain("Scope").And.Contain("brokerValue").And.Contain("Broker");
        }

        /// <summary>
        /// Verifies the symbol tooltip helper falls back to a symbol's expression when the binding source is a UserSymbol rather than an Expression.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void UserSymbol_ShowSymbols_UsesUserSymbolExpressionWhenBindingSourceIsSymbol() {
            Variable variable = CreateVariable("local", "42");
            TextBox textBox = new TextBox();
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(UserSymbol.Identifier)) { Source = variable });

            UserSymbol.ShowSymbols(textBox);

            textBox.ToolTip.Should().Be(Loc.Instance["LblNoSymbols"]);
        }

        /// <summary>
        /// Verifies the symbol tooltip helper reports an unknown binding source when neither an expression nor user symbol is available.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void UserSymbol_ShowSymbols_ReportsUnknownBindingSource() {
            TextBox textBox = new TextBox();
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(TextBox.Tag)) { Source = textBox });

            UserSymbol.ShowSymbols(textBox);

            textBox.ToolTip.Should().Be("??");
        }

        /// <summary>
        /// Verifies symbol diagnostic helpers and fallback stringification remain safe for detached symbols.
        /// </summary>
        [Test]
        public void UserSymbol_Diagnostics_LogOnceWarnAndStringifyDetachedSymbols() {
            Variable variable = CreateVariable("local", "42");

            UserSymbol.LogOnce("symbol warning");
            UserSymbol.LogOnce("symbol warning");
            UserSymbol.Warn("symbol warning direct");

            variable.SParent().Should().BeNull();
            variable.ToString().Should().Contain("local");
        }
    }
}
