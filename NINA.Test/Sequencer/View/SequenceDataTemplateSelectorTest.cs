#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Trigger;
using NINA.View.Sequencer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Test.Sequencer.View {

    [TestFixture]
    public class SequenceDataTemplateSelectorTest {
        private DataTemplate SequenceContainer = new DataTemplate();
        private DataTemplate DeepSkyObjectContainer = new DataTemplate();
        private DataTemplate SequenceItem = new DataTemplate();
        private DataTemplate SequenceTrigger = new DataTemplate();
        private DataTemplate SequenceCondition = new DataTemplate();

        private SequenceDataTemplateSelector sut;

        [SetUp]
        public void Setup() {
            sut = new SequenceDataTemplateSelector();
            sut.SequenceContainer = SequenceContainer;
            sut.DeepSkyObjectContainer = DeepSkyObjectContainer;
            sut.SequenceItem = SequenceItem;
            sut.SequenceTrigger = SequenceTrigger;
            sut.SequenceCondition = SequenceCondition;
        }

        [Test]
        public void SelectTemplate_IImmutableContainer_CorrectTemplateSelected() {
            var template = sut.SelectTemplate(new Mock<IImmutableContainer>().Object, default);

            template.Should().Be(SequenceItem);
        }

        [Test]
        public void SelectTemplate_IDeepSkyObjectContainer_CorrectTemplateSelected() {
            var template = sut.SelectTemplate(new Mock<IDeepSkyObjectContainer>().Object, default);

            template.Should().Be(DeepSkyObjectContainer);
        }

        [Test]
        public void SelectTemplate_ISequenceCondition_CorrectTemplateSelected() {
            var template = sut.SelectTemplate(new Mock<ISequenceCondition>().Object, default);

            template.Should().Be(SequenceCondition);
        }

        [Test]
        public void SelectTemplate_ISequenceContainer_CorrectTemplateSelected() {
            var template = sut.SelectTemplate(new Mock<ISequenceContainer>().Object, default);

            template.Should().Be(SequenceContainer);
        }

        [Test]
        public void SelectTemplate_ISequenceTrigger_CorrectTemplateSelected() {
            var template = sut.SelectTemplate(new Mock<ISequenceTrigger>().Object, default);

            template.Should().Be(SequenceTrigger);
        }

        [Test]
        public void SelectTemplate_SomethingElse_DefaultTemplateSelected() {
            var template = sut.SelectTemplate(new object(), default);

            template.Should().Be(SequenceItem);
        }
    }
}