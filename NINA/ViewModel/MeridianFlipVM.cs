using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel
{
    class MeridianFlipVM : BaseVM
    {
        public MeridianFlipVM(ITelescope telescope) {
            Telescope = telescope;
            Steps = new AsyncObservableCollection<WorkflowStep>() {
                new WorkflowStep("PassMeridian", "Pass Meridian"),
                new WorkflowStep("StopAutoguider", "Stop Autoguider"),
                new WorkflowStep("Flip", "Do the Flip"),
                new WorkflowStep("Recenter", "Platesolve & Recenter"),
                new WorkflowStep("ResumeAutoguider", "Resume Autoguider"),
                new WorkflowStep("Settle", "Settle")
            };

            /* todo */

            PlateSolveResultList = new AsyncObservableCollection<PlateSolveResult>();
            PlateSolveResultList.Add(new PlateSolveResult());
            PlateSolveResultList.Add(new PlateSolveResult());

            

            RegisterMediatorMessages();

            //Task.Run(() => CheckMeridianFlip(new CaptureSequence(1000, "abc", null, null, 1), null));

        }

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

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                Telescope = (ITelescope)o;
            }, MediatorMessages.TelescopeChanged);

        }

        private AsyncObservableCollection<WorkflowStep> _steps;
        public AsyncObservableCollection<WorkflowStep> Steps {
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
        private void ShouldFlip(double exposureTime) {
            if (Settings.AutoMeridianFlip) {
                if (Telescope?.Connected == true) {

                    if (Telescope.TimeToMeridianFlip < (exposureTime / 60 / 60)) {
                    }
                }
            }
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
        public async Task CheckMeridianFlip(CaptureSequence seq, CancellationTokenSource tokenSource) {

            



            if (Settings.AutoMeridianFlip) {
                if (Telescope?.Connected == true) {

                    if (Telescope.TimeToMeridianFlip < (seq.ExposureTime / 60 / 60)) {
                        var service = new WindowService();
                        var flip = DoFlip(new CancellationTokenSource(), new Progress<string>(p => Status = p));

                        service.ShowDialog(this, "Meridian Flip");
                        await flip;

                        await service.Close();
                    }
                }
            }

        }

        private async Task<bool> DoFlip(CancellationTokenSource tokenSource, IProgress<string> progress) {
            try {

            

                //Notification.ShowInformation(Locale.Loc.Instance["LblMeridianFlipInit"], TimeSpan.FromSeconds(RemainingTime.Seconds));
                ActiveStep = Steps[0];
                do {
                    RemainingTime = TimeSpan.FromSeconds(Telescope.TimeToMeridianFlip * 60 * 60);
                    //progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", RemainingTime));
                    await Task.Delay(500, tokenSource.Token);
                
                } while (RemainingTime.TotalSeconds >= 1);

                ActiveStep.Finished = true;
                ActiveStep = Steps[1];
            
                await PHD2Client.Pause(true);

                ActiveStep.Finished = true;
                ActiveStep = Steps[2];

                var coords = Telescope.Coordinates;

           
                var flipsuccess = Telescope.MeridianFlip();

                //Let scope settle 
                await Task.Delay(TimeSpan.FromSeconds(Settings.MeridianFlipSettleTime), tokenSource.Token);

                ActiveStep.Finished = true;
                ActiveStep = Steps[3];

                if (flipsuccess) {
                    if (Settings.RecenterAfterFlip) {

                        //todo needs to be solve until error < x                                

                        await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SolveWithCapture, new object[] { null, progress, tokenSource });

                    
                        Mediator.Instance.Notify(MediatorMessages.SyncronizeTelescope, null);
                        Telescope.SlewToCoordinates(coords.RA, coords.Dec);
                    }

                    ActiveStep.Finished = true;
                    ActiveStep = Steps[4];
                
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

                    ActiveStep.Finished = true;
                    ActiveStep = Steps[5];

                    RemainingTime = TimeSpan.FromSeconds(Settings.MeridianFlipSettleTime);
                    do {

                        RemainingTime = TimeSpan.FromSeconds(RemainingTime.TotalSeconds - 1);

                        //progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", RemainingTime));
                        await Task.Delay(1000, tokenSource.Token);

                    } while (RemainingTime.TotalSeconds >= 1);
                    

                    ActiveStep.Finished = true;
                }

            } catch (Exception) {
                return false;
            }
            return true;
        }
    }

    public class WorkflowStep : BaseINPC {
        public WorkflowStep(string id, string title) {
            Id = id;
            Title = title;
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
    }
}
