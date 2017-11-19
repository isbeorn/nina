using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.Windows.Threading;
using NINA.Model.MyGuider;
using System.Windows.Input;
using System.Threading;
using NINA.Utility.Notification;

namespace NINA.ViewModel {
    class GuiderVM : DockableVM {
        public GuiderVM() : base() {
            Title = "LblGuider";
            ContentId = nameof(GuiderVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["GuiderSVG"];
            ConnectGuiderCommand = new AsyncCommand<bool>(
                async () =>
                    await Task.Run<bool>(() => Connect()),
                (object o) =>
                    !(Guider?.Connected == true)
            );
            DisconnectGuiderCommand = new RelayCommand((object o) => Disconnect(), (object o) => Guider?.Connected == true);



            /*SetUpPlotModels();*/

            MaxY = 4;

            GuideStepsHistory = new AsyncObservableLimitedSizedStack<IGuideStep>(100);
            GuideStepsHistoryMinimal = new AsyncObservableLimitedSizedStack<IGuideStep>(10);
            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsync(async (object o) => {
                CancellationToken token = (CancellationToken)o;
                await Dither(token);
            }, AsyncMediatorMessages.DitherGuider);

            Mediator.Instance.RegisterAsync(async (object o) => {
                CancellationToken token = (CancellationToken)o;
                await Pause(token);
            }, AsyncMediatorMessages.PauseGuider);

            Mediator.Instance.RegisterAsync(async (object o) => {
                CancellationToken token = (CancellationToken)o;
                await Resume(token);
            }, AsyncMediatorMessages.ResumeGuider);

            Mediator.Instance.RegisterAsync(async (object o) => {
                CancellationToken token = (CancellationToken)o;
                await AutoSelectGuideStar(token);
            }, AsyncMediatorMessages.AutoSelectGuideStar);

            Mediator.Instance.RegisterAsync(async (object o) => {
                CancellationToken token = (CancellationToken)o;
                await StartGuiding(token);
            }, AsyncMediatorMessages.StartGuider);
        }

        private async Task AutoSelectGuideStar(CancellationToken token) {
            if(Guider?.Connected == true) {
                await Guider?.AutoSelectGuideStar();
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }            
        }

        private async Task Pause(CancellationToken token) {
            if(Guider?.Connected == true) {
                await Guider?.Pause(true);
            }            
        }

        private async Task Resume(CancellationToken token) {
            if (Guider?.Connected == true) {
                await Guider?.Pause(false);

                var elapsed = new TimeSpan();
                while (Guider?.Paused == true) {
                    elapsed += await Utility.Utility.Delay(500, token);
                    if (elapsed.TotalSeconds > 60) {
                        //Failsafe when phd is not sending resume message
                        Notification.ShowWarning(Locale.Loc.Instance["LblGuiderNoResume"]/*, ToastNotifications.NotificationsSource.NeverEndingNotification*/);
                        break;
                    }
                }
            }
                
        }

        private static Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

        private async Task<bool> Connect() {
            GuideStepsHistory.Clear();
            GuideStepsHistoryMinimal.Clear();
            Guider = new PHD2Guider();
            Guider.PropertyChanged += Guider_PropertyChanged;
            return await Guider.Connect();
        }

        private bool Disconnect() {
            GuideStepsHistory.Clear();
            GuideStepsHistoryMinimal.Clear();
            var discon = Guider.Disconnect();
            Guider = null;
            return discon;
        }

        private void Guider_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "GuideStep") {
                GuideStepsHistoryMinimal.Add(Guider.GuideStep);
                GuideStepsHistory.Add(Guider.GuideStep);
            }
        }

        private async Task<bool> StartGuiding(CancellationToken token) {
            if (Guider?.Connected == true) {
                await Guider.StartGuiding();
                return await Task.Run<bool>(async () => {                    
                    while (Guider?.IsCalibrating == true) {
                        await Task.Delay(1000, token);                        
                    }
                    return true;
                });

            } else {
                return false;
            }
        }

        private async Task<bool> Dither(CancellationToken token) {
            if(Guider?.Connected == true) {
                await Guider.Dither();
                return await Task.Run<bool>(async () => {
                    var elapsed = new TimeSpan();
                    while (Guider?.IsDithering == true) {
                        elapsed += await Utility.Utility.Delay(500, token);

                        if (elapsed.TotalSeconds > 60) {
                            //Failsafe when phd is not sending settlingdone message
                            Notification.ShowWarning(Locale.Loc.Instance["LblGuiderNoSettleDone"]);
                            Guider.IsDithering = false;
                        }
                    }
                    return true;
                });
            } else {
                return false;
            }
        }


        public AsyncObservableLimitedSizedStack<IGuideStep> GuideStepsHistory { get; set; }
        public AsyncObservableLimitedSizedStack<IGuideStep> GuideStepsHistoryMinimal { get; set; }

        private IGuider _guider;
        public IGuider Guider {
            get {
                return _guider;
            }
            set {
                _guider = value;
                RaisePropertyChanged();
            }
        }

        public double Interval {
            get {
                return MaxY / 4;
            }
        }

        private double _maxY;
        public double MaxY {
            get {
                return _maxY;
            }

            set {
                _maxY = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(MinY));
            }
        }


        public double MinY {
            get {
                return -MaxY;
            }
        }


        public ICommand ConnectGuiderCommand { get; private set; }

        public ICommand DisconnectGuiderCommand { get; private set; }
    }
}
