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
using NINA.Core.Enum;
using NINA.Sequencer.DragDrop;
using NINA.View.Sequencer.Converter;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.View.Converter {

    [TestFixture]
    public class DroppableToDropIntoParametersConverterTest {

        [Test]
        public void Convert_null_ToDropIntoParameters() {
            var sut = new DroppableToDropIntoParametersConverter();
            var conversion = sut.Convert(null, default, default, default);

            conversion.Should().BeOfType<DropIntoParameters>();
            conversion.As<DropIntoParameters>().Position.Should().Be(DropTargetEnum.Center);
        }

        [Test]
        public void Convert_IDroppable_ToDropIntoParameters() {
            var droppable = new Mock<IDroppable>();

            var sut = new DroppableToDropIntoParametersConverter();
            var conversion = sut.Convert(droppable.Object, default, default, default);

            conversion.Should().BeOfType<DropIntoParameters>();
            conversion.As<DropIntoParameters>().Position.Should().Be(DropTargetEnum.Center);
        }

        [Test]
        public void ConvertBack_NotImplemented() {
            var sut = new DroppableToDropIntoParametersConverter();

            Action act = () => sut.ConvertBack(default, default, default, default);

            act.Should().Throw<NotImplementedException>();
        }
    }
}