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
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem {

    [TestFixture]
    public class SequenceItemTest {

        [Test]
        public void AttachNewParent_PreviousWasNull_NewParentAttached() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;
            var testContainerMock = new Mock<ISequenceContainer>();

            var newParent = testContainerMock.Object;

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(newParent);

            //Assert
            sut.Parent.Should().Be(newParent);
            mock.Verify(x => x.AfterParentChanged(), Times.Once);
        }

        [Test]
        public async Task Run_Successfully_StatusToComplete() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
        }

        [Test]
        public async Task Run_Successfully_MultiAttempts_RunOnlyOnce_StatusToComplete() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.Attempts = 10;
            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
            mock.Verify(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Run_Successfully_MultiAttempts_FailTwiceFinishOnThird_StatusToComplete() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            mock.SetupSequence(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("Failed to run"))
                .Throws(new Exception("Failed to run"))
                .Returns(Task.CompletedTask);

            //Act
            var sut = mock.Object;
            sut.Attempts = 10;
            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
            mock.Verify(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task Run_Unsuccessfully_StatusToFailed() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            mock.Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Throws(new Exception("Failed to run"));
            var sut = mock.Object;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }

        [Test]
        public async Task Run_Cancelled_StatusToCreated() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            mock
                .Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
            var sut = mock.Object;

            var cts = new CancellationTokenSource();

            try {
                cts.Cancel();
                await sut.Run(default, cts.Token);
                Assert.Fail(); // When no exception is thrown
            } catch (OperationCanceledException) {
            }

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public async Task Run_Failed_ValidationFailure() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            mock
                .Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new SequenceEntityFailedValidationException());
            var sut = mock.Object;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }

        [Test]
        public async Task Run_Skipped_StatusToCreated() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            mock
                .Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new SequenceItemSkippedException());
            var sut = mock.Object;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.SKIPPED);
        }

        [Test]
        public async Task ResetProgress() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            await sut.Run(default, default);
            sut.ResetProgress();

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public void Skip() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;
            mock.SetupGet(x => x.Attempts).Returns(1);

            //Act
            var sut = mock.Object;
            sut.Skip();

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.SKIPPED);
        }

        [Test]
        public void GetEstimatedDuration() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.GetEstimatedDuration().Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void MoveUp() {
            var parent = new Mock<ISequenceContainer>();
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.MoveUp();

            parent.Verify(x => x.MoveUp(It.Is<ISequenceItem>(s => s == sut)), Times.Once);
        }

        [Test]
        public void MoveDown() {
            var parent = new Mock<ISequenceContainer>();
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.MoveDown();

            parent.Verify(x => x.MoveDown(It.Is<ISequenceItem>(s => s == sut)), Times.Once);
        }

        [Test]
        public void Detach() {
            var parent = new Mock<ISequenceContainer>();
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.Detach();

            parent.Verify(x => x.Remove(It.Is<ISequenceItem>(s => s == sut)), Times.Once);
        }

        [Test]
        public async Task Run_Unsuccessfully_After3Retries_StatusToFailed() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            mock
                .SetupSequence(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("Failed to run"))
                .Throws(new Exception("Failed to run"))
                .Throws(new Exception("Failed to run"));

            //Act
            var sut = mock.Object;
            sut.Attempts = 3;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
            mock.Verify(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task Run_Successfully_After3Retries_StatusToFinished() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            mock
                .SetupSequence(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("Failed to run"))
                .Throws(new Exception("Failed to run"))
                .Returns(Task.CompletedTask);

            //Act
            var sut = mock.Object;
            sut.Attempts = 3;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
            mock.Verify(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task Run_Unsuccessfully_StatusToFailed_SkipInstructionSetOnError_ProperErrorActionTaken() {
            var parent = new Mock<ISequenceContainer>();

            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;
            mock.Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Throws(new Exception("Failed to run"));

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.ErrorBehavior = NINA.Sequencer.Utility.InstructionErrorBehavior.SkipInstructionSetOnError;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
            parent.Verify(x => x.Interrupt(), Times.Once);
        }

        [Test]
        public async Task Run_Unsuccessfully_StatusToFailed_AbortOnError_ProperErrorActionTaken() {
            var root = new Mock<ISequenceRootContainer>();
            var parent = new Mock<ISequenceContainer>();
            parent.Setup(x => x.Parent).Returns(root.Object);

            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;
            mock.Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Throws(new Exception("Failed to run"));

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.ErrorBehavior = NINA.Sequencer.Utility.InstructionErrorBehavior.AbortOnError;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
            parent.Verify(x => x.Interrupt(), Times.Never);
            root.Verify(x => x.Interrupt(), Times.Once);
        }

        [Test]
        public async Task Run_Unsuccessfully_StatusToFailed_SkipToSequenceEndInstructions_ProperErrorActionTaken() {
            var root = new Mock<ISequenceRootContainer>();
            var parent = new Mock<ISequenceContainer>();

            var start = new Mock<ISequenceContainer>();
            start.Setup(x => x.Status).Returns(SequenceEntityStatus.RUNNING);
            var target = new Mock<ISequenceContainer>();
            target.Setup(x => x.Status).Returns(SequenceEntityStatus.RUNNING);
            var end = new Mock<ISequenceContainer>();
            end.Setup(x => x.Status).Returns(SequenceEntityStatus.RUNNING);
            parent.Setup(x => x.Parent).Returns(target.Object);
            start.Setup(x => x.Parent).Returns(root.Object);
            target.Setup(x => x.Parent).Returns(root.Object);
            end.Setup(x => x.Parent).Returns(root.Object);

            root.Setup(x => x.Items).Returns(new List<ISequenceItem>() { start.Object, target.Object, end.Object });

            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;
            mock.Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Throws(new Exception("Failed to run"));

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.ErrorBehavior = NINA.Sequencer.Utility.InstructionErrorBehavior.SkipToSequenceEndInstructions;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);

            await WaitUntilVerifiedAsync(parent, x => x.Interrupt(), Times.Exactly(0), TimeSpan.FromSeconds(1));
            await WaitUntilVerifiedAsync(root, x => x.Interrupt(), Times.Exactly(0), TimeSpan.FromSeconds(1));
            await WaitUntilVerifiedAsync(start, x => x.Interrupt(), Times.Exactly(1), TimeSpan.FromSeconds(1));
            await WaitUntilVerifiedAsync(target, x => x.Interrupt(), Times.Exactly(1), TimeSpan.FromSeconds(1));
            await WaitUntilVerifiedAsync(end, x => x.Interrupt(), Times.Exactly(0), TimeSpan.FromSeconds(1));
        }

        public static async Task WaitUntilVerifiedAsync<T>(
            Mock<T> mock,
            Expression<Action<T>> expression,
            Times times,
            TimeSpan timeout,
            int pollIntervalMs = 10)
            where T : class {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout) {
                try {
                    mock.Verify(expression, times);
                    return;
                } catch (MockException) {
                    await Task.Delay(pollIntervalMs);
                }
            }

            // Fail after timeout
            mock.Verify(expression, times);
        }

    }
}