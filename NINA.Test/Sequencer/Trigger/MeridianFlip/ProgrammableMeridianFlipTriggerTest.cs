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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Test.Sequencer.Trigger.MeridianFlip {

    [TestFixture]
    public class ProgrammableMeridianFlipTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IMeridianFlipVMFactory> meridianFlipVMFactoryMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IDomeFollower> domeFollowerMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IImageHistoryVM> historyMock;
        private Mock<IAutoFocusVMFactory> autoFocusVMFactoryMock;
        private Mock<IPlateSolverFactory> plateSolverFactoryMock;
        private Mock<IWindowServiceFactory> windowServiceFactoryMock;
        private Mock<IApplicationResourceDictionary> resourceDictionaryMock;
        private TelescopeInfo telescopeInfo;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            meridianFlipVMFactoryMock = new Mock<IMeridianFlipVMFactory>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            domeFollowerMock = new Mock<IDomeFollower>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            historyMock = new Mock<IImageHistoryVM>();
            autoFocusVMFactoryMock = new Mock<IAutoFocusVMFactory>();
            plateSolverFactoryMock = new Mock<IPlateSolverFactory>();
            windowServiceFactoryMock = new Mock<IWindowServiceFactory>();
            resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();

            telescopeInfo = new TelescopeInfo() {
                Connected = true,
                TrackingEnabled = true,
                TimeToMeridianFlip = 0
            };

            resourceDictionaryMock.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(() => telescopeInfo);
            telescopeMediatorMock.Setup(x => x.SetTrackingEnabled(It.IsAny<bool>()))
                .Callback<bool>(enabled => telescopeInfo.TrackingEnabled = enabled)
                .Returns(true);
            telescopeMediatorMock.Setup(x => x.MeridianFlip(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            telescopeMediatorMock.Setup(x => x.RaiseBeforeMeridianFlip(It.IsAny<BeforeMeridianFlipEventArgs>())).Returns(Task.CompletedTask);
            telescopeMediatorMock.Setup(x => x.RaiseAfterMeridianFlip(It.IsAny<AfterMeridianFlipEventArgs>())).Returns(Task.CompletedTask);
            telescopeMediatorMock.Setup(x => x.GetCurrentPosition()).Returns(new Coordinates(Angle.ByHours(10), Angle.ByDegree(20), Epoch.JNOW));
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = false });

            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo() { Connected = false });
            guiderMediatorMock.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            guiderMediatorMock.Setup(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            guiderMediatorMock.Setup(x => x.AutoSelectGuideStar(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            guiderMediatorMock.Setup(x => x.GetDevice()).Returns(new Mock<IGuider>().Object);

            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = false });
            imagingMediatorMock.Setup(x => x.GetImageRotation()).Returns(0);
        }

        [Test]
        public void AfterParentChanged_SeedsDefaultsFromProfileOnce() {
            SetupMeridianFlipSettings(minutesAfter: 0, maxMinutesAfter: 0, pauseBefore: 0, useSideOfPier: false, settleTime: 0, autofocusAfterFlip: true, recenter: true);
            ProgrammableMeridianFlipTrigger sut = CreateSut();
            SequentialContainer parent = new SequentialContainer();

            sut.BeforeFlipActions.Items.Should().BeEmpty();
            sut.AfterFlipActions.Items.Should().BeEmpty();

            parent.Add(sut);

            sut.BeforeFlipActions.Items.Should().BeEmpty();
            sut.AfterFlipActions.Items.Should().HaveCount(2);
            sut.AfterFlipActions.Items[0].Should().BeOfType<RunAutofocus>();
            sut.AfterFlipActions.Items[1].Should().BeOfType<Center>();
        }

        [Test]
        public void Clone_CopiesNestedActionsWithoutReseeding() {
            SetupMeridianFlipSettings(minutesAfter: 0, maxMinutesAfter: 0, pauseBefore: 0, useSideOfPier: false, settleTime: 0, autofocusAfterFlip: true, recenter: true);
            ProgrammableMeridianFlipTrigger sut = CreateSut();
            SequentialContainer originalParent = new SequentialContainer();
            SequentialContainer cloneParent = new SequentialContainer();

            originalParent.Add(sut);
            sut.AfterFlipActions.Add(new RecordingInstruction());
            sut.IsExpanded = false;

            ProgrammableMeridianFlipTrigger clone = (ProgrammableMeridianFlipTrigger)sut.Clone();
            cloneParent.Add(clone);

            clone.Should().NotBeSameAs(sut);
            clone.BeforeFlipActions.Should().NotBeSameAs(sut.BeforeFlipActions);
            clone.AfterFlipActions.Should().NotBeSameAs(sut.AfterFlipActions);
            clone.AfterFlipActions.Items.Should().HaveCount(3);
            clone.IsExpanded.Should().BeFalse();
        }

        [Test]
        public void ShouldTrigger_IncludesBeforeFlipActionDuration() {
            SetupMeridianFlipSettings(minutesAfter: 5, maxMinutesAfter: 10, pauseBefore: 0, useSideOfPier: false, settleTime: 0);
            ProgrammableMeridianFlipTrigger sut = CreateSut();
            Mock<ISequenceItem> nextItemMock = new Mock<ISequenceItem>();

            telescopeInfo.TimeToMeridianFlip = TimeSpan.FromMinutes(8).TotalHours;
            sut.BeforeFlipActions.Add(new RecordingInstruction() { EstimatedDuration = TimeSpan.FromMinutes(7) });
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(1));

            sut.ShouldTrigger(null, nextItemMock.Object).Should().BeTrue();
        }

        [Test]
        public async Task Execute_StopsTrackingBeforeBeforeFlipActionsAndRunsCustomStagesInOrder() {
            SetupMeridianFlipSettings(minutesAfter: 0, maxMinutesAfter: 0, pauseBefore: 0, useSideOfPier: false, settleTime: 0);
            ProgrammableMeridianFlipTrigger sut = CreateSut();
            List<string> order = new List<string>();
            bool? expandedDuringBeforeActions = null;
            sut.IsExpanded = false;

            telescopeMediatorMock.Setup(x => x.SetTrackingEnabled(It.IsAny<bool>()))
                .Callback<bool>(enabled => {
                    telescopeInfo.TrackingEnabled = enabled;
                    order.Add(enabled ? "TrackingOn" : "TrackingOff");
                })
                .Returns(true);
            telescopeMediatorMock.Setup(x => x.MeridianFlip(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
                .Callback(() => order.Add("Flip"))
                .ReturnsAsync(true);

            sut.BeforeFlipActions.Add(new RecordingInstruction() {
                ExecuteAction = () => {
                    expandedDuringBeforeActions = sut.IsExpanded;
                    order.Add(telescopeInfo.TrackingEnabled ? "BeforeTrackingOn" : "BeforeTrackingOff");
                    telescopeInfo.TimeToMeridianFlip = TimeSpan.FromHours(6).TotalHours;
                }
            });
            sut.AfterFlipActions.Add(new RecordingInstruction() {
                ExecuteAction = () => order.Add("After")
            });

            await sut.Execute(new SequentialContainer(), new Progress<ApplicationStatus>(), CancellationToken.None);

            order.Should().ContainInOrder("TrackingOff", "BeforeTrackingOff", "TrackingOn", "Flip", "After");
            expandedDuringBeforeActions.Should().BeTrue();
            sut.IsExpanded.Should().BeFalse();
            telescopeInfo.TrackingEnabled.Should().BeTrue();
            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Never);
            guiderMediatorMock.Verify(x => x.AutoSelectGuideStar(It.IsAny<CancellationToken>()), Times.Never);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Execute_ReportsRemainingWaitTimeThroughActiveStageTitle() {
            SetupMeridianFlipSettings(minutesAfter: 0, maxMinutesAfter: 0, pauseBefore: 0, useSideOfPier: false, settleTime: 0);
            ProgrammableMeridianFlipTrigger sut = CreateSut();
            CancellationTokenSource cts = new CancellationTokenSource();
            string waitForFlipWindowDescription = Loc.Instance["Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_WaitForFlipWindow_Description"];

            telescopeInfo.TimeToMeridianFlip = TimeSpan.FromSeconds(5).TotalHours;

            Task executeTask = sut.Execute(new SequentialContainer(), new Progress<ApplicationStatus>(), cts.Token);

            bool observedCountdown = SpinWait.SpinUntil(
                () => sut.WaitForFlipWindowStage.Status == SequenceEntityStatus.RUNNING
                    && sut.ActiveStageTitle != null
                    && sut.ActiveStageTitle.StartsWith($"{waitForFlipWindowDescription} ", StringComparison.Ordinal),
                TimeSpan.FromSeconds(2));

            observedCountdown.Should().BeTrue();
            sut.ActiveStageTitle.Should().NotBe(waitForFlipWindowDescription);

            await cts.CancelAsync();

            Func<Task> act = async () => await executeTask;
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task Execute_TracksVisibleStageStatusesAndResetsThemAfterExecution() {
            SetupMeridianFlipSettings(minutesAfter: 0, maxMinutesAfter: 0, pauseBefore: 0, useSideOfPier: false, settleTime: 0);
            ProgrammableMeridianFlipTrigger sut = CreateSut();
            TaskCompletionSource beforeStarted = new TaskCompletionSource();
            TaskCompletionSource releaseBefore = new TaskCompletionSource();

            telescopeInfo.TimeToMeridianFlip = 0;
            sut.BeforeFlipActions.Add(new RecordingInstruction() {
                ExecuteAsync = async token => {
                    beforeStarted.TrySetResult();
                    await releaseBefore.Task.WaitAsync(token);
                }
            });

            Task executeTask = sut.Execute(new SequentialContainer(), new Progress<ApplicationStatus>(), CancellationToken.None);

            await beforeStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            sut.StopTrackingStage.Status.Should().Be(SequenceEntityStatus.FINISHED);
            sut.BeforeFlipActions.Status.Should().Be(SequenceEntityStatus.RUNNING);
            sut.WaitForFlipWindowStage.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.ResumeTrackingAndFlipStage.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.SettleStage.Status.Should().Be(SequenceEntityStatus.CREATED);

            releaseBefore.TrySetResult();
            await executeTask;

            sut.StopTrackingStage.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.BeforeFlipActions.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.WaitForFlipWindowStage.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.ResumeTrackingAndFlipStage.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.SettleStage.Status.Should().Be(SequenceEntityStatus.CREATED);
            sut.AfterFlipActions.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public async Task Execute_DoesNotEvaluateParentTriggerSetDuringBeforeFlipActionsExecution() {
            SetupMeridianFlipSettings(minutesAfter: 0, maxMinutesAfter: 0, pauseBefore: 0, useSideOfPier: false, settleTime: 0);
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer context = new SequentialContainer();
            ObservingTrigger siblingTrigger = new ObservingTrigger();
            RecordingInstruction instruction = new RecordingInstruction();
            ProgrammableMeridianFlipTrigger sut = CreateSut();

            root.Add(context);
            context.Add(siblingTrigger);
            context.Add(sut);
            sut.BeforeFlipActions.Add(instruction);

            await sut.Execute(context, new Progress<ApplicationStatus>(), CancellationToken.None);

            instruction.ExecuteCount.Should().Be(1);
            siblingTrigger.ShouldTriggerCount.Should().Be(0);
            siblingTrigger.ShouldTriggerAfterCount.Should().Be(0);
        }

        [Test]
        public void Validate_ReportsCustomActionIssuesAndOnlyBlocksOnDisconnectedTelescope() {
            SetupMeridianFlipSettings(minutesAfter: 0, maxMinutesAfter: 0, pauseBefore: 0, useSideOfPier: false, settleTime: 0);
            ProgrammableMeridianFlipTrigger sut = CreateSut();
            sut.BeforeFlipActions = new IssueReportingSequentialContainer("before issue");
            sut.AfterFlipActions = new IssueReportingSequentialContainer("after issue");

            sut.Validate().Should().BeTrue();
            sut.Issues.Should().Equal("before issue", "after issue");

            telescopeInfo.Connected = false;

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().Contain(Loc.Instance["LblTelescopeNotConnected"]);
            sut.Issues.Should().Contain("before issue");
            sut.Issues.Should().Contain("after issue");
        }

        private ProgrammableMeridianFlipTrigger CreateSut() {
            return new ProgrammableMeridianFlipTrigger(
                profileServiceMock.Object,
                cameraMediatorMock.Object,
                telescopeMediatorMock.Object,
                focuserMediatorMock.Object,
                applicationStatusMediatorMock.Object,
                meridianFlipVMFactoryMock.Object,
                safetyMonitorMediatorMock.Object,
                guiderMediatorMock.Object,
                imagingMediatorMock.Object,
                domeMediatorMock.Object,
                domeFollowerMock.Object,
                filterWheelMediatorMock.Object,
                historyMock.Object,
                autoFocusVMFactoryMock.Object,
                plateSolverFactoryMock.Object,
                windowServiceFactoryMock.Object,
                resourceDictionaryMock.Object);
        }

        private void SetupMeridianFlipSettings(double minutesAfter, double maxMinutesAfter, double pauseBefore, bool useSideOfPier, int settleTime, bool recenter = false, bool autofocusAfterFlip = false, bool rotateImageAfterFlip = false) {
            Mock<IMeridianFlipSettings> settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minutesAfter);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxMinutesAfter);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(pauseBefore);
            settings.SetupGet(m => m.UseSideOfPier).Returns(useSideOfPier);
            settings.SetupGet(m => m.SettleTime).Returns(settleTime);
            settings.SetupGet(m => m.Recenter).Returns(recenter);
            settings.SetupGet(m => m.AutoFocusAfterFlip).Returns(autofocusAfterFlip);
            settings.SetupGet(m => m.RotateImageAfterFlip).Returns(rotateImageAfterFlip);
            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);
        }

        private sealed class RecordingInstruction : NINA.Sequencer.SequenceItem.SequenceItem {
            public Action? ExecuteAction { get; set; }
            public Func<CancellationToken, Task>? ExecuteAsync { get; set; }
            public TimeSpan EstimatedDuration { get; set; }
            public int ExecuteCount { get; private set; }

            public override object Clone() {
                return new RecordingInstruction() {
                    ExecuteAction = ExecuteAction,
                    ExecuteAsync = ExecuteAsync,
                    EstimatedDuration = EstimatedDuration
                };
            }

            public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
                ExecuteCount++;
                if (ExecuteAsync != null) {
                    await ExecuteAsync(token);
                }

                ExecuteAction?.Invoke();
            }

            public override TimeSpan GetEstimatedDuration() {
                return EstimatedDuration;
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

        private sealed class IssueReportingSequentialContainer : SequentialContainer {
            private readonly string issue;

            public IssueReportingSequentialContainer(string issue) {
                this.issue = issue;
            }

            public override bool Validate() {
                Issues.Clear();
                Issues.Add(issue);
                return false;
            }
        }
    }
}
