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
using NINA.Core.Enum;
using NINA.View.Sequencer.Converter;
using NUnit.Framework;
using System;
using System.Windows;

namespace NINA.Test.Sequencer.View.Converter {

    [TestFixture]
    public class SequenceEntityStatusRunningToVisibilityConverterTest {

        [Test]
        public void Convert_Null_VisibilityCollapsed() {
            var sut = new SequenceEntityStatusRunningToVisibilityConverter();
            var conversion = sut.Convert(null, default, default, default);

            conversion.Should().BeOfType<Visibility>();
            conversion.As<Visibility>().Should().Be(Visibility.Collapsed);
        }

        [Test]
        [TestCase(SequenceEntityStatus.CREATED, Visibility.Collapsed)]
        [TestCase(SequenceEntityStatus.FAILED, Visibility.Collapsed)]
        [TestCase(SequenceEntityStatus.FINISHED, Visibility.Collapsed)]
        [TestCase(SequenceEntityStatus.RUNNING, Visibility.Visible)]
        [TestCase(SequenceEntityStatus.SKIPPED, Visibility.Collapsed)]
        public void Convert_EntitySkipped_VisibilityCollapsed(SequenceEntityStatus status, Visibility expected) {
            var sut = new SequenceEntityStatusRunningToVisibilityConverter();
            var conversion = sut.Convert(status, default, default, default);

            conversion.Should().BeOfType<Visibility>();
            conversion.As<Visibility>().Should().Be(expected);
        }

        [Test]
        public void ConvertBack_NotImplemented() {
            var sut = new SequenceEntityStatusRunningToVisibilityConverter();

            Action act = () => sut.ConvertBack(default, default, default, default);

            act.Should().Throw<NotImplementedException>();
        }
    }
}