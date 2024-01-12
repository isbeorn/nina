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
using NINA.Sequencer.DragDrop;
using NINA.Core.Enum;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.DragDrop {

    [TestFixture]
    public class DropIntoParametersTest {

        [Test]
        public void ctor_IDroppable_null_Test() {
            var sut = new DropIntoParameters(null);

            sut.Source.Should().BeNull();
            sut.Target.Should().BeNull();
            sut.Position.Should().BeNull();
        }

        [Test]
        public void ctor_IDroppable_notnull_Test() {
            var droppableMock = new Mock<IDroppable>();
            var sut = new DropIntoParameters(droppableMock.Object);

            sut.Source.Should().BeSameAs(droppableMock.Object);
            sut.Target.Should().BeNull();
            sut.Position.Should().BeNull();
        }

        [Test]
        public void ctor_IDroppable_IDroppable_null_Test() {
            var sut = new DropIntoParameters(null, null);

            sut.Source.Should().BeNull();
            sut.Target.Should().BeNull();
            sut.Position.Should().BeNull();
        }

        [Test]
        public void ctor_IDroppable_IDroppable_Test() {
            var droppableMock = new Mock<IDroppable>();
            var droppable2Mock = new Mock<IDroppable>();
            var sut = new DropIntoParameters(droppableMock.Object, droppable2Mock.Object);

            sut.Source.Should().BeSameAs(droppableMock.Object);
            sut.Target.Should().BeSameAs(droppable2Mock.Object);
            sut.Position.Should().BeNull();
        }

        [Test]
        public void ctor_IDroppable_IDroppable_DropTargetEnum_null_Test() {
            var sut = new DropIntoParameters(null, null, null);

            sut.Source.Should().BeNull();
            sut.Target.Should().BeNull();
            sut.Position.Should().BeNull();
        }

        [Test]
        public void ctor_IDroppable_IDroppable_DropTargetEnum_Test() {
            var droppableMock = new Mock<IDroppable>();
            var droppable2Mock = new Mock<IDroppable>();
            var sut = new DropIntoParameters(droppableMock.Object, droppable2Mock.Object, DropTargetEnum.Center);

            sut.Source.Should().BeSameAs(droppableMock.Object);
            sut.Target.Should().BeSameAs(droppable2Mock.Object);
            sut.Position.Should().Be(DropTargetEnum.Center);
        }
    }
}