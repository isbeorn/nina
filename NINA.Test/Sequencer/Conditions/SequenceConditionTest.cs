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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NUnit.Framework;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class SequenceConditionTest {

        private class SeuqenceConditionImpl : SequenceCondition {

            public override bool Check(ISequenceItem prevItem, ISequenceItem nextItem) {
                throw new NotImplementedException();
            }

            public override object Clone() {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void AttachNewParent_Null_Test() {
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(null);

            sut.Parent.Should().BeNull();
        }

        [Test]
        public void AttachNewParent_NewParentAttached_Test() {
            var parent = new Mock<ISequenceContainer>();
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(parent.Object);

            sut.Parent.Should().Be(parent.Object);
        }

        [Test]
        public void Detach_Test() {
            var parent = new Mock<ISequenceContainer>();
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(parent.Object);
            sut.Detach();

            parent.Verify(x => x.Remove(It.Is<ISequenceCondition>(c => c == sut)), Times.Once);
        }

        [Test]
        public void MoveUp_Test() {
            var parent = new Mock<ISequenceContainer>();
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(parent.Object);
            Action act = () => sut.MoveUp();
            act.Should().Throw<NotImplementedException>();
        }

        [Test]
        public void MoveDown_Test() {
            var parent = new Mock<ISequenceContainer>();
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(parent.Object);
            Action act = () => sut.MoveDown();
            act.Should().Throw<NotImplementedException>();
        }

        [Test]
        public void ShowMenu_Test() {
            var sut = new SeuqenceConditionImpl();
            sut.ShowMenu = true;

            sut.ShowMenu.Should().BeTrue();
        }

        [Test]
        public void ResetProgress_ShowMenuTest() {
            var sut = new SeuqenceConditionImpl();
            sut.ShowMenu = true;
            sut.ResetProgressCommand.Execute(default);

            sut.ShowMenu.Should().BeFalse();
        }

        [Test]
        public virtual void ResetProgress_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.ResetProgress();
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public virtual void Initialize_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.Initialize();
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public virtual void SequenceBlockInitialize_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.SequenceBlockInitialize();
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public virtual void SequenceBlockStarted_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.SequenceBlockStarted();
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public virtual void SequenceBlockFinished_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.SequenceBlockFinished();
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public virtual void SequenceBlockTeardown_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.SequenceBlockTeardown();
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public virtual void Teardown_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.Teardown();
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public virtual void MoveUp_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.Invoking(x => x.MoveUp()).Should().Throw<NotImplementedException>();
        }

        [Test]
        public virtual void MoveDown_NoOp() {
            var sut = new SeuqenceConditionImpl();
            sut.Invoking(x => x.MoveDown()).Should().Throw<NotImplementedException>();
        }

        [Test]
        public virtual void MoveUp_IsNull() {
            var sut = new SeuqenceConditionImpl();
            sut.MoveUpCommand.Should().BeNull();
        }

        [Test]
        public virtual void MoveDown_IsNull() {
            var sut = new SeuqenceConditionImpl();
            sut.MoveDownCommand.Should().BeNull();
        }

        [Test]
        public virtual void Detach_HasNoParent_NoOp() {
            var sut = new SeuqenceConditionImpl();

            sut.DetachCommand.Execute(default);
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public virtual void Detach_HasParent_CallsRemove() {
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new SeuqenceConditionImpl();
            sut.Parent = parentMock.Object;

            sut.DetachCommand.Execute(default);

            parentMock.Verify(x => x.Remove(It.Is<ISequenceCondition>(y => y == sut)));
        }

        [Test]
        public virtual void ShowMenuCommand_FlipsShowMenu() {
            var sut = new SeuqenceConditionImpl();

            sut.ShowMenu = true;
            sut.ShowMenuCommand.Execute(default);

            sut.ShowMenu.Should().BeFalse();
        }
    }
}