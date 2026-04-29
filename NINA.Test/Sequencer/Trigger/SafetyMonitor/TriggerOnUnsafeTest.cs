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
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.SafetyMonitor;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Test.Sequencer.Trigger.SafetyMonitor {

    [TestFixture]
    public class TriggerOnUnsafeTest {
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private Mock<IApplicationResourceDictionary> resourceDictionaryMock;
        private SafetyMonitorInfo safetyMonitorInfo;
        private TriggerOnUnsafe sut;

        [SetUp]
        public void Setup() {
            safetyMonitorInfo = new SafetyMonitorInfo() {
                Connected = true,
                IsSafe = true
            };

            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            safetyMonitorMediatorMock.Setup(x => x.GetInfo()).Returns(() => safetyMonitorInfo);

            resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            resourceDictionaryMock.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());

            sut = new TriggerOnUnsafe(safetyMonitorMediatorMock.Object, resourceDictionaryMock.Object) {
                Name = "Trigger On Unsafe",
                Description = "Description",
                Category = "Safety",
                Icon = new GeometryGroup()
            };
        }

        /// <summary>
        /// Verifies cloning preserves trigger metadata and creates independent nested trigger instructions.
        /// </summary>
        [Test]
        public void Clone_CopiesMetadataAndNestedSteps() {
            sut.IsExpanded = false;

            TriggerOnUnsafe clone = (TriggerOnUnsafe)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be(sut.Name);
            clone.Description.Should().Be(sut.Description);
            clone.Category.Should().Be(sut.Category);
            clone.Icon.Should().BeSameAs(sut.Icon);
            clone.BeforeWaitForSafe.Should().NotBeSameAs(sut.BeforeWaitForSafe);
            clone.AfterWaitForSafe.Should().NotBeSameAs(sut.AfterWaitForSafe);
            clone.WaitUntilSafe.Should().NotBeSameAs(sut.WaitUntilSafe);
            clone.IsExpanded.Should().BeFalse();
        }

        /// <summary>
        /// Verifies a disconnected safety monitor does not trigger before this sequence context has observed a connection.
        /// </summary>
        [Test]
        public void ShouldTrigger_DisconnectedBeforeSafetyMonitorWasConnected_ReturnsFalse() {
            safetyMonitorInfo.Connected = false;
            safetyMonitorInfo.IsSafe = false;

            sut.ShouldTrigger(null, null).Should().BeFalse();
            sut.ShouldTriggerAfter(null, null).Should().BeFalse();
        }

        /// <summary>
        /// Verifies an actively connected unsafe safety monitor queues the trigger.
        /// </summary>
        [Test]
        public void ShouldTrigger_ConnectedUnsafe_ReturnsTrue() {
            safetyMonitorInfo.Connected = true;
            safetyMonitorInfo.IsSafe = false;

            sut.ShouldTrigger(null, null).Should().BeTrue();
        }

        /// <summary>
        /// Verifies disconnecting after a known connection is treated as unsafe and queues the trigger.
        /// </summary>
        [Test]
        public void ShouldTrigger_DisconnectedAfterSafetyMonitorWasConnected_ReturnsTrue() {
            safetyMonitorInfo.Connected = true;
            safetyMonitorInfo.IsSafe = true;

            sut.ShouldTrigger(null, null).Should().BeFalse();

            safetyMonitorInfo.Connected = false;

            sut.ShouldTrigger(null, null).Should().BeTrue();
        }

        /// <summary>
        /// Verifies the disconnected event does not interrupt running work before the monitor has connected once.
        /// </summary>
        [Test]
        public void DisconnectedEvent_BeforeFirstConnection_DoesNotInterruptRunningItems() {
            safetyMonitorInfo.Connected = false;
            safetyMonitorInfo.IsSafe = false;
            Mock<ISequenceItem> runningItemMock = AttachToRunningRoot();

            safetyMonitorMediatorMock.Raise(x => x.Disconnected += null, safetyMonitorMediatorMock.Object, EventArgs.Empty);

            runningItemMock.Verify(x => x.Skip(), Times.Never);
            runningItemMock.Verify(x => x.ResetProgress(), Times.Never);
        }

        /// <summary>
        /// Verifies the disconnected event interrupts and resets running work after a prior monitor connection.
        /// </summary>
        [Test]
        public void DisconnectedEvent_AfterFirstConnection_InterruptsRunningItems() {
            safetyMonitorInfo.Connected = false;
            safetyMonitorInfo.IsSafe = false;
            Mock<ISequenceItem> runningItemMock = AttachToRunningRoot();

            safetyMonitorInfo.Connected = true;
            safetyMonitorInfo.IsSafe = true;
            safetyMonitorMediatorMock.Raise(x => x.Connected += null, safetyMonitorMediatorMock.Object, EventArgs.Empty);

            safetyMonitorInfo.Connected = false;
            safetyMonitorMediatorMock.Raise(x => x.Disconnected += null, safetyMonitorMediatorMock.Object, EventArgs.Empty);

            runningItemMock.Verify(x => x.Skip(), Times.Once);
            runningItemMock.Verify(x => x.ResetProgress(), Times.Once);
        }

        /// <summary>
        /// Verifies an unsafe state-change event interrupts and resets running work.
        /// </summary>
        [Test]
        public void IsSafeChanged_ToUnsafe_InterruptsRunningItems() {
            safetyMonitorInfo.Connected = true;
            safetyMonitorInfo.IsSafe = true;
            Mock<ISequenceItem> runningItemMock = AttachToRunningRoot();

            safetyMonitorInfo.IsSafe = false;
            safetyMonitorMediatorMock.Raise(x => x.IsSafeChanged += null, safetyMonitorMediatorMock.Object, new IsSafeEventArgs(false));

            runningItemMock.Verify(x => x.Skip(), Times.Once);
            runningItemMock.Verify(x => x.ResetProgress(), Times.Once);
        }

        /// <summary>
        /// Verifies an unsafe state-change event cancels another running trigger's execution token even when it has no child item.
        /// </summary>
        [Test]
        public async Task IsSafeChanged_ToUnsafe_InterruptsRunningTriggerToken() {
            safetyMonitorInfo.Connected = true;
            safetyMonitorInfo.IsSafe = true;
            SequenceRootContainer root = new SequenceRootContainer() {
                Status = SequenceEntityStatus.RUNNING
            };
            TokenObservingTrigger runningTrigger = new TokenObservingTrigger();
            runningTrigger.AttachNewParent(root);
            sut.AttachNewParent(root);

            Task runTask = runningTrigger.Run(root, Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);
            await runningTrigger.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

            safetyMonitorInfo.IsSafe = false;
            safetyMonitorMediatorMock.Raise(x => x.IsSafeChanged += null, safetyMonitorMediatorMock.Object, new IsSafeEventArgs(false));
            await runTask.WaitAsync(TimeSpan.FromSeconds(5));

            runningTrigger.ObservedCancellation.Should().BeTrue();
            runningTrigger.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        /// <summary>
        /// Verifies executing the trigger builds the expected nested instruction list and resets progress afterwards.
        /// </summary>
        [Test]
        public async Task Execute_WhenAlreadySafe_RunsAndResetsNestedSteps() {
            safetyMonitorInfo.Connected = true;
            safetyMonitorInfo.IsSafe = true;

            await sut.Execute(new SequentialContainer(), Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            sut.BeforeWaitForSafe.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.WaitUntilSafe.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.AfterWaitForSafe.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.TriggerRunner.GetItemsSnapshot().Should().HaveCount(3);
        }

        [Test]
        public async Task Execute_WhenCollapsed_ExpandsDuringRunAndRestoresCollapsedState() {
            safetyMonitorInfo.Connected = true;
            safetyMonitorInfo.IsSafe = true;
            bool? expandedDuringBeforeWaitForSafe = null;
            sut.IsExpanded = false;
            sut.BeforeWaitForSafe.Add(new TestInstruction() {
                ExecuteAction = () => expandedDuringBeforeWaitForSafe = sut.IsExpanded
            });

            await sut.Execute(new SequentialContainer(), Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            expandedDuringBeforeWaitForSafe.Should().BeTrue();
            sut.IsExpanded.Should().BeFalse();
        }

        /// <summary>
        /// Verifies runtime execution uses an isolated context so before/after instruction sets do not evaluate sibling triggers on the live parent container.
        /// </summary>
        [Test]
        public async Task Execute_DoesNotEvaluateParentTriggerSetDuringBeforeWaitForSafeExecution() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer context = new SequentialContainer();
            ObservingTrigger siblingTrigger = new ObservingTrigger();
            TestInstruction instruction = new TestInstruction();

            root.Add(context);
            context.Add(siblingTrigger);
            context.Add(sut);
            sut.BeforeWaitForSafe.Add(instruction);

            await sut.Execute(context, Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            instruction.ExecuteCount.Should().Be(1);
            siblingTrigger.ShouldTriggerCount.Should().Be(0);
            siblingTrigger.ShouldTriggerAfterCount.Should().Be(0);
        }

        /// <summary>
        /// Verifies validation remains non-blocking when the monitor is disconnected.
        /// </summary>
        [Test]
        public void Validate_SafetyMonitorDisconnected_DoesNotReportSafetyMonitorIssue() {
            safetyMonitorInfo.Connected = false;
            safetyMonitorInfo.IsSafe = false;

            sut.Validate().Should().BeTrue();
            sut.Issues.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies nested instruction container issues are exposed while validation still returns true.
        /// </summary>
        [Test]
        public void Validate_InstructionContainerIssues_ReturnsTrueAndReportsIssues() {
            sut.BeforeWaitForSafe = new IssueReportingSequentialContainer("before issue");
            sut.AfterWaitForSafe = new IssueReportingSequentialContainer("after issue");

            sut.Validate().Should().BeTrue();
            sut.Issues.Should().Equal("before issue", "after issue");
        }

        /// <summary>
        /// Verifies the trigger string representation identifies the trigger type.
        /// </summary>
        [Test]
        public void ToString_ReturnsTriggerName() {
            sut.ToString().Should().Be("Trigger: TriggerOnUnsafe");
        }

        /// <summary>
        /// Attaches the trigger to a running root container with one mocked running item.
        /// </summary>
        private Mock<ISequenceItem> AttachToRunningRoot() {
            Mock<ISequenceItem> runningItemMock = new Mock<ISequenceItem>();
            runningItemMock.SetupGet(x => x.Status).Returns(SequenceEntityStatus.RUNNING);

            SequenceRootContainer root = new SequenceRootContainer() {
                Status = SequenceEntityStatus.RUNNING
            };
            root.AddRunningItem(runningItemMock.Object);

            sut.AttachNewParent(root);

            return runningItemMock;
        }

        /// <summary>
        /// Test-only container that simulates nested instruction validation feedback.
        /// </summary>
        private sealed class IssueReportingSequentialContainer : SequentialContainer {
            private readonly string issue;

            /// <summary>
            /// Initializes a container that reports the specified issue during validation.
            /// </summary>
            public IssueReportingSequentialContainer(string issue) {
                this.issue = issue;
            }

            /// <summary>
            /// Reports the configured issue and returns false to prove trigger validation remains non-blocking.
            /// </summary>
            public override bool Validate() {
                Issues.Clear();
                Issues.Add(issue);
                return false;
            }
        }

        private sealed class ObservingTrigger : SequenceTrigger {
            public int ShouldTriggerCount { get; private set; }
            public int ShouldTriggerAfterCount { get; private set; }

            public override object Clone() {
                return new ObservingTrigger();
            }

            public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
                ShouldTriggerCount++;
                return false;
            }

            public override bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem) {
                ShouldTriggerAfterCount++;
                return false;
            }

            public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
                return Task.CompletedTask;
            }
        }

        private sealed class TokenObservingTrigger : SequenceTrigger {
            public TaskCompletionSource<bool> Started { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool ObservedCancellation { get; private set; }

            public override object Clone() {
                return new TokenObservingTrigger();
            }

            public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
                return true;
            }

            public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
                Started.TrySetResult(true);
                try {
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                } catch (OperationCanceledException) when (token.IsCancellationRequested) {
                    ObservedCancellation = true;
                    throw;
                }
            }
        }

        private sealed class TestInstruction : NINA.Sequencer.SequenceItem.SequenceItem {
            public int ExecuteCount { get; private set; }
            public Action? ExecuteAction { get; init; }

            public override object Clone() {
                return new TestInstruction();
            }

            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
                ExecuteCount++;
                ExecuteAction?.Invoke();
                return Task.CompletedTask;
            }
        }
    }
}
