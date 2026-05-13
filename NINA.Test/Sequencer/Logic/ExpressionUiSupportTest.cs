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

        [Test]
        public void SymbolValueDisplayConverter_FormatsTemporalValuesForSymbolUiDisplay() {
            SymbolValueDisplayConverter sut = new SymbolValueDisplayConverter();
            DateTime dateTime = new DateTime(2026, 5, 12, 12, 34, 56, DateTimeKind.Utc);

            sut.Convert(dateTime, typeof(string), null, CultureInfo.GetCultureInfo("de-DE")).Should().Be("2026-05-12 12:34:56");
            sut.Convert(new DateTimeOffset(dateTime), typeof(string), null, CultureInfo.GetCultureInfo("de-DE")).Should().Be("2026-05-12 12:34:56");
            sut.Convert(new DateOnly(2026, 5, 12), typeof(string), null, CultureInfo.GetCultureInfo("de-DE")).Should().Be("2026-05-12");
            sut.Convert(new TimeOnly(12, 34, 56), typeof(string), null, CultureInfo.GetCultureInfo("de-DE")).Should().Be("12:34:56");
            sut.Convert(12.5d, typeof(string), null, CultureInfo.InvariantCulture).Should().Be(12.5d);
            sut.Convert("CoverOpen", typeof(string), null, CultureInfo.InvariantCulture).Should().Be("CoverOpen");
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
    }
}
