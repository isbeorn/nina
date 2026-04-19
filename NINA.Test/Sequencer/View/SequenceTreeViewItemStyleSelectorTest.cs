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
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.View.Sequencer;
using NUnit.Framework;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Test.Sequencer.View {

    [TestFixture]
    public class SequenceTreeViewItemStyleSelectorTest {

        /// <summary>
        /// Verifies root and immutable sequencing areas use the default tree style while normal sequence items use the item style.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SelectStyle_UsesDefaultForRootAreasAndItemStyleForItems() {
            Style defaultStyle = new Style();
            Style itemStyle = new Style();
            SequenceTreeViewItemStyleSelector sut = new SequenceTreeViewItemStyleSelector {
                DefaultStyle = defaultStyle,
                ContainerStyle = new Style(),
                ItemStyle = itemStyle
            };

            sut.SelectStyle(new SequenceRootContainer(), new FrameworkElement()).Should().BeSameAs(defaultStyle);
            sut.SelectStyle(new StartAreaContainer(), new FrameworkElement()).Should().BeSameAs(defaultStyle);
            sut.SelectStyle(new TargetAreaContainer(), new FrameworkElement()).Should().BeSameAs(defaultStyle);
            sut.SelectStyle(new EndAreaContainer(), new FrameworkElement()).Should().BeSameAs(defaultStyle);
            sut.SelectStyle(new UnknownSequenceItem("Missing"), new FrameworkElement()).Should().BeSameAs(itemStyle);
        }

        /// <summary>
        /// Verifies mutable containers use the container style only when their data template is hierarchical or absent.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SelectStyle_ChoosesContainerStyleOnlyForHierarchicalOrMissingContainerTemplates() {
            Style containerStyle = new Style();
            Style itemStyle = new Style();
            SequenceTreeViewItemStyleSelector sut = new SequenceTreeViewItemStyleSelector {
                DefaultStyle = new Style(),
                ContainerStyle = containerStyle,
                ItemStyle = itemStyle
            };
            FrameworkElement missingTemplateScope = new FrameworkElement();
            FrameworkElement hierarchicalTemplateScope = new FrameworkElement();
            hierarchicalTemplateScope.Resources.Add(new DataTemplateKey(typeof(SequentialContainer)), new HierarchicalDataTemplate());
            FrameworkElement flatTemplateScope = new FrameworkElement();
            flatTemplateScope.Resources.Add(new DataTemplateKey(typeof(SequentialContainer)), new DataTemplate());
            SequentialContainer container = new SequentialContainer();

            sut.SelectStyle(container, missingTemplateScope).Should().BeSameAs(containerStyle);
            sut.SelectStyle(container, hierarchicalTemplateScope).Should().BeSameAs(containerStyle);
            sut.SelectStyle(container, flatTemplateScope).Should().BeSameAs(itemStyle);
        }
    }
}
