using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    class MeridianFlipVM : BaseVM {
        public MeridianFlipVM() {
            CancelCommand = new RelayCommand(Cancel);
            RegisterMediatorMessages();
        }

        private void Cancel(object obj) {
            _tokensource?.Cancel();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                Telescope = (ITelescope)o;
            }, MediatorMessages.TelescopeChanged);

            Mediator.Instance.RegisterAsync(async (object o) => {
                object[] args = o as object[];
                CaptureSequence seq = (CaptureSequence)args[0];
                CancellationTokenSource source = (CancellationTokenSource)args[1];
                await CheckMeridianFlip(seq, source);
            }, AsyncMediatorMessages.CheckMeridianFlip);
        }

        private IProgress<string> _progress;
        private CancellationTokenSource _tokensource;

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();
            }
        }

        private AutomatedWorkflow _steps;
        public AutomatedWorkflow Steps {
            get {
                return _steps;
            }
            set {
                _steps = value;
                RaisePropertyChanged();
            }
        }

        private ITelescope _telescope;
        public ITelescope Telescope {
            get {
                return _telescope;
            }
            private set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private bool ShouldFlip(double exposureTime) {
            if (Settings.AutoMeridianFlip) {
                if (Telescope?.Connected == true) {

                    if ((Telescope.TimeToMeridianFlip - (Settings.PauseTimeBeforeMeridian / 60)) < (exposureTime / 60 / 60)) {
                        return true;
                    }
                }
            }
            return false;
        }

        private TimeSpan _remainingTime;
        public TimeSpan RemainingTime {
            get {
                return _remainingTime;
            }
            set {
                _remainingTime = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Checks if auto meridian flip should be considered and executes it
        /// 1) Compare next exposure length with time to meridian - If exposure length is greater than time to flip the system will wait
        /// 2) Pause Guider
        /// 3) Execute the flip
        /// 4) If recentering is enabled, platesolve current position, sync and recenter to old target position
        /// 5) Resume Guider
        /// </summary>
        /// <param name="seq">Current Sequence row</param>
        /// <param name="tokenSource">cancel token</param>
        /// <param name="progress">progress reporter</param>
        /// <returns></returns>
        public async Task CheckMeridianFlip(CaptureSequence seq, CancellationTokenSource tokensource) {
            if (ShouldFlip(seq.ExposureTime)) {
                var service = new WindowService();
                this._tokensource = tokensource;
                this._progress = new Progress<string>(p => Status = p);
                var flip = DoMeridianFlip();

                service.ShowDialog(this, "Meridian Flip");
                await flip;

                await service.Close();
            }
        }

        private Coordinates _targetCoordinates;

        private async Task<bool> PassMeridian(CancellationTokenSource tokenSource, IProgress<string> progress) {
            var timeToFlip = Telescope.TimeToMeridianFlip * 60 * 60;
            progress.Report("Stop Scope tracking");
            _targetCoordinates = Telescope.Coordinates;
            Mediator.Instance.Notify(MediatorMessages.SetTelescopeTracking, false);            
            do {
                RemainingTime = TimeSpan.FromSeconds(timeToFlip);
                //progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", RemainingTime));
                await Task.Delay(1000, tokenSource.Token);
                timeToFlip -= 1;

            } while (RemainingTime.TotalSeconds >= 1);
            progress.Report("Resume Scope tracking");
            Mediator.Instance.Notify(MediatorMessages.SetTelescopeTracking, true); 
            return true;
        }

        private async Task<bool> DoFilp(CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report("Flipping Scope");
            var flipsuccess = Telescope.MeridianFlip(_targetCoordinates);

            await Settle(tokenSource, progress);

            return flipsuccess;
        }


        private async Task<bool> Recenter(CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (Settings.RecenterAfterFlip) {
                progress.Report("Initiating platesolve");
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.CaputureSolveSyncAndReslew, new object[] { tokenSource, progress });
            }
            return true;
        }

        private async Task<bool> StopAutoguider(CancellationTokenSource tokenSource, IProgress<string> progress) {
            await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.PauseGuider, tokenSource.Token);
            return true;
        }

        private async Task<bool> SelectNewGuideStar(CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report("Select new Guidestar");
            await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.AutoSelectGuideStar, tokenSource.Token);

            return true;
        }

        private async Task<bool> ResumeAutoguider(CancellationTokenSource tokenSource, IProgress<string> progress) {

            await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.ResumeGuider, tokenSource.Token);

            return true;
        }

        private async Task<bool> Settle(CancellationTokenSource tokenSource, IProgress<string> progress) {
            RemainingTime = TimeSpan.FromSeconds(Settings.MeridianFlipSettleTime);
            do {
                progress.Report(Locale.Loc.Instance["LblSettle"] + " " + RemainingTime.ToString(@"hh\:mm\:ss"));
                RemainingTime = TimeSpan.FromSeconds(RemainingTime.TotalSeconds - 1);

                await Task.Delay(1000, tokenSource.Token);

            } while (RemainingTime.TotalSeconds >= 1);
            return true;
        }

        private async Task<bool> DoMeridianFlip() {
            try {
                Steps = new AutomatedWorkflow();

                Steps.Add(new WorkflowStep("StopAutoguider", Locale.Loc.Instance["LblStopAutoguider"], () => StopAutoguider(_tokensource, _progress)));

                Steps.Add(new WorkflowStep("PassMeridian", Locale.Loc.Instance["LblPassMeridian"], () => PassMeridian(_tokensource, _progress)));
                Steps.Add(new WorkflowStep("Flip", Locale.Loc.Instance["LblFlip"], () => DoFilp(_tokensource, _progress)));
                if (Settings.RecenterAfterFlip) {
                    Steps.Add(new WorkflowStep("Recenter", Locale.Loc.Instance["LblRecenter"], () => Recenter(_tokensource, _progress)));
                }


                Steps.Add(new WorkflowStep("SelectNewGuideStar", Locale.Loc.Instance["LblSelectNewGuideStar"], () => SelectNewGuideStar(_tokensource, _progress)));
                Steps.Add(new WorkflowStep("ResumeAutoguider", Locale.Loc.Instance["LblResumeAutoguider"], () => ResumeAutoguider(_tokensource, _progress)));

                Steps.Add(new WorkflowStep("Settle", Locale.Loc.Instance["LblSettle"], () => Settle(_tokensource, _progress)));

                await Steps.Process();
            } catch (Exception) {
                await ResumeAutoguider(new CancellationTokenSource(), _progress);
                Mediator.Instance.Notify(MediatorMessages.SetTelescopeTracking, true);
                return false;
            }
            return true;
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand {
            get {
                return _cancelCommand;
            }
            set {
                _cancelCommand = value;
            }

        }
    }

    public class AutomatedWorkflow : BaseINPC, ICollection<WorkflowStep> {
        private AsyncObservableLimitedSizedStack<WorkflowStep> _internalStack;

        public AutomatedWorkflow() {
            _internalStack = new AsyncObservableLimitedSizedStack<WorkflowStep>(int.MaxValue);
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

        public bool Remove(WorkflowStep item) {
            return _internalStack.Remove(item);
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

        private WorkflowStep _activeStep;
        public WorkflowStep ActiveStep {
            get {
                return _activeStep;
            }
            set {
                _activeStep = value;
                RaisePropertyChanged();
            }
        }
    }


    public class WorkflowStep : BaseINPC {
        public WorkflowStep(string id, string title, Func<Task<bool>> action) {
            Id = id;
            Title = title;
            Action = action;
        }

        private string _id;
        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _title;
        public string Title {
            get {
                return _title;
            }
            set {
                _title = value;
                RaisePropertyChanged();
            }
        }

        private bool _finished;
        public bool Finished {
            get {
                return _finished;
            }
            set {
                _finished = value;
                RaisePropertyChanged();
            }
        }

        private Func<Task<bool>> _action;
        public Func<Task<bool>> Action {
            get {
                return _action;
            }
            set {
                _action = value;
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
