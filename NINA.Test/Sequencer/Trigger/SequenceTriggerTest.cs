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
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Trigger {

    [TestFixture]
    public class SequenceTriggerTest {
        public Mock<SequenceTrigger> sequenceTriggerMock;
        public Mock<ISequenceContainer> sequenceContainerMock;
        public Mock<IProgress<ApplicationStatus>> applicationStatusMock;

        [SetUp]
        public void Setup() {
            sequenceTriggerMock = new Mock<SequenceTrigger>();
            sequenceContainerMock = new Mock<ISequenceContainer>();
            applicationStatusMock = new Mock<IProgress<ApplicationStatus>>();
        }

        [Test]
        public async Task Trigger_Failed_ValidationFailure_Status() {
            //Arrange
            sequenceTriggerMock.CallBase = true;

            //Act
            sequenceTriggerMock
                .Setup(x => x.Execute(It.IsAny<ISequenceContainer>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new SequenceEntityFailedValidationException());
            var sut = sequenceTriggerMock.Object;

            //Assert
            try {
                await sut.Run(default, default, default);
            } catch (SequenceEntityFailedValidationException) {
            }

            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }

        /// <summary>
        /// Verifies trigger menu and enable commands toggle state and close the menu when the trigger is disabled.
        /// </summary>
        [Test]
        public void Commands_ToggleMenuAndDisabledState() {
            TestTrigger sut = new TestTrigger();

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
        /// Verifies a disabled trigger run exits without executing the trigger body.
        /// </summary>
        [Test]
        public async Task Run_DisabledTriggerSkipsExecution() {
            TestTrigger sut = new TestTrigger {
                Status = SequenceEntityStatus.DISABLED
            };

            await sut.Run(new SequentialContainer(), Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            sut.ExecuteCount.Should().Be(0);
            sut.Status.Should().Be(SequenceEntityStatus.DISABLED);
        }

        /// <summary>
        /// Verifies trigger run marks successful execution finished and resets the trigger runner before invoking the body.
        /// </summary>
        [Test]
        public async Task Run_SuccessfulTriggerSetsFinishedStatus() {
            TestTrigger sut = new TestTrigger();

            await sut.Run(new SequentialContainer(), Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            sut.ExecuteCount.Should().Be(1);
            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
        }

        /// <summary>
        /// Verifies trigger run converts cancellation back to the created state instead of treating it as a failure.
        /// </summary>
        [Test]
        public async Task Run_OperationCanceledResetsStatusToCreated() {
            TestTrigger sut = new TestTrigger {
                ExecuteAction = () => throw new OperationCanceledException()
            };

            await sut.Run(new SequentialContainer(), Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        /// <summary>
        /// Verifies validatable triggers fail before execution when validation reports issues.
        /// </summary>
        [Test]
        public async Task Run_InvalidValidatableTriggerFailsWithoutExecuting() {
            InvalidTrigger sut = new InvalidTrigger();

            await sut.Run(new SequentialContainer(), Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            sut.ExecuteCount.Should().Be(0);
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
            sut.Issues.Should().ContainSingle("invalid trigger");
        }

        /// <summary>
        /// Verifies detach and no-op lifecycle members keep the base trigger contract stable for concrete triggers.
        /// </summary>
        [Test]
        public void LifecycleAndDetachMembers_UseBaseNoOpBehavior() {
            Mock<ISequenceContainer> parentMock = new Mock<ISequenceContainer>();
            TestTrigger sut = new TestTrigger();

            sut.AttachNewParent(parentMock.Object);
            sut.DetachCommand.Execute(null);
            sut.Initialize();
            sut.SequenceBlockInitialize();
            sut.SequenceBlockStarted();
            sut.SequenceBlockFinished();
            sut.SequenceBlockTeardown();
            sut.Teardown();

            parentMock.Verify(x => x.Remove(It.Is<ISequenceTrigger>(trigger => trigger == sut)), Times.Once);
            sut.ShouldTriggerAfter(null, null).Should().BeFalse();
            sut.MoveUpCommand.Should().BeNull();
            sut.MoveDownCommand.Should().BeNull();
            sut.AskHasChanged("anything").Should().BeFalse();
            sut.Invoking(x => x.MoveUp()).Should().Throw<NotImplementedException>();
            sut.Invoking(x => x.MoveDown()).Should().Throw<NotImplementedException>();
        }

        private class TestTrigger : SequenceTrigger {
            public Action ExecuteAction { get; set; }
            public int ExecuteCount { get; private set; }

            public override object Clone() {
                return new TestTrigger();
            }

            public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
                ExecuteCount++;
                ExecuteAction?.Invoke();
                return Task.CompletedTask;
            }

            public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
                return true;
            }
        }

        private sealed class InvalidTrigger : TestTrigger, IValidatable {
            public IList<string> Issues { get; set; } = new List<string> { "invalid trigger" };

            public bool Validate() {
                return false;
            }
        }
    }
}
