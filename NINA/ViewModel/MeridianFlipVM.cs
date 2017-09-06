using NINA.Model.MyTelescope;
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
        public MeridianFlipVM() {
            Test = "Did it work?";
        }

        private string _test;
        public string Test {
            get {
                return _test;
            }
            private set {
                _test = value;
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
        private async Task CheckMeridianFlip(double exposureTime, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (Settings.AutoMeridianFlip) {
                if (Telescope?.Connected == true) {

                    if (Telescope.TimeToMeridianFlip < (exposureTime / 60 / 60)) {
                        int remainingtime = (int)(Telescope.TimeToMeridianFlip * 60 * 60);
                        Notification.ShowInformation(Locale.Loc.Instance["LblMeridianFlipInit"], TimeSpan.FromSeconds(remainingtime));
                        do {
                            progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", remainingtime));
                            await Task.Delay(1000, tokenSource.Token);
                            remainingtime = remainingtime - 1;
                        } while (remainingtime > 0);


                        progress.Report("Pausing PHD2");
                        await PHD2Client.Pause(true);

                        var coords = Telescope.Coordinates;

                        progress.Report("Executing Meridian Flip");
                        var flipsuccess = Telescope.MeridianFlip();

                        if (flipsuccess) {
                            if (Settings.RecenterAfterFlip) {
                                progress.Report("Initializing Platesolve");

                                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SolveWithCapture, new object[] { null, progress, tokenSource});


                                progress.Report("Sync and Reslew");
                                Mediator.Instance.Notify(MediatorMessages.SyncronizeTelescope, null);
                                Telescope.SlewToCoordinates(coords.RA, coords.Dec);
                            }

                            progress.Report("Resuming PHD2");
                            await PHD2Client.AutoSelectStar();
                            await PHD2Client.Pause(false);

                            var time = 0;
                            while (PHD2Client.Paused) {
                                await Task.Delay(500, tokenSource.Token);
                                time += 500;
                                if (time > 20000) {
                                    //Failsafe when phd is not sending resume message
                                    Notification.ShowWarning(Locale.Loc.Instance["LblPHD2NoResume"]);
                                    tokenSource.Token.ThrowIfCancellationRequested();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
