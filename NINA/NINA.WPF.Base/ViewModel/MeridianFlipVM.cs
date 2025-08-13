#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.WPF.Base.ViewModel {

    public class MeridianFlipVM : BaseVM, IMeridianFlipVM {

        public MeridianFlipVM(
                IProfileService profileService,
                ITelescopeMediator telescopeMediator,
                IGuiderMediator guiderMediator,
                IImagingMediator imagingMediator,
                IDomeMediator domeMediator,
                IDomeFollower domeFollower,
                IApplicationStatusMediator applicationStatusMediator,
                IFilterWheelMediator filterWheelMediator,
                IImageHistoryVM history,
                IAutoFocusVMFactory autoFocusVMFactory) : base(profileService) {
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.applicationStatusMediator = applicationStatusMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.history = history;
            this.autoFocusVMFactory = autoFocusVMFactory;
            CancelCommand = new RelayCommand(Cancel);
        }

        private IProgress<ApplicationStatus> _progress;

        private TimeSpan _remainingTime;

        private ApplicationStatus _status;

        private AutomatedWorkflow _steps;

        private Coordinates _targetCoordinates;

        private CancellationTokenSource internalCancellationToken;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeFollower domeFollower;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IImageHistoryVM history;
        private readonly IAutoFocusVMFactory autoFocusVMFactory;

        public ICommand CancelCommand { get; set; }

        public TimeSpan RemainingTime {
            get => _remainingTime;
            set {
                _remainingTime = value;
                RaisePropertyChanged();
            }
        }

        public ApplicationStatus Status {
            get => _status;
            set {
                _status = value;
                if(value != null) {
                    value.Source = "MeridianFlip";
                    RaisePropertyChanged();

                    this.applicationStatusMediator.StatusUpdate(value);
                }                
            }
        }

        public AutomatedWorkflow Steps {
            get => _steps;
            set {
                _steps = value;
                RaisePropertyChanged();
            }
        }

        private void Cancel(object obj) {
            try { internalCancellationToken?.Cancel(); } catch { }
        }

        private async Task<bool> DoFlip(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblFlippingScope"] });
            Logger.Info($"Meridian Flip - Scope will flip to coordinates RA: {_targetCoordinates.RAString} Dec: {_targetCoordinates.DecString} Epoch: {_targetCoordinates.Epoch}");
            var flipsuccess = await telescopeMediator.MeridianFlip(_targetCoordinates, token);
            Logger.Trace($"Meridian Flip - Successful flip: {flipsuccess}");

            await Settle(token, progress);

            var domeInfo = domeMediator.GetInfo();
            if (domeInfo.Connected && domeInfo.CanSetAzimuth) {
                progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSynchronizingDome"] });
                try {
                    if (domeFollower.IsFollowing) {
                        Logger.Info($"Meridian Flip - Waiting for dome to synchronize to scope");

                        await domeFollower.WaitForDomeSynchronization(token);
                    } else {
                        Logger.Info($"Meridian Flip - Synchronize dome to scope since dome following is not enabled");
                        if (!await domeFollower.TriggerTelescopeSync()) {
                            Notification.ShowWarning(Loc.Instance["LblDomeSyncFailureDuringMeridianFlip"]);
                            Logger.Warning("Meridian Flip - Synchronize dome operation didn't complete successfully. Moving on");
                        }
                    }
                } catch (Exception e) {
                    Notification.ShowWarning(Loc.Instance["LblDomeSyncFailureDuringMeridianFlip"]);
                    Logger.Error("Meridian Flip - Synchronize dome operation didn't complete successfully. Moving on", e);
                }
            }

            return flipsuccess;
        }

        private async Task<bool> DoMeridianFlip(Coordinates targetCoordinates, TimeSpan timeToFlip, CancellationToken requestCancellationToken) {
            var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken, this.internalCancellationToken.Token).Token;
            try {
                _targetCoordinates = targetCoordinates;
                RemainingTime = timeToFlip;

                Logger.Info($"Meridian Flip - Initializing Meridian Flip. " + Environment.NewLine +
                    $"Current target coordinates RA: {_targetCoordinates.RAString} Dec: {_targetCoordinates.DecString} Epoch: {_targetCoordinates.Epoch}" + Environment.NewLine +
                    $"Remaining wait time: {timeToFlip}" + Environment.NewLine +
                    $"Pause Time Before Meridian: {profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian}" + Environment.NewLine +
                    $"Minutes After Meridian: {profileService.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian}" + Environment.NewLine +
                    $"Max Minutes After Meridian: {profileService.ActiveProfile.MeridianFlipSettings.MaxMinutesAfterMeridian}" + Environment.NewLine +
                    $"AutoFocus After Flip: {profileService.ActiveProfile.MeridianFlipSettings.AutoFocusAfterFlip}" + Environment.NewLine +
                    $"Recenter: {profileService.ActiveProfile.MeridianFlipSettings.Recenter}" + Environment.NewLine +
                    $"Use SideOfPier: {profileService.ActiveProfile.MeridianFlipSettings.UseSideOfPier}" + Environment.NewLine +
                    $"Settle Time: {profileService.ActiveProfile.MeridianFlipSettings.SettleTime}");

                Steps = new AutomatedWorkflow();

                var guiderInfo = guiderMediator.GetInfo();
                var guiderRelevant = guiderInfo.Connected && !(guiderMediator.GetDevice() is DirectGuider);
                if (guiderRelevant) { 
                    Steps.Add(new WorkflowStep("StopAutoguider", Loc.Instance["LblStopAutoguider"], () => StopAutoguider(cancellationToken, _progress)));
                }

                Steps.Add(new WorkflowStep("PassMeridian", Loc.Instance["LblPassMeridian"], () => PassMeridian(cancellationToken, _progress)));
                Steps.Add(new WorkflowStep("Flip", Loc.Instance["LblFlip"], () => DoFlip(cancellationToken, _progress)));
                if (profileService.ActiveProfile.MeridianFlipSettings.AutoFocusAfterFlip) {
                    Steps.Add(new WorkflowStep("Autofocus", Loc.Instance["LblAutoFocus"], () => AutoFocus(cancellationToken, _progress)));
                }
                if (profileService.ActiveProfile.MeridianFlipSettings.Recenter) {
                    Steps.Add(new WorkflowStep("Recenter", Loc.Instance["LblRecenter"], () => Recenter(cancellationToken, _progress)));
                }

                if(guiderRelevant) {
                    Steps.Add(new WorkflowStep("SelectNewGuideStar", Loc.Instance["LblSelectNewGuideStar"], () => SelectNewGuideStar(cancellationToken, _progress)));
                    Steps.Add(new WorkflowStep("ResumeAutoguider", Loc.Instance["LblResumeAutoguider"], () => ResumeAutoguider(cancellationToken, _progress)));
                }

                Steps.Add(new WorkflowStep("Settle", Loc.Instance["LblSettle"], () => Settle(cancellationToken, _progress)));

                if (profileService.ActiveProfile.MeridianFlipSettings.RotateImageAfterFlip) {
                    Steps.Add(new WorkflowStep("RotateImageAfterFlip", Loc.Instance["LblRotateImageAfterFlip"], () => RotateImageAfterFlip()));
                }

                await Steps.Process();

            } catch (OperationCanceledException) {
                Logger.Info("Meridian Flip - Cancelled by user");
            } catch (Exception ex) {
                Logger.Error("Meridian Flip failed", ex);
                Notification.ShowError(Loc.Instance["LblMeridianFlipFailed"] + Environment.NewLine + ex.Message);

                try {
                    Logger.Info("Meridian Flip - Resuming Autoguider after meridian flip error");
                    await ResumeAutoguider(cancellationToken, _progress);
                } catch (Exception ex2) {
                    Logger.Error(ex2);
                    Notification.ShowError(Loc.Instance["GuiderResumeFailed"]);
                }

                Logger.Info("Meridian Flip - Re-enable Tracking after meridian flip error");
                telescopeMediator.SetTrackingEnabled(true);
                return false;
            } finally {
                _progress.Report(new ApplicationStatus() { Status = "" });
            }
            Logger.Info("Meridian Flip - Exiting meridian flip");
            return true;
        }

        private async Task<bool> RotateImageAfterFlip() {
            imagingMediator.SetImageRotation(imagingMediator.GetImageRotation() + 180);
            return true;
        }

        private async Task<bool> AutoFocus(CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Info($"Meridian Flip - Running Autofocus");
            var autoFocus = this.autoFocusVMFactory.Create();
            progress.Report(new ApplicationStatus { Status = Loc.Instance["LblAutoFocus"] });
            var service = WindowServiceFactory.Create();
            service.Show(autoFocus, Loc.Instance["LblAutoFocus"], System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
            try {
                FilterInfo filter = null;
                var selectedFilter = filterWheelMediator.GetInfo()?.SelectedFilter;
                if (selectedFilter != null) {
                    filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(x => x.Position == selectedFilter.Position).FirstOrDefault();
                }

                var report = await autoFocus.StartAutoFocus(filter, token, progress);
                history.AppendAutoFocusPoint(report);
            } finally {
                service.DelayedClose(TimeSpan.FromSeconds(10));
            }

            return true;
        }

        private async Task<bool> PassMeridian(CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Meridian Flip - Passing meridian");

            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblStopTracking"] });

            Logger.Info("Meridian Flip - Stopping tracking to pass meridian");
            telescopeMediator.SetTrackingEnabled(false);
            do {
                progress.Report(new ApplicationStatus() { Status = RemainingTime.ToString(@"hh\:mm\:ss") });

                //progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", RemainingTime));
                var delta = await CoreUtil.Delay(1000, token);

                RemainingTime -= delta;
            } while (RemainingTime.TotalSeconds >= 1);
            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblResumeTracking"] });

            Logger.Info("Meridian Flip - Resuming tracking after passing meridian");
            telescopeMediator.SetTrackingEnabled(true);

            Logger.Trace("Meridian Flip - Meridian passed");
            return true;
        }

        private async Task<bool> Recenter(CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (profileService.ActiveProfile.MeridianFlipSettings.Recenter) {
                Logger.Info("Meridian Flip - Recenter after meridian flip");

                progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblInitiatePlatesolve"] });

                var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
                var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);
                var seq = new CaptureSequence(
                    profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                    CaptureSequence.ImageTypes.SNAPSHOT,
                    profileService.ActiveProfile.PlateSolveSettings.Filter,
                    new BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                    1
                );
                seq.Gain = profileService.ActiveProfile.PlateSolveSettings.Gain;

                var solver = new CenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator, filterWheelMediator, domeMediator, domeFollower);
                var parameter = new CenterSolveParameter() {
                    Attempts = profileService.ActiveProfile.PlateSolveSettings.NumberOfAttempts,
                    Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                    Coordinates = _targetCoordinates,
                    DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                    FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                    MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                    PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                    ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                    Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                    SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                    Threshold = profileService.ActiveProfile.PlateSolveSettings.Threshold,
                    NoSync = profileService.ActiveProfile.TelescopeSettings.NoSync,
                    BlindFailoverEnabled = profileService.ActiveProfile.PlateSolveSettings.BlindFailoverEnabled
                };
                var result = await solver.Center(seq, parameter, default, progress, token);
                if (!result.Success) {
                    Logger.Error("Center after meridian flip failed. Continuing without it");
                    Notification.ShowError(Loc.Instance["LblMeridianFlipCenterFailed"]);
                    // Recenter is best effort, so always return true
                }
            }
            return true;
        }

        private async Task<bool> ResumeAutoguider(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblResumeGuiding"] });
            Logger.Info("Meridian Flip - Resuming Autoguider");
            var result = await this.guiderMediator.StartGuiding(false, progress, token);

            return result;
        }

        private async Task<bool> SelectNewGuideStar(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSelectGuidestar"] });
            Logger.Info("Meridian Flip - Selecting new guide star");
            return await this.guiderMediator.AutoSelectGuideStar(token);
        }

        private async Task<bool> Settle(CancellationToken token, IProgress<ApplicationStatus> progress) {
            RemainingTime = TimeSpan.FromSeconds(profileService.ActiveProfile.MeridianFlipSettings.SettleTime);
            Logger.Info($"Meridian Flip - Settling scope for {profileService.ActiveProfile.MeridianFlipSettings.SettleTime}");
            do {
                progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSettle"] + " " + RemainingTime.ToString(@"hh\:mm\:ss") });

                var delta = await CoreUtil.Delay(1000, token);

                RemainingTime = TimeSpan.FromSeconds(RemainingTime.TotalSeconds - delta.TotalSeconds);
            } while (RemainingTime.TotalSeconds >= 1);
            return true;
        }

        private async Task<bool> StopAutoguider(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblStopGuiding"] });
            Logger.Info("Meridian Flip - Stopping Autoguider");
            var result = await this.guiderMediator.StopGuiding(token);
            return result;
        }

        private IWindowServiceFactory windowServiceFactory;

        public IWindowServiceFactory WindowServiceFactory {
            get {
                if (windowServiceFactory == null) {
                    windowServiceFactory = new WindowServiceFactory();
                }
                return windowServiceFactory;
            }
            set => windowServiceFactory = value;
        }

        /// <summary>
        /// Checks if auto meridian flip should be considered and executes it
        /// 1) Compare next exposure length with time to meridian - If exposure length is greater
        ///    than time to flip the system will wait
        /// 2) Stop Guider
        /// 3) Execute the flip
        /// 4) If recentering is enabled, platesolve current position, sync and recenter to old
        ///    target position
        /// 5) Resume Guider
        /// </summary>
        /// <param name="targetCoordinates">Reference Coordinates to slew to after flip</param>
        /// <param name="timeToFlip">Remaining time to actually flip the scope</param>
        /// <returns></returns>
        public async Task<bool> MeridianFlip(Coordinates targetCoordinates, TimeSpan timeToFlip, CancellationToken cancellationToken = default) {
            var service = WindowServiceFactory.Create();
            this.internalCancellationToken?.Dispose();
            this.internalCancellationToken = new CancellationTokenSource();
            this._progress = new Progress<ApplicationStatus>(p => Status = p);

            await telescopeMediator.RaiseBeforeMeridianFlip(new BeforeMeridianFlipEventArgs(targetCoordinates));

            var flip = DoMeridianFlip(targetCoordinates, timeToFlip, cancellationToken);

            var serviceTask = service.ShowDialog(this, Loc.Instance["LblMeridianFlipInit"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.None, CancelCommand);
            var flipResult = await flip;

            await telescopeMediator.RaiseAfterMeridianFlip(new AfterMeridianFlipEventArgs(flipResult, targetCoordinates));

            await service.Close();
            await serviceTask;
            return flipResult;
        }
    }

    public class AutomatedWorkflow : BaseINPC, ICollection<WorkflowStep> {
        private WorkflowStep _activeStep;
        private AsyncObservableLimitedSizedStack<WorkflowStep> _internalStack;

        public AutomatedWorkflow() {
            _internalStack = new AsyncObservableLimitedSizedStack<WorkflowStep>(int.MaxValue);
        }

        public WorkflowStep ActiveStep {
            get => _activeStep;
            set {
                _activeStep = value;
                RaisePropertyChanged();
            }
        }

        public int Count => _internalStack.Count;

        public bool IsReadOnly => _internalStack.IsReadOnly;

        public void Add(WorkflowStep item) {
            _internalStack.Add(item);
        }

        public void Clear() {
            _internalStack.Clear();
        }

        public bool Contains(WorkflowStep item) {
            return _internalStack.Contains(item);
        }

        public void CopyTo(WorkflowStep[] array, int arrayIndex) {
            _internalStack.CopyTo(array, arrayIndex);
        }

        public IEnumerator<WorkflowStep> GetEnumerator() {
            return _internalStack.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _internalStack.GetEnumerator();
        }

        public async Task<bool> Process() {
            var success = true;

            var item = _internalStack.First();
            do {
                ActiveStep = item.Value;
                success = await ActiveStep.Process() && success;
                item = item.Next;
            }
            while (item != null);

            return success;
        }

        public bool Remove(WorkflowStep item) {
            return _internalStack.Remove(item);
        }
    }

    public class WorkflowStep : BaseINPC {
        private Func<Task<bool>> _action;

        private bool _finished;

        private string _id;

        private string _title;

        public WorkflowStep(string id, string title, Func<Task<bool>> action) {
            Id = id;
            Title = title;
            Action = action;
        }

        public Func<Task<bool>> Action {
            get => _action;
            set {
                _action = value;
                RaisePropertyChanged();
            }
        }

        public bool Finished {
            get => _finished;
            set {
                _finished = value;
                RaisePropertyChanged();
            }
        }

        public string Id {
            get => _id;
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        public string Title {
            get => _title;
            set {
                _title = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> Process() {
            var success = await Action();
            Finished = success;
            return success;
        }
    }
}