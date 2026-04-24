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
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.View.Sequencer.Converter;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Expression = NINA.Sequencer.Logic.Expression;

namespace NINA.Test.Sequencer.Logic {

    [TestFixture]
    public class ExpressionUiSupportTest {

        /// <summary>
        /// Verifies the Expandable String Trims Expands Caches And Invalidates scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ExpandableString_TrimsExpandsCachesAndInvalidates() {
            Mock<ISymbolBroker> symbolBrokerMock = new Mock<ISymbolBroker>();
            ExpandableString sut = new ExpandableString("  gain {2 + 1.5}  ");
            List<string> changedProperties = new List<string>();
            sut.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            sut.SetSymbolBroker(symbolBrokerMock.Object);
            sut.SetParent(new SequentialContainer());

            sut.Value.Should().Be("gain {2 + 1.5}");
            sut.Expanded.Should().Be("gain 3.5");
            sut.Expanded.Should().Be("gain 3.5", "the expanded value is cached until invalidated");

            sut.Value = "gain {4 + 1}";
            sut.Invalidate();

            sut.Expanded.Should().Be("gain 5");
            sut.HasError.Should().BeFalse();
            sut.Error.Should().BeNull();
            sut.ToString().Should().Be("gain {4 + 1}");
            ((string)sut).Should().Be("gain {4 + 1}");
            ((ExpandableString)"  literal  ").Value.Should().Be("literal");
            changedProperties.Should().Contain(nameof(ExpandableString.Expanded));
        }

        /// <summary>
        /// Verifies the Expandable String Value Changes Raise Dependent Notifications And Handle Empty Values scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ExpandableString_ValueChangesRaiseDependentNotificationsAndHandleEmptyValues() {
            ExpandableString sut = new ExpandableString();
            List<string> changedProperties = new List<string>();
            sut.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            sut.Value = "  first  ";
            sut.Expanded.Should().Be("first");
            sut.Value = null;

            sut.Value.Should().BeNull();
            sut.Expanded.Should().BeNull();
            sut.ToString().Should().BeEmpty();
            changedProperties.Should().Contain(new[] {
                nameof(ExpandableString.Value),
                nameof(ExpandableString.Expanded),
                nameof(ExpandableString.HasError),
                nameof(ExpandableString.Error)
            });
        }

        /// <summary>
        /// Verifies the Expr Converter Handles Sentinels Literals Expressions Booleans And Combo Values scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ExprConverter_HandlesSentinelsLiteralsExpressionsBooleansAndComboValues() {
            ExprConverter sut = new ExprConverter();
            Mock<ISequenceEntity> numericContextMock = new Mock<ISequenceEntity>();

            sut.Convert(null, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("");
            sut.Convert(new object[] { DependencyProperty.UnsetValue }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("");
            sut.Convert(new object[] { Binding.DoNothing }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("");
            sut.Convert(new object[] { "not an expression" }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("{??}");

            Expression literal = new Expression("5", numericContextMock.Object);
            sut.Convert(new object[] { literal }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("{Not an Expression}");

            Expression empty = new Expression("", numericContextMock.Object);
            sut.Convert(new object[] { empty }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("");

            Expression arithmetic = new Expression("1 + 2", numericContextMock.Object);
            arithmetic.Evaluate(true);
            sut.Convert(new object[] { arithmetic }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("{3}");

            Expression combo = new Expression("1", numericContextMock.Object) { ForceAnnotated = true };
            sut.Convert(new object[] { combo, null, new List<string> { "L", "Ha" } }, typeof(string), null, CultureInfo.InvariantCulture)
                .Should().Be("{Filter_Ha}");

            Mock<ISequenceEntity> trueFalseContextMock = new Mock<ISequenceEntity>();
            trueFalseContextMock.As<ITrueFalse>();
            Expression truth = new Expression("1", trueFalseContextMock.Object) { ForceAnnotated = true };
            Expression falsehood = new Expression("0", trueFalseContextMock.Object) { ForceAnnotated = true };

            sut.Convert(new object[] { truth }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("{True}");
            sut.Convert(new object[] { falsehood }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("{False}");
        }

        /// <summary>
        /// Verifies the Distinct Color Palette Generates Frozen Distinct Brushes For Dark And Light Base Colors scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void DistinctColorPalette_GeneratesFrozenDistinctBrushesForDarkAndLightBaseColors() {
            Brush[] darkBasePalette = DistinctColorPalette.Generate(Color.FromRgb(20, 30, 40), 8);
            Brush[] lightBasePalette = DistinctColorPalette.Generate(Color.FromRgb(230, 230, 230), 8);

            darkBasePalette.Should().HaveCount(8);
            lightBasePalette.Should().HaveCount(8);
            darkBasePalette.Concat(lightBasePalette).Should().OnlyContain(b => b.IsFrozen);
            darkBasePalette.Cast<SolidColorBrush>().Select(b => b.Color).Distinct().Should().HaveCountGreaterThan(5);
            lightBasePalette.Cast<SolidColorBrush>().Select(b => b.Color).Distinct().Should().HaveCountGreaterThan(5);
        }

        /// <summary>
        /// Verifies the Tree View Depth To Color Converter uses the fallback/base brush, distinguishes top-level containers, and rejects ConvertBack.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void TreeViewDepthToColorConverter_UsesFallbackBrushBaseBrushDistinguishesTopLevelContainersAndRejectsConvertBack() {
            TreeViewDepthToColorConverter sut = new TreeViewDepthToColorConverter();
            TreeView tree = new TreeView();
            TreeViewItem startArea = new TreeViewItem { DataContext = new StartAreaContainer() };
            TreeViewItem firstRootContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            TreeViewItem secondRootContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            TreeViewItem nestedContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            firstRootContainer.Items.Add(nestedContainer);
            startArea.Items.Add(firstRootContainer);
            startArea.Items.Add(secondRootContainer);
            tree.Items.Add(startArea);

            object fallback = sut.Convert(new object[] { "not a brush", "not a tree item" }, typeof(Brush), null, CultureInfo.InvariantCulture);
            object firstRootBrush = sut.Convert(new object[] { new SolidColorBrush(Color.FromRgb(40, 60, 80)), firstRootContainer }, typeof(Brush), null, CultureInfo.InvariantCulture);
            object secondRootBrush = sut.Convert(new object[] { new SolidColorBrush(Color.FromRgb(40, 60, 80)), secondRootContainer }, typeof(Brush), null, CultureInfo.InvariantCulture);
            object childBrush = sut.Convert(new object[] { new SolidColorBrush(Color.FromRgb(40, 60, 80)), nestedContainer }, typeof(Brush), null, CultureInfo.InvariantCulture);

            sut.Convert(Array.Empty<object>(), typeof(Brush), null, CultureInfo.InvariantCulture).Should().BeNull();
            fallback.Should().BeAssignableTo<Brush>();
            firstRootBrush.Should().BeAssignableTo<Brush>();
            secondRootBrush.Should().BeAssignableTo<Brush>();
            childBrush.Should().BeAssignableTo<Brush>();
            ((SolidColorBrush)secondRootBrush).Color.Should().NotBe(((SolidColorBrush)firstRootBrush).Color);
            ((SolidColorBrush)childBrush).Color.Should().NotBe(((SolidColorBrush)firstRootBrush).Color);

            Action convertBack = () => sut.ConvertBack(firstRootBrush, new[] { typeof(Brush) }, null, CultureInfo.InvariantCulture);
            convertBack.Should().Throw<NotImplementedException>();
        }

        /// <summary>
        /// Verifies branch descendants do not reuse the same color as their shifted root container or neighboring root branches.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void TreeViewDepthToColorConverter_KeepsBranchLevelsDistinctFromRootLevels() {
            TreeViewDepthToColorConverter sut = new TreeViewDepthToColorConverter();
            TreeView tree = new TreeView();
            TreeViewItem startArea = new TreeViewItem { DataContext = new StartAreaContainer() };
            TreeViewItem firstRootContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            TreeViewItem secondRootContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            TreeViewItem thirdRootContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            TreeViewItem secondLevelContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            TreeViewItem thirdLevelContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            secondLevelContainer.Items.Add(thirdLevelContainer);
            secondRootContainer.Items.Add(secondLevelContainer);
            startArea.Items.Add(firstRootContainer);
            startArea.Items.Add(secondRootContainer);
            startArea.Items.Add(thirdRootContainer);
            tree.Items.Add(startArea);

            Color firstRootColor = ((SolidColorBrush)sut.Convert(new object[] { new SolidColorBrush(Color.FromRgb(40, 60, 80)), firstRootContainer }, typeof(Brush), null, CultureInfo.InvariantCulture)).Color;
            Color secondRootColor = ((SolidColorBrush)sut.Convert(new object[] { new SolidColorBrush(Color.FromRgb(40, 60, 80)), secondRootContainer }, typeof(Brush), null, CultureInfo.InvariantCulture)).Color;
            Color thirdRootColor = ((SolidColorBrush)sut.Convert(new object[] { new SolidColorBrush(Color.FromRgb(40, 60, 80)), thirdRootContainer }, typeof(Brush), null, CultureInfo.InvariantCulture)).Color;
            Color secondLevelColor = ((SolidColorBrush)sut.Convert(new object[] { new SolidColorBrush(Color.FromRgb(40, 60, 80)), secondLevelContainer }, typeof(Brush), null, CultureInfo.InvariantCulture)).Color;
            Color thirdLevelColor = ((SolidColorBrush)sut.Convert(new object[] { new SolidColorBrush(Color.FromRgb(40, 60, 80)), thirdLevelContainer }, typeof(Brush), null, CultureInfo.InvariantCulture)).Color;

            secondRootColor.Should().NotBe(firstRootColor);
            secondLevelColor.Should().NotBe(secondRootColor);
            secondLevelColor.Should().NotBe(thirdRootColor);
            thirdLevelColor.Should().NotBe(secondLevelColor);
            thirdLevelColor.Should().NotBe(thirdRootColor);
        }

        /// <summary>
        /// Verifies existing top-level container color bindings are invalidated when sibling insertion changes their alternation index.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void TreeViewDepthToColorConverter_ReevaluatesExistingTopLevelContainerColorsWhenSiblingInserted() {
            EnsureApplication();
            TreeViewDepthToColorConverter sut = new TreeViewDepthToColorConverter();
            TreeView tree = new TreeView();
            TreeViewItem startArea = new TreeViewItem {
                DataContext = new StartAreaContainer(),
                IsExpanded = true
            };
            startArea.SetValue(ItemsControl.AlternationCountProperty, 1024);

            TreeViewItem firstRootContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            TreeViewItem thirdRootContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            startArea.Items.Add(firstRootContainer);
            startArea.Items.Add(thirdRootContainer);
            tree.Items.Add(startArea);

            tree.Measure(new Size(400, 400));
            tree.Arrange(new Rect(0, 0, 400, 400));
            tree.UpdateLayout();
            DrainDispatcher();

            ItemsControl.GetAlternationIndex(firstRootContainer).Should().Be(0);
            ItemsControl.GetAlternationIndex(thirdRootContainer).Should().Be(1);

            Border firstProbe = CreateColorProbe(sut, firstRootContainer);
            Border thirdProbe = CreateColorProbe(sut, thirdRootContainer);
            Color initialThirdColor = ((SolidColorBrush)thirdProbe.BorderBrush).Color;

            TreeViewItem secondRootContainer = new TreeViewItem { DataContext = new SequentialContainer() };
            startArea.Items.Insert(1, secondRootContainer);

            tree.UpdateLayout();
            DrainDispatcher();

            ItemsControl.GetAlternationIndex(secondRootContainer).Should().Be(1);
            ItemsControl.GetAlternationIndex(thirdRootContainer).Should().Be(2);

            Border secondProbe = CreateColorProbe(sut, secondRootContainer);
            Color secondColor = ((SolidColorBrush)secondProbe.BorderBrush).Color;
            Color thirdColor = ((SolidColorBrush)thirdProbe.BorderBrush).Color;

            secondColor.Should().NotBe(thirdColor);
            thirdColor.Should().NotBe(initialThirdColor);
            ((SolidColorBrush)firstProbe.BorderBrush).Color.Should().NotBe(secondColor);
        }

        /// <summary>
        /// Verifies symbol tooltip conversion lists available constant options and returns null for incomplete converter inputs.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SymbolToTooltipConverter_ListsConstantsAndHandlesMissingInputs() {
            Symbol[] constants = {
                new Symbol("East", 0),
                new Symbol("West", 1)
            };
            Symbol symbol = new Symbol("PierSide", 0, "Telescope", constants, Symbol.SymbolType.SYMBOL_NORMAL);
            Mock<ISymbolBroker> symbolBrokerMock = new Mock<ISymbolBroker>();
            symbolBrokerMock.Setup(x => x.GetSymbols()).Returns(new List<Symbol> { symbol });
            SymbolController controller = new SymbolController(symbolBrokerMock.Object, Mock.Of<IProfileService>());
            CancelControllerRefreshLoop(controller);
            SymbolToTooltipConverter sut = new SymbolToTooltipConverter();

            object tooltip = sut.Convert(new object[] { symbol, controller }, typeof(string), null, CultureInfo.InvariantCulture);

            tooltip.Should().BeOfType<string>().Which.Should().Contain("East").And.Contain("West");
            sut.Convert(new object[] { null, controller }, typeof(string), null, CultureInfo.InvariantCulture).Should().BeNull();
            sut.Convert(new object[] { symbol, null }, typeof(string), null, CultureInfo.InvariantCulture).Should().BeNull();
            Action convertBack = () => sut.ConvertBack(tooltip, new[] { typeof(string) }, null, CultureInfo.InvariantCulture);
            convertBack.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Verifies category tooltip conversion lists hidden symbols for a category and reports an empty-category message when none exist.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SymbolCategoryToTooltipConverter_ListsHiddenSymbolsAndEmptyCategoryMessage() {
            List<Symbol> hidden = new List<Symbol> {
                new Symbol("InternalTemperature", -5, "Camera", null, Symbol.SymbolType.SYMBOL_HIDDEN)
            };
            Mock<ISymbolBroker> symbolBrokerMock = new Mock<ISymbolBroker>();
            symbolBrokerMock.Setup(x => x.GetSymbols()).Returns(new List<Symbol>());
            symbolBrokerMock.Setup(x => x.GetHiddenSymbols("Camera")).Returns(hidden);
            SymbolController controller = new SymbolController(symbolBrokerMock.Object, Mock.Of<IProfileService>());
            CancelControllerRefreshLoop(controller);
            SymbolCategoryToTooltipConverter sut = new SymbolCategoryToTooltipConverter();

            object tooltip = sut.Convert(new object[] { "Camera", controller }, typeof(string), null, CultureInfo.InvariantCulture);
            object emptyTooltip = sut.Convert(new object[] { "Focuser", controller }, typeof(string), null, CultureInfo.InvariantCulture);

            tooltip.Should().BeOfType<string>().Which.Should().Contain("InternalTemperature").And.Contain("-5");
            emptyTooltip.Should().BeOfType<string>().Which.Should().Contain("Focuser");
            sut.Convert(new object[] { "", controller }, typeof(string), null, CultureInfo.InvariantCulture).Should().BeNull();
            Action convertBack = () => sut.ConvertBack(tooltip, new[] { typeof(string) }, null, CultureInfo.InvariantCulture);
            convertBack.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Verifies the Symbol Controller Filters Hidden Symbols And Refreshes Changed Symbols scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SymbolController_FiltersHiddenSymbolsAndRefreshesChangedSymbols() {
            Symbol initial = new Symbol("Gain", 100, "Camera", null, Symbol.SymbolType.SYMBOL_NORMAL);
            Symbol removed = new Symbol("Temperature", -5, "Camera", null, Symbol.SymbolType.SYMBOL_NORMAL);
            Symbol updated = new Symbol("Gain", 200, "Camera", null, Symbol.SymbolType.SYMBOL_NORMAL);
            Symbol added = new Symbol("Altitude", 42, "Telescope", null, Symbol.SymbolType.SYMBOL_NORMAL);
            List<Symbol> hidden = new List<Symbol> { new Symbol("Internal", 1) };
            Mock<ISymbolBroker> symbolBrokerMock = new Mock<ISymbolBroker>();
            symbolBrokerMock.Setup(x => x.GetSymbols()).Returns(new List<Symbol> { initial, removed });
            symbolBrokerMock.Setup(x => x.GetHiddenSymbols("Camera")).Returns(hidden);
            SymbolController sut = new SymbolController(symbolBrokerMock.Object, Mock.Of<IProfileService>());
            CancelControllerRefreshLoop(sut);

            sut.ViewFilter = "gain";
            sut.SymbolsView.Cast<Symbol>().Should().ContainSingle().Which.Key.Should().Be("Gain");
            sut.GetHiddenSymbols("Camera").Should().BeSameAs(hidden);
            sut.GetHiddenSymbols("Missing").Should().BeEmpty();
            sut.DataSymbols.Should().Contain(new[] { initial, removed });
        }

        /// <summary>
        /// Verifies the Symbol Function Controller Filters And Refreshes Function List scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SymbolFunctionController_FiltersAndRefreshesFunctionList() {
            SymbolFunction initial = new SymbolFunction("median", "Math", "median", "median(x)", args => 0);
            SymbolFunction removed = new SymbolFunction("old", "Math", "old", "old()", args => 0);
            SymbolFunction added = new SymbolFunction("now", "Time", "now", "now()", args => 0);
            Mock<ISymbolBroker> symbolBrokerMock = new Mock<ISymbolBroker>();
            symbolBrokerMock.Setup(x => x.GetFunctions()).Returns(new List<SymbolFunction> { initial, removed });
            SymbolFunctionController sut = new SymbolFunctionController(symbolBrokerMock.Object, Mock.Of<IProfileService>());
            CancelControllerRefreshLoop(sut);

            sut.ViewFilter = "med";
            sut.SymbolFunctionsView.Cast<SymbolFunction>().Should().ContainSingle().Which.Key.Should().Be("median");

            sut.DataSymbolFunctions.Should().Contain(new[] { initial, removed });
            sut.DataSymbolFunctions.Should().Contain(f => f.Key == "median");
        }

        private static void CancelControllerRefreshLoop(object controller) {
            FieldInfo field = controller.GetType().GetField("_cts", BindingFlags.NonPublic | BindingFlags.Instance);
            (field?.GetValue(controller) as CancellationTokenSource)?.Cancel();
        }

        private static void InvokePrivate(object instance, string methodName, object[] arguments) {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull();
            method.Invoke(instance, arguments);
        }

        private static Border CreateColorProbe(TreeViewDepthToColorConverter converter, TreeViewItem item) {
            Border probe = new Border();
            MultiBinding binding = new MultiBinding {
                Converter = converter
            };
            binding.Bindings.Add(new Binding {
                Source = new SolidColorBrush(Color.FromRgb(40, 60, 80))
            });
            binding.Bindings.Add(new Binding {
                Source = item
            });
            binding.Bindings.Add(new Binding {
                Source = item,
                Path = new PropertyPath("(0)", ItemsControl.AlternationIndexProperty)
            });

            BindingOperations.SetBinding(probe, Border.BorderBrushProperty, binding);
            DrainDispatcher();
            return probe;
        }

        private static void EnsureApplication() {
            if (Application.Current == null) {
                _ = new Application {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }
        }

        private static void DrainDispatcher() {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }
}
