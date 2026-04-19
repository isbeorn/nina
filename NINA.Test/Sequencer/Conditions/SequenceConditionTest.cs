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
using NINA.Core.Enum;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
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
            public Action CheckAction { get; set; }
            public int CheckCount { get; private set; }
            public bool CheckResult { get; set; }

            public override bool Check(ISequenceItem prevItem, ISequenceItem nextItem) {
                CheckCount++;
                CheckAction?.Invoke();
                return CheckResult;
            }

            public override object Clone() {
                return new SeuqenceConditionImpl();
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

        /// <summary>
        /// Verifies condition menu and enable commands toggle state and prevent menu opening while disabled.
        /// </summary>
        [Test]
        public void Commands_ToggleMenuAndDisabledState() {
            SeuqenceConditionImpl sut = new SeuqenceConditionImpl();

            sut.ShowMenuCommand.Execute(null);
            sut.ShowMenu.Should().BeTrue();

            sut.DisableEnableCommand.Execute(null);

            sut.Status.Should().Be(SequenceEntityStatus.DISABLED);
            sut.ShowMenu.Should().BeFalse();
            sut.ShowMenuCommand.CanExecute(null).Should().BeFalse();

            sut.DisableEnableCommand.Execute(null);

            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.ShowMenuCommand.CanExecute(null).Should().BeTrue();
        }

        /// <summary>
        /// Verifies disabled conditions return false without running the concrete condition check.
        /// </summary>
        [Test]
        public void RunCheck_DisabledConditionSkipsCheck() {
            SeuqenceConditionImpl sut = new SeuqenceConditionImpl {
                Status = SequenceEntityStatus.DISABLED
            };

            sut.RunCheck(null, null).Should().BeFalse();

            sut.CheckCount.Should().Be(0);
        }

        /// <summary>
        /// Verifies successful condition checks return the concrete result and leave status unchanged.
        /// </summary>
        [Test]
        public void RunCheck_SuccessReturnsConcreteCheckResult() {
            SeuqenceConditionImpl sut = new SeuqenceConditionImpl {
                CheckResult = true
            };

            sut.RunCheck(null, null).Should().BeTrue();

            sut.CheckCount.Should().Be(1);
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        /// <summary>
        /// Verifies condition check exceptions are caught, marked failed, and surfaced as a false run-check result.
        /// </summary>
        [Test]
        public void RunCheck_ExceptionMarksConditionFailed() {
            SeuqenceConditionImpl sut = new SeuqenceConditionImpl {
                CheckAction = () => throw new InvalidOperationException("boom")
            };

            sut.RunCheck(null, null).Should().BeFalse();

            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }

        /// <summary>
        /// Verifies validatable conditions fail before checking when validation reports issues.
        /// </summary>
        [Test]
        public void RunCheck_InvalidValidatableConditionFailsWithoutChecking() {
            InvalidCondition sut = new InvalidCondition();

            sut.RunCheck(null, null).Should().BeFalse();

            sut.CheckCount.Should().Be(0);
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
            sut.Issues.Should().ContainSingle("invalid condition");
        }

        /// <summary>
        /// Verifies watchdogs start only when the condition is inside a sequence root and cancel outside the runnable tree.
        /// </summary>
        [Test]
        public void RunWatchdogIfInsideSequenceRoot_StartsInsideRootAndCancelsOutsideRoot() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer child = new SequentialContainer();
            WatchdogCondition sut = new WatchdogCondition();
            Mock<IConditionWatchdog> watchdogMock = new Mock<IConditionWatchdog>();
            sut.ConditionWatchdog = watchdogMock.Object;
            root.Add(child);
            child.Add(sut);

            sut.RunWatchdog();

            watchdogMock.Verify(x => x.Start(), Times.Once);

            sut.AttachNewParent(new SequentialContainer());
            sut.RunWatchdog();

            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        /// <summary>
        /// Verifies active-state detection requires a root-contained running parent and an enabled condition.
        /// </summary>
        [Test]
        public void IsActive_RequiresRootRunningParentAndEnabledCondition() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer child = new SequentialContainer();
            WatchdogCondition sut = new WatchdogCondition();
            root.Add(child);
            child.Add(sut);

            sut.IsConditionActive().Should().BeFalse();

            child.Status = SequenceEntityStatus.RUNNING;
            sut.IsConditionActive().Should().BeTrue();

            sut.Status = SequenceEntityStatus.DISABLED;
            sut.IsConditionActive().Should().BeFalse();
        }

        private class WatchdogCondition : SeuqenceConditionImpl {
            public void RunWatchdog() {
                RunWatchdogIfInsideSequenceRoot();
            }

            public bool IsConditionActive() {
                return IsActive();
            }
        }

        private sealed class InvalidCondition : SeuqenceConditionImpl, IValidatable {
            public IList<string> Issues { get; set; } = new List<string> { "invalid condition" };

            public bool Validate() {
                return false;
            }
        }
    }
}
