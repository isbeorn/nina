#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger.MeridianFlip {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_Description")]
    [ExportMetadata("Icon", "MeridianFlipSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ProgrammableMeridianFlipTrigger : MeridianFlipTrigger {
        private enum ProgrammableMeridianFlipStage {
            None,
            StopTracking,
            BeforeFlipActions,
            WaitForFlipWindow,
            ResumeTrackingAndFlip,
            Settle,
            AfterFlipActions
        }

        private readonly IApplicationResourceDictionary resourceDictionary;
        private readonly IGuiderMediator guiderMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeFollower domeFollower;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IImageHistoryVM history;
        private readonly IAutoFocusVMFactory autoFocusVMFactory;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly IWindowServiceFactory windowServiceFactory;
        private bool isExpanded = true;
        private bool shouldSeedDefaults = true;
        private ProgrammableMeridianFlipStage activeStage = ProgrammableMeridianFlipStage.None;
        private string activeStageTitle;

        [ImportingConstructor]
        public ProgrammableMeridianFlipTrigger(
            IProfileService profileService,
            ICameraMediator cameraMediator,
            ITelescopeMediator telescopeMediator,
            IFocuserMediator focuserMediator,
            IApplicationStatusMediator applicationStatusMediator,
            IMeridianFlipVMFactory meridianFlipVMFactory,
            ISafetyMonitorMediator safetyMonitorMediator,
            IGuiderMediator guiderMediator,
            IImagingMediator imagingMediator,
            IDomeMediator domeMediator,
            IDomeFollower domeFollower,
            IFilterWheelMediator filterWheelMediator,
            IImageHistoryVM history,
            IAutoFocusVMFactory autoFocusVMFactory,
            IPlateSolverFactory plateSolverFactory,
            IWindowServiceFactory windowServiceFactory,
            IApplicationResourceDictionary resourceDictionary)
            : base(profileService, cameraMediator, telescopeMediator, focuserMediator, applicationStatusMediator, meridianFlipVMFactory, safetyMonitorMediator) {
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.filterWheelMediator = filterWheelMediator;
            this.history = history;
            this.autoFocusVMFactory = autoFocusVMFactory;
            this.plateSolverFactory = plateSolverFactory;
            this.windowServiceFactory = windowServiceFactory;
            this.resourceDictionary = resourceDictionary;

            BeforeFlipActions = CreateActionContainer("Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_BeforeFlipActions_Description");
            AfterFlipActions = CreateActionContainer("Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_AfterFlipActions_Description");
        }

        private ProgrammableMeridianFlipTrigger(ProgrammableMeridianFlipTrigger cloneMe)
            : this(
                cloneMe.profileService,
                cloneMe.cameraMediator,
                cloneMe.telescopeMediator,
                cloneMe.focuserMediator,
                cloneMe.applicationStatusMediator,
                cloneMe.meridianFlipVMFactory,
                cloneMe.safetyMonitorMediator,
                cloneMe.guiderMediator,
                cloneMe.imagingMediator,
                cloneMe.domeMediator,
                cloneMe.domeFollower,
                cloneMe.filterWheelMediator,
                cloneMe.history,
                cloneMe.autoFocusVMFactory,
                cloneMe.plateSolverFactory,
                cloneMe.windowServiceFactory,
                cloneMe.resourceDictionary) {
            CopyMetaData(cloneMe);
            BeforeFlipActions = (SequentialContainer)cloneMe.BeforeFlipActions.Clone();
            AfterFlipActions = (SequentialContainer)cloneMe.AfterFlipActions.Clone();
            IsExpanded = cloneMe.IsExpanded;
            shouldSeedDefaults = cloneMe.shouldSeedDefaults;
        }

        [JsonProperty]
        public SequentialContainer BeforeFlipActions { get; set; }

        [JsonProperty]
        public SequentialContainer AfterFlipActions { get; set; }

        [JsonProperty]
        public bool IsExpanded {
            get => isExpanded;
            set {
                isExpanded = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public string ActiveStageTitle {
            get => activeStageTitle;
            private set {
                activeStageTitle = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public WorkflowStageState StopTrackingStage { get; } = new WorkflowStageState();

        [JsonIgnore]
        public WorkflowStageState WaitForFlipWindowStage { get; } = new WorkflowStageState();

        [JsonIgnore]
        public WorkflowStageState ResumeTrackingAndFlipStage { get; } = new WorkflowStageState();

        [JsonIgnore]
        public WorkflowStageState SettleStage { get; } = new WorkflowStageState();

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            shouldSeedDefaults = false;
            EnsureActionContainers();
            AttachActionContainersToContext(Parent);
            ResetWorkflowStageStatuses();
            ClearActiveStage();
        }

        public override object Clone() {
            return new ProgrammableMeridianFlipTrigger(this);
        }

        protected override TimeSpan GetReservedExecutionDuration(ISequenceItem nextItem) {
            return base.GetReservedExecutionDuration(nextItem) + EstimateContainerDuration(BeforeFlipActions);
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            EnsureActionContainers();

            Coordinates target = ResolveFlipTarget(context ?? Parent);
            TimeSpan timeToFlip = CalculateScheduledFlipDelay();
            DateTime flipDeadlineUtc = DateTime.UtcNow.Add(timeToFlip);
            bool trackingStopped = false;
            bool completed = false;
            DateTime flipDeadlineLocal = DateTime.Now.Add(timeToFlip);
            ISequenceContainer originalBeforeFlipActionsParent = BeforeFlipActions.Parent;
            ISequenceContainer originalAfterFlipActionsParent = AfterFlipActions.Parent;
            ISequenceContainer runtimeParent = ItemUtility.CreateTriggerRunnerContext(context ?? Parent);
            bool restoreCollapsedAfterExecution = !IsExpanded;

            IsExpanded = true;

            try {
                ResetWorkflowStageStatuses();
                lastFlipTime = DateTime.Now;
                lastFlipCoordiantes = target;
                EarliestFlipTime = flipDeadlineLocal;
                LatestFlipTime = flipDeadlineLocal;

                LogExecutionSummary(target, timeToFlip, flipDeadlineLocal);

                if (!ReferenceEquals(BeforeFlipActions.Parent, runtimeParent)) {
                    BeforeFlipActions.AttachNewParent(runtimeParent);
                }

                if (!ReferenceEquals(AfterFlipActions.Parent, runtimeParent)) {
                    AfterFlipActions.AttachNewParent(runtimeParent);
                }

                SetActiveStage(ProgrammableMeridianFlipStage.StopTracking, Loc.Instance["LblStopTracking"]);
                progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblStopTracking"] });
                Logger.Info("Programmable Meridian Flip - Stopping tracking before running pre-flip actions");
                telescopeMediator.SetTrackingEnabled(false);
                trackingStopped = true;

                await ExecuteActionSet(BeforeFlipActions, ProgrammableMeridianFlipStage.BeforeFlipActions, progress, token);

                SetActiveStage(ProgrammableMeridianFlipStage.WaitForFlipWindow, Loc.Instance["Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_WaitForFlipWindow_Description"]);
                Logger.Info("Programmable Meridian Flip - Keeping tracking stopped while waiting for the flip window");
                telescopeMediator.SetTrackingEnabled(false);
                trackingStopped = true;
                await WaitForFlipDeadline(flipDeadlineUtc, progress, token);

                SetActiveStage(ProgrammableMeridianFlipStage.ResumeTrackingAndFlip, Loc.Instance["Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_ResumeTrackingAndFlip_Description"]);
                progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblResumeTracking"] });
                Logger.Info("Programmable Meridian Flip - Resuming tracking before initiating the flip");
                telescopeMediator.SetTrackingEnabled(true);
                trackingStopped = false;

                await ExecuteMeridianFlipCore(target, progress, token);

                await ExecuteActionSet(AfterFlipActions, ProgrammableMeridianFlipStage.AfterFlipActions, progress, token);

                if (profileService.ActiveProfile.MeridianFlipSettings.RotateImageAfterFlip) {
                    await RotateImageAfterFlip(progress);
                }

                completed = true;
            } finally {
                if (!ReferenceEquals(BeforeFlipActions.Parent, originalBeforeFlipActionsParent)) {
                    BeforeFlipActions.AttachNewParent(originalBeforeFlipActionsParent);
                }

                if (!ReferenceEquals(AfterFlipActions.Parent, originalAfterFlipActionsParent)) {
                    AfterFlipActions.AttachNewParent(originalAfterFlipActionsParent);
                }

                BeforeFlipActions.ResetProgress();
                AfterFlipActions.ResetProgress();
                ResetWorkflowStageStatuses();
                ClearActiveStage();

                if (!completed) {
                    if (trackingStopped) {
                        Logger.Info("Programmable Meridian Flip - Restoring tracking after incomplete programmable meridian flip");
                        telescopeMediator.SetTrackingEnabled(true);
                    }
                }

                if (restoreCollapsedAfterExecution) {
                    IsExpanded = false;
                }

                progress?.Report(new ApplicationStatus());
                Logger.Info($"Programmable Meridian Flip - Exiting programmable meridian flip. Completed: {completed}");
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            EnsureActionContainers();
            SeedDefaultsIfNeeded();
            AttachActionContainersToContext(Parent);
            ResetWorkflowStageStatuses();
            ClearActiveStage();
            Validate();
        }

        public override bool Validate() {
            EnsureActionContainers();

            List<string> i = new List<string>();
            bool valid = true;

            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
                valid = false;
            }

            BeforeFlipActions.Validate();
            AfterFlipActions.Validate();
            i.AddRange(BeforeFlipActions.Issues);
            i.AddRange(AfterFlipActions.Issues);

            Issues = i;
            return valid;
        }

        public override string ToString() {
            return $"Trigger: {nameof(ProgrammableMeridianFlipTrigger)}";
        }

        private void EnsureActionContainers() {
            BeforeFlipActions ??= CreateActionContainer("Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_BeforeFlipActions_Description");
            AfterFlipActions ??= CreateActionContainer("Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_AfterFlipActions_Description");

            if (string.IsNullOrWhiteSpace(BeforeFlipActions.Name)) {
                BeforeFlipActions.Name = Loc.Instance["Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_BeforeFlipActions_Description"];
            }

            if (string.IsNullOrWhiteSpace(AfterFlipActions.Name)) {
                AfterFlipActions.Name = Loc.Instance["Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_AfterFlipActions_Description"];
            }
        }

        /// <summary>
        /// Seeds a freshly dropped trigger from the active meridian-flip profile once, while keeping clones and loaded sequences unchanged.
        /// </summary>
        private void SeedDefaultsIfNeeded() {
            if (!shouldSeedDefaults || Parent == null) {
                return;
            }

            shouldSeedDefaults = false;

            if (profileService.ActiveProfile.MeridianFlipSettings.AutoFocusAfterFlip) {
                AfterFlipActions.Add(CreateDefaultAutofocusItem());
            }

            if (profileService.ActiveProfile.MeridianFlipSettings.Recenter) {
                AfterFlipActions.Add(CreateDefaultCenterItem());
            }
        }

        private SequentialContainer CreateActionContainer(string labelKey) {
            SequentialContainer container = new SequentialContainer() {
                Name = Loc.Instance[labelKey],
                IsExpanded = true
            };

            return container.AddMetaData(resourceDictionary) as SequentialContainer;
        }

        private RunAutofocus CreateDefaultAutofocusItem() {
            return new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory).AddMetaData(resourceDictionary) as RunAutofocus;
        }

        private Center CreateDefaultCenterItem() {
            return new Center(profileService, telescopeMediator, imagingMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory).AddMetaData(resourceDictionary) as Center;
        }

        private void AttachActionContainersToContext(ISequenceContainer parent) {
            ISequenceContainer instructionSetParent = ItemUtility.CreateTriggerRunnerContext(parent);
            BeforeFlipActions.AttachNewParent(instructionSetParent);
            AfterFlipActions.AttachNewParent(instructionSetParent);
        }

        private Coordinates ResolveFlipTarget(ISequenceContainer context) {
            ContextCoordinates contextCoordinates = ItemUtility.RetrieveContextCoordinates(context);
            Coordinates target = contextCoordinates?.Coordinates;

            if (contextCoordinates == null) {
                target = telescopeMediator.GetCurrentPosition();
                Logger.Warning("No target information available for programmable meridian flip. Taking current telescope coordinates instead for the flip.");
            }

            if (target != null && target.RA == 0 && target.Dec == 0) {
                target = telescopeMediator.GetCurrentPosition();
                Logger.Warning("Target coordinates are all zero. Most likely not intended. Taking current telescope coordinates instead for the programmable meridian flip.");
            }

            return target;
        }

        private TimeSpan CalculateScheduledFlipDelay() {
            TimeSpan timeToFlip = CalculateMinimumTimeRemaining();
            if (timeToFlip > TimeSpan.FromHours(2)) {
                Logger.Info("Programmable Meridian Flip - Detected delayed flip state. Clamping wait time to zero.");
                return TimeSpan.Zero;
            }

            return timeToFlip;
        }

        private async Task ExecuteActionSet(SequentialContainer actionSet, ProgrammableMeridianFlipStage stage, IProgress<ApplicationStatus> progress, CancellationToken token) {
            SetActiveStage(stage, actionSet.Name);
            Logger.Info($"Programmable Meridian Flip - Running {actionSet.Name}: {CountEnabledItems(actionSet)} enabled item(s), estimated duration {EstimateContainerDuration(actionSet)}");
            actionSet.ResetAll();
            await actionSet.Run(progress, token);

            if (actionSet.Status == SequenceEntityStatus.FAILED || HasFailedItems(actionSet)) {
                throw new SequenceEntityFailedException($"{actionSet.Name} failed to execute");
            }
        }

        private async Task WaitForFlipDeadline(DateTime flipDeadlineUtc, IProgress<ApplicationStatus> progress, CancellationToken token) {
            string waitForFlipWindowDescription = Loc.Instance["Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_WaitForFlipWindow_Description"];
            TimeSpan remaining = flipDeadlineUtc - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) {
                Logger.Warning($"Programmable Meridian Flip - Before-flip actions exceeded the saved flip deadline by {-remaining}. Flipping immediately.");
                remaining = TimeSpan.Zero;
            }

            UpdateWaitForFlipWindowStatus(waitForFlipWindowDescription, remaining, progress);
            while (remaining.TotalSeconds >= 1) {
                TimeSpan delta = await CoreUtil.Delay(1000, token);
                remaining = remaining - delta;
                UpdateWaitForFlipWindowStatus(waitForFlipWindowDescription, remaining, progress);
            }
        }

        private async Task ExecuteMeridianFlipCore(Coordinates target, IProgress<ApplicationStatus> progress, CancellationToken token) {
            bool flipSuccessful = false;
            await telescopeMediator.RaiseBeforeMeridianFlip(new BeforeMeridianFlipEventArgs(target));

            try {
                progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblFlippingScope"] });
                Logger.Info($"Programmable Meridian Flip - Scope will flip to coordinates RA: {target.RAString} Dec: {target.DecString} Epoch: {target.Epoch}");
                flipSuccessful = await telescopeMediator.MeridianFlip(target, token);
                Logger.Trace($"Programmable Meridian Flip - Successful flip: {flipSuccessful}");

                SetActiveStage(ProgrammableMeridianFlipStage.Settle, Loc.Instance["Lbl_SequenceTrigger_ProgrammableMeridianFlipTrigger_InitialSettleAndSyncDome_Description"]);
                await Settle(progress, token);
                await SynchronizeDome(progress, token);

                if (!flipSuccessful) {
                    throw new SequenceEntityFailedException(Loc.Instance["LblMeridianFlipFailed"]);
                }
            } finally {
                await telescopeMediator.RaiseAfterMeridianFlip(new AfterMeridianFlipEventArgs(flipSuccessful, target));
            }
        }

        private async Task SynchronizeDome(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var domeInfo = domeMediator.GetInfo();
            if (!domeInfo.Connected || !domeInfo.CanSetAzimuth) {
                return;
            }

            progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblSynchronizingDome"] });
            try {
                if (domeFollower.IsFollowing) {
                    Logger.Info("Programmable Meridian Flip - Waiting for dome to synchronize to scope");
                    await domeFollower.WaitForDomeSynchronization(token);
                } else {
                    Logger.Info("Programmable Meridian Flip - Synchronizing dome to scope since dome following is not enabled");
                    if (!await domeFollower.TriggerTelescopeSync()) {
                        Notification.ShowWarning(Loc.Instance["LblDomeSyncFailureDuringMeridianFlip"]);
                        Logger.Warning("Programmable Meridian Flip - Synchronize dome operation didn't complete successfully. Moving on");
                    }
                }
            } catch (Exception ex) {
                Notification.ShowWarning(Loc.Instance["LblDomeSyncFailureDuringMeridianFlip"]);
                Logger.Error("Programmable Meridian Flip - Synchronize dome operation didn't complete successfully. Moving on", ex);
            }
        }

        private async Task Settle(IProgress<ApplicationStatus> progress, CancellationToken token) {
            TimeSpan remaining = TimeSpan.FromSeconds(profileService.ActiveProfile.MeridianFlipSettings.SettleTime);
            Logger.Info($"Programmable Meridian Flip - Settling scope for {profileService.ActiveProfile.MeridianFlipSettings.SettleTime} seconds");
            while (remaining.TotalSeconds >= 1) {
                progress?.Report(new ApplicationStatus() { Status = $"{Loc.Instance["LblSettle"]} {remaining:hh\\:mm\\:ss}" });
                TimeSpan delta = await CoreUtil.Delay(1000, token);
                remaining = TimeSpan.FromSeconds(remaining.TotalSeconds - delta.TotalSeconds);
            }
        }

        private Task RotateImageAfterFlip(IProgress<ApplicationStatus> progress) {
            progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblRotateImageAfterFlip"] });
            Logger.Info("Programmable Meridian Flip - Rotating image after flip by 180 degrees");
            imagingMediator.SetImageRotation(imagingMediator.GetImageRotation() + 180);
            return Task.CompletedTask;
        }

        private void LogExecutionSummary(Coordinates target, TimeSpan timeToFlip, DateTime flipDeadlineLocal) {
            var settings = profileService.ActiveProfile.MeridianFlipSettings;
            var telescopeInfo = telescopeMediator.GetInfo();

            Logger.Info("Programmable Meridian Flip - Initializing programmable meridian flip. " +
                $"Target: {FormatCoordinates(target)}; " +
                $"Remaining wait time: {timeToFlip}; " +
                $"Flip deadline: {flipDeadlineLocal:yyyy-MM-dd HH:mm:ss}; " +
                $"Settings: PauseBeforeMeridian={settings.PauseTimeBeforeMeridian} min, MinutesAfterMeridian={settings.MinutesAfterMeridian} min, MaxMinutesAfterMeridian={settings.MaxMinutesAfterMeridian} min, UseSideOfPier={settings.UseSideOfPier}, SettleTime={settings.SettleTime} sec, RotateImageAfterFlip={settings.RotateImageAfterFlip}; " +
                $"Profile seeding defaults: AutoFocusAfterFlip={settings.AutoFocusAfterFlip}, Recenter={settings.Recenter}; " +
                $"Action plan: BeforeActions={CountEnabledItems(BeforeFlipActions)} enabled item(s) / {EstimateContainerDuration(BeforeFlipActions)}, AfterActions={CountEnabledItems(AfterFlipActions)} enabled item(s) / {EstimateContainerDuration(AfterFlipActions)}; " +
                $"Telescope state: Tracking={telescopeInfo.TrackingEnabled}, SideOfPier={telescopeInfo.SideOfPier}, TimeToMeridianFlip={telescopeInfo.TimeToMeridianFlip} h, AtPark={telescopeInfo.AtPark}, AtHome={telescopeInfo.AtHome}");
        }

        private static string FormatCoordinates(Coordinates coordinates) {
            if (coordinates == null) {
                return "unknown";
            }

            return $"RA: {coordinates.RAString} Dec: {coordinates.DecString} Epoch: {coordinates.Epoch}";
        }

        private void SetActiveStage(ProgrammableMeridianFlipStage stage, string stageTitle) {
            CompleteStage(activeStage);
            activeStage = stage;
            ActiveStageTitle = stageTitle;
            SetStageStatus(stage, SequenceEntityStatus.RUNNING);
        }

        private void UpdateWaitForFlipWindowStatus(string stageTitle, TimeSpan remaining, IProgress<ApplicationStatus> progress) {
            string status = $"{stageTitle} {remaining:hh\\:mm\\:ss}";
            ActiveStageTitle = status;
            progress?.Report(new ApplicationStatus() { Status = status });
        }

        private void ResetWorkflowStageStatuses() {
            StopTrackingStage.Status = SequenceEntityStatus.CREATED;
            WaitForFlipWindowStage.Status = SequenceEntityStatus.CREATED;
            ResumeTrackingAndFlipStage.Status = SequenceEntityStatus.CREATED;
            SettleStage.Status = SequenceEntityStatus.CREATED;
        }

        private void CompleteStage(ProgrammableMeridianFlipStage stage) {
            SetStageStatus(stage, SequenceEntityStatus.FINISHED);
        }

        private void SetStageStatus(ProgrammableMeridianFlipStage stage, SequenceEntityStatus status) {
            WorkflowStageState stageState = GetStageState(stage);
            if (stageState != null) {
                stageState.Status = status;
            }
        }

        private WorkflowStageState GetStageState(ProgrammableMeridianFlipStage stage) {
            return stage switch {
                ProgrammableMeridianFlipStage.StopTracking => StopTrackingStage,
                ProgrammableMeridianFlipStage.WaitForFlipWindow => WaitForFlipWindowStage,
                ProgrammableMeridianFlipStage.ResumeTrackingAndFlip => ResumeTrackingAndFlipStage,
                ProgrammableMeridianFlipStage.Settle => SettleStage,
                _ => null
            };
        }

        private void ClearActiveStage() {
            activeStage = ProgrammableMeridianFlipStage.None;
            ActiveStageTitle = null;
        }

        private static bool HasFailedItems(ISequenceContainer container) {
            foreach (ISequenceItem item in container.GetItemsSnapshot()) {
                if (item.Status == SequenceEntityStatus.FAILED) {
                    return true;
                }

                if (item is ISequenceContainer childContainer && HasFailedItems(childContainer)) {
                    return true;
                }
            }

            return false;
        }

        private static int CountEnabledItems(ISequenceContainer container) {
            if (container == null) {
                return 0;
            }

            int total = 0;
            foreach (ISequenceItem item in container.GetItemsSnapshot()) {
                if (item.Status == SequenceEntityStatus.DISABLED) {
                    continue;
                }

                if (item is ISequenceContainer childContainer) {
                    total += CountEnabledItems(childContainer);
                } else {
                    total++;
                }
            }

            return total;
        }

        private static TimeSpan EstimateContainerDuration(ISequenceContainer container) {
            if (container == null) {
                return TimeSpan.Zero;
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (ISequenceItem item in container.GetItemsSnapshot()) {
                if (item.Status == SequenceEntityStatus.DISABLED) {
                    continue;
                }

                if (item is ISequenceContainer childContainer) {
                    total += EstimateContainerDuration(childContainer);
                } else {
                    total += item.GetEstimatedDuration();
                }
            }

            return total;
        }

        /// <summary>
        /// Lightweight status holder for fixed trigger workflow stages so sequencer progress templates can be reused in the trigger UI.
        /// </summary>
        public sealed class WorkflowStageState : BaseINPC {
            private SequenceEntityStatus status = SequenceEntityStatus.CREATED;

            public SequenceEntityStatus Status {
                get => status;
                set {
                    status = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
