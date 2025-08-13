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
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.View.Sequencer.Converter;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Test.Sequencer.View.Converter {

    [TestFixture]
    public class StrategyEvaluatesConditionsAndTriggersToVisibilityConverterTest {

        [Test]
        public void Convert_ParallelStrategy_Collapsed() {
            var container = new Mock<ISequenceContainer>();
            container.Setup(x => x.Strategy).Returns(new ParallelStrategy());

            var sut = new StrategyEvaluatesConditionsAndTriggersToVisibilityConverter();
            var conversion = sut.Convert(container.Object, default, default, default);

            conversion.Should().Be(Visibility.Collapsed);
        }

        [Test]
        public void Convert_OtherStrategy_Collapsed() {
            var container = new Mock<ISequenceContainer>();
            container.Setup(x => x.Strategy).Returns(new Mock<IExecutionStrategy>().Object);
            var sut = new StrategyEvaluatesConditionsAndTriggersToVisibilityConverter();
            var conversion = sut.Convert(container.Object, default, default, default);

            conversion.Should().Be(Visibility.Visible);
        }

        [Test]
        public void Convert_InvalidType_ThrowsArgumentException() {
            var sut = new StrategyEvaluatesConditionsAndTriggersToVisibilityConverter();
            Action act = () => sut.Convert(new object(), default, default, default);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Convert_Null_VisibilityCollapsed() {
            var sut = new StrategyEvaluatesConditionsAndTriggersToVisibilityConverter();
            var conversion = sut.Convert(null, default, default, default);

            conversion.Should().BeOfType<Visibility>();
            conversion.As<Visibility>().Should().Be(Visibility.Collapsed);
        }

        [Test]
        public void ConvertBack_NotImplemented() {
            var sut = new StrategyEvaluatesConditionsAndTriggersToVisibilityConverter();

            Action act = () => sut.ConvertBack(default, default, default, default);

            act.Should().Throw<NotImplementedException>();
        }
    }
}