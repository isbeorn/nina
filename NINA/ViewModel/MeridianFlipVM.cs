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

namespace NINA.ViewModel
{
    class MeridianFlipVM : BaseVM
    {
        public MeridianFlipVM() {           

            /* todo */

            PlateSolveResultList = new AsyncObservableCollection<PlateSolveResult>();
            PlateSolveResultList.Add(new PlateSolveResult());
            PlateSolveResultList.Add(new PlateSolveResult());

            

            RegisterMediatorMessages();
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

        private AsyncObservableCollection<PlateSolveResult> _plateSolveResultList;
        public AsyncObservableCollection<PlateSolveResult> PlateSolveResultList {
            get {
                return _plateSolveResultList;
            }
            set {
                _plateSolveResultList = value;
                RaisePropertyChanged();
            }
        }

        
        
        private PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
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

        //move to imaging vm?
        private bool ShouldFlip(double exposureTime) {
            if (Settings.AutoMeridianFlip) {
                if (Telescope?.Connected == true) {

                    if (Telescope.TimeToMeridianFlip < (exposureTime / 60 / 60)) {
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
        /// 2) Pause PHD2
        /// 3) Execute the flip
        /// 4) If recentering is enabled, platesolve current position, sync and recenter to old target position
        /// 5) Resume PHD2
        /// </summary>
        /// <param name="seq">Current Sequence row</param>
        /// <param name="tokenSource">cancel token</param>
        /// <param name="progress">progress reporter</param>
        /// <returns></returns>
        public async Task CheckMeridianFlip(CaptureSequence seq, CancellationTokenSource tokensource) {
            if(ShouldFlip(seq.ExposureTime)) {
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
            do {
                RemainingTime = TimeSpan.FromSeconds(Telescope.TimeToMeridianFlip * 60 * 60);
                //progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", RemainingTime));
                await Task.Delay(500, tokenSource.Token);

            } while (RemainingTime.TotalSeconds >= 1);
            return true;
        }

        private async Task<bool> DoFilp(CancellationTokenSource tokenSource, IProgress<string> progress) {
            _targetCoordinates = Telescope.Coordinates;

            var flipsuccess = Telescope.MeridianFlip();

            //Let scope settle 
            await Task.Delay(TimeSpan.FromSeconds(Settings.MeridianFlipSettleTime), tokenSource.Token);

            return flipsuccess;
        }

        private async Task<bool> Recenter(CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (Settings.RecenterAfterFlip) {

                //todo needs to be solve until error < x                                

                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SolveWithCapture, new object[] { null, progress, tokenSource });


                Mediator.Instance.Notify(MediatorMessages.SyncronizeTelescope, null);
                Telescope.SlewToCoordinates(_targetCoordinates.RA, _targetCoordinates.Dec);
            }

            

            return true;
        }

        private async Task<bool> StopAutoguider(CancellationTokenSource tokensource, IProgress<string> progress) {
            if (PHD2Client?.Connected == true) {
                await PHD2Client.Pause(true);
            }
            return true;
        }

        private async Task<bool> ResumeAutoguider(CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (PHD2Client?.Connected == true) {
                await PHD2Client.AutoSelectStar();
                await Task.Delay(TimeSpan.FromSeconds(5), tokenSource.Token);
                await PHD2Client.Pause(false);

                var time = 0;
                while (PHD2Client.Paused) {
                    await Task.Delay(500, tokenSource.Token);
                    time += 500;
                    if (time > 20000) {
                        //Failsafe when phd is not sending resume message
                        Notification.ShowWarning(Locale.Loc.Instance["LblPHD2NoResume"]/*, ToastNotifications.NotificationsSource.NeverEndingNotification*/);
                        break;
                    }
                    tokenSource.Token.ThrowIfCancellationRequested();
                }
            }
            return true;
        }

        private async Task<bool> Settle(CancellationTokenSource tokenSource, IProgress<string> progress) {
            RemainingTime = TimeSpan.FromSeconds(Settings.MeridianFlipSettleTime);
            do {

                RemainingTime = TimeSpan.FromSeconds(RemainingTime.TotalSeconds - 1);

                //progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", RemainingTime));
                await Task.Delay(1000, tokenSource.Token);

            } while (RemainingTime.TotalSeconds >= 1);
            return true;
        }

        private async Task<bool> DoMeridianFlip() {
            try {
                Steps = new AutomatedWorkflow() {
                    new WorkflowStep("PassMeridian", Locale.Loc.Instance["LblPassMeridian"], () => PassMeridian(_tokensource, _progress)),
                    new WorkflowStep("StopAutoguider", Locale.Loc.Instance["LblStopAutoguider"], () => StopAutoguider(_tokensource, _progress)),
                    new WorkflowStep("Flip", Locale.Loc.Instance["LblFlip"], () => DoFilp(_tokensource, _progress)),
                    new WorkflowStep("Recenter", Locale.Loc.Instance["LblRecenter"], () => Recenter(_tokensource, _progress)),
                    new WorkflowStep("ResumeAutoguider", Locale.Loc.Instance["LblResumeAutoguider"], () => ResumeAutoguider(_tokensource, _progress)),
                    new WorkflowStep("Settle", Locale.Loc.Instance["LblSettle"], () => Settle(_tokensource, _progress))
                };
                await Steps.Process();
            } catch (Exception) {
                return false;
            }
            return true;
        }
    }

    public class AutomatedWorkflow : BaseINPC, ICollection<WorkflowStep> {
        private AsyncObservableLimitedSizedStack<WorkflowStep> _internalStack;

        public AutomatedWorkflow () {
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
                if (!success) { break; }
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
