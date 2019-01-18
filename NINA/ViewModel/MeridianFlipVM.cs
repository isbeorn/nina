#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class MeridianFlipVM : BaseVM {

        public MeridianFlipVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator, IImagingMediator imagingMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            CancelCommand = new RelayCommand(Cancel);
        }

        private ICommand _cancelCommand;

        private IProgress<ApplicationStatus> _progress;

        private TimeSpan _remainingTime;

        private ApplicationStatus _status;

        private AutomatedWorkflow _steps;

        private Coordinates _targetCoordinates;

        private CancellationTokenSource _tokensource;
        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public ICommand CancelCommand {
            get {
                return _cancelCommand;
            }
            set {
                _cancelCommand = value;
            }
        }

        public TimeSpan RemainingTime {
            get {
                return _remainingTime;
            }
            set {
                _remainingTime = value;
                RaisePropertyChanged();
            }
        }

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = "MeridianFlip";
                RaisePropertyChanged();

                this.applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public AutomatedWorkflow Steps {
            get {
                return _steps;
            }
            set {
                _steps = value;
                RaisePropertyChanged();
            }
        }

        private void Cancel(object obj) {
            _tokensource?.Cancel();
        }

        private async Task<bool> DoFilp(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblFlippingScope"] });
            var flipsuccess = telescopeMediator.MeridianFlip(_targetCoordinates);

            await Settle(token, progress);

            return flipsuccess;
        }

        private async Task<bool> DoMeridianFlip(TelescopeInfo telescopeInfo) {
            try {
                var token = _tokensource.Token;
                Steps = new AutomatedWorkflow();

                Steps.Add(new WorkflowStep("StopAutoguider", Locale.Loc.Instance["LblStopAutoguider"], () => StopAutoguider(token, _progress)));

                Steps.Add(new WorkflowStep("PassMeridian", Locale.Loc.Instance["LblPassMeridian"], () => PassMeridian(telescopeInfo, token, _progress)));
                Steps.Add(new WorkflowStep("Flip", Locale.Loc.Instance["LblFlip"], () => DoFilp(token, _progress)));
                if (profileService.ActiveProfile.MeridianFlipSettings.Recenter) {
                    Steps.Add(new WorkflowStep("Recenter", Locale.Loc.Instance["LblRecenter"], () => Recenter(token, _progress)));
                }

                Steps.Add(new WorkflowStep("SelectNewGuideStar", Locale.Loc.Instance["LblSelectNewGuideStar"], () => SelectNewGuideStar(token, _progress)));
                Steps.Add(new WorkflowStep("ResumeAutoguider", Locale.Loc.Instance["LblResumeAutoguider"], () => ResumeAutoguider(token, _progress)));

                Steps.Add(new WorkflowStep("Settle", Locale.Loc.Instance["LblSettle"], () => Settle(token, _progress)));

                await Steps.Process();
            } catch (Exception ex) {
                Logger.Error(ex);

                try {
                    await ResumeAutoguider(new CancellationToken(), _progress);
                } catch (Exception ex2) {
                    Logger.Error(ex2);
                    Notification.ShowError(Locale.Loc.Instance["GuiderResumeFailed"]);
                }

                telescopeMediator.SetTracking(true);
                return false;
            } finally {
                _progress.Report(new ApplicationStatus() { Status = "" });
            }
            return true;
        }

        private async Task<bool> PassMeridian(TelescopeInfo telescopeInfo, CancellationToken token, IProgress<ApplicationStatus> progress) {
            var timeToFlip = telescopeInfo.TimeToMeridianFlip * 60 * 60;
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStopTracking"] });
            _targetCoordinates = telescopeInfo.Coordinates;
            telescopeMediator.SetTracking(false);
            do {
                RemainingTime = TimeSpan.FromSeconds(timeToFlip);
                progress.Report(new ApplicationStatus() { Status = RemainingTime.ToString(@"hh\:mm\:ss") });

                //progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", RemainingTime));
                var delta = await Utility.Utility.Delay(1000, token);

                timeToFlip -= delta.TotalSeconds;
            } while (RemainingTime.TotalSeconds >= 1);
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblResumeTracking"] });
            telescopeMediator.SetTracking(true);
            return true;
        }

        private async Task<bool> Recenter(CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (profileService.ActiveProfile.MeridianFlipSettings.Recenter) {
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblInitiatePlatesolve"] });

                var solver = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
                var seq = new CaptureSequence(
                    profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                    CaptureSequence.ImageTypes.SNAP,
                    profileService.ActiveProfile.PlateSolveSettings.Filter,
                    new Model.MyCamera.BinningMode(1, 1),
                    1);

                await solver.CaptureSolveSyncAndReslew(seq, true, true, true, token, progress, true, profileService.ActiveProfile.PlateSolveSettings.Threshold);
            }
            return true;
        }

        private async Task<bool> ResumeAutoguider(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblResumeGuiding"] });
            var result = await this.guiderMediator.StartGuiding(token);

            return result;
        }

        private async Task<bool> SelectNewGuideStar(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSelectGuidestar"] });
            return await this.guiderMediator.AutoSelectGuideStar(token);
        }

        private async Task<bool> Settle(CancellationToken token, IProgress<ApplicationStatus> progress) {
            RemainingTime = TimeSpan.FromSeconds(profileService.ActiveProfile.MeridianFlipSettings.SettleTime);
            do {
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSettle"] + " " + RemainingTime.ToString(@"hh\:mm\:ss") });

                var delta = await Utility.Utility.Delay(1000, token);

                RemainingTime = TimeSpan.FromSeconds(RemainingTime.TotalSeconds - delta.TotalSeconds);
            } while (RemainingTime.TotalSeconds >= 1);
            return true;
        }

        public static bool ShouldFlip(IProfileService profileService, double exposureTime, TelescopeInfo telescopeInfo) {
            if (profileService.ActiveProfile.MeridianFlipSettings.Enabled) {
                if (telescopeInfo.Connected == true) {
                    if ((telescopeInfo.TimeToMeridianFlip - (profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian / 60)) < (exposureTime / 60 / 60)) {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> StopAutoguider(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStopGuiding"] });
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
            set {
                windowServiceFactory = value;
            }
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
        /// <param name="seq">Current Sequence row</param>
        /// <returns></returns>
        public async Task<bool> MeridianFlip(CaptureSequence seq, TelescopeInfo telescopeInfo) {
            var service = WindowServiceFactory.Create();
            this._tokensource = new CancellationTokenSource();
            this._progress = new Progress<ApplicationStatus>(p => Status = p);
            var flip = DoMeridianFlip(telescopeInfo);

            var serviceTask = service.ShowDialog(this, Locale.Loc.Instance["LblAutoMeridianFlip"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.None, CancelCommand);
            var flipResult = await flip;

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
            get {
                return _activeStep;
            }
            set {
                _activeStep = value;
                RaisePropertyChanged();
            }
        }

        public int Count {
            get {
                return _internalStack.Count;
            }
        }

        public bool IsReadOnly {
            get {
                return _internalStack.IsReadOnly;
            }
        }

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
            get {
                return _action;
            }
            set {
                _action = value;
                RaisePropertyChanged();
            }
        }

        public bool Finished {
            get {
                return _finished;
            }
            set {
                _finished = value;
                RaisePropertyChanged();
            }
        }

        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        public string Title {
            get {
                return _title;
            }
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