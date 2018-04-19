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
using NINA.Utility.Mediator;

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

            Mediator.Instance.RegisterAsyncRequest(
                new DitherGuiderMessageHandle(async (DitherGuiderMessage msg) => {
                    return await Dither(msg.Token);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new PauseGuiderMessageHandle(async (PauseGuiderMessage msg) => {
                    if(msg.Pause) {
                        return await Pause(msg.Token);
                    } else {
                        return await Resume(msg.Token);
                    }
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new AutoSelectGuideStarMessageHandle(async (AutoSelectGuideStarMessage msg) => {
                    return await AutoSelectGuideStar(msg.Token);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new StartGuiderMessageHandle(async (StartGuiderMessage msg) => {
                    return await StartGuiding(msg.Token);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new StopGuiderMessageHandle(async (StopGuiderMessage msg) => {
                    return await StopGuiding(msg.Token);
                })
            );
        }

        private async Task<bool> AutoSelectGuideStar(CancellationToken token) {
            if(Guider?.Connected == true) {
                var result = await Guider?.AutoSelectGuideStar();
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return result;
            } else {
                return false;
            }          
        }

        private async Task<bool> Pause(CancellationToken token) {
            if(Guider?.Connected == true) {
                return await Guider?.Pause(true, token);
            } else {
                return false;
            }    
        }

        private async Task<bool> Resume(CancellationToken token) {
            if (Guider?.Connected == true) {
                await Guider?.Pause(false, token);
                await Utility.Utility.Wait(TimeSpan.FromSeconds(Settings.GuiderSettleTime), token);
                return true;
            } else {
                return false;
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
            if (e.PropertyName == "PixelScale") {
                PixelScale = Guider.PixelScale;
            }
            if (e.PropertyName == "GuideStep") {
                var step = Guider.GuideStep;
                if(GuiderScale == GuiderScaleEnum.ARCSECONDS) {
                    ConvertStepToArcSec(step);
                }
                GuideStepsHistoryMinimal.Add(step);
                GuideStepsHistory.Add(step);
            }
        }

        private void ConvertStepToArcSec(IGuideStep pixelStep) {
            pixelStep.RADistanceRaw = pixelStep.RADistanceRaw * PixelScale;
            pixelStep.DecDistanceRaw = pixelStep.DecDistanceRaw * PixelScale;
            pixelStep.RADistanceGuide = pixelStep.RADistanceGuide * PixelScale;
            pixelStep.DecDistanceGuide = pixelStep.DecDistanceGuide * PixelScale;
        }

        private void ConvertStepToPixels(IGuideStep arcsecStep) {
            arcsecStep.RADistanceRaw = arcsecStep.RADistanceRaw / PixelScale;
            arcsecStep.DecDistanceRaw = arcsecStep.DecDistanceRaw / PixelScale;
            arcsecStep.RADistanceGuide = arcsecStep.RADistanceGuide / PixelScale;
            arcsecStep.DecDistanceGuide = arcsecStep.DecDistanceGuide / PixelScale;
        }

        private GuiderScaleEnum _guiderScale;
        public GuiderScaleEnum GuiderScale {
            get {
                return _guiderScale;
            }
            set {
                _guiderScale = value;
                RaisePropertyChanged();
                foreach(IGuideStep s in GuideStepsHistory) {
                    if(GuiderScale == GuiderScaleEnum.ARCSECONDS) {
                        ConvertStepToArcSec(s);
                    } else {
                        ConvertStepToPixels(s);
                    }
                    
                }
                foreach (IGuideStep s in GuideStepsHistoryMinimal) {
                    if (GuiderScale == GuiderScaleEnum.ARCSECONDS) {
                        ConvertStepToArcSec(s);
                    } else {
                        ConvertStepToPixels(s);
                    }
                }
                RaisePropertyChanged(nameof(GuideStepsHistory));
                RaisePropertyChanged(nameof(GuideStepsHistoryMinimal));
            }
        }

        private double _pixelScale;
        public double PixelScale {
            get {
                return _pixelScale;
            }
            set {
                _pixelScale = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> StartGuiding(CancellationToken token) {
            if (Guider?.Connected == true) {
                return await Guider.StartGuiding(token);
            } else {
                return false;
            }
        }

        private async Task<bool> StopGuiding(CancellationToken token) {
            if (Guider?.Connected == true) {
                return await Guider.StopGuiding(token);
            } else {
                return false;
            }
        }

        private async Task<bool> Dither(CancellationToken token) {
            if(Guider?.Connected == true) {
                Mediator.Instance.Request(new StatusUpdateMessage() { Status = new Model.ApplicationStatus() { Status = Locale.Loc.Instance["LblDither"], Source = Title } });
                await Guider?.Dither(token);
                Mediator.Instance.Request(new StatusUpdateMessage() { Status = new Model.ApplicationStatus() { Status = string.Empty, Source = Title } });
                return true;
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
