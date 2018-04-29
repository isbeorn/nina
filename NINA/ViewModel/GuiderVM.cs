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
using NINA.Utility.Profile;
using NINA.Utility.Enum;

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

            GuideStepsHistory = new AsyncObservableLimitedSizedStack<IGuideStep>(HistorySize);
            GuideStepsHistoryMinimal = new AsyncObservableLimitedSizedStack<IGuideStep>(MinimalHistorySize);

            RegisterMediatorMessages();
        }

        public enum GuideStepsHistoryType {
            GuideStepsLarge,
            GuideStepsMinimal
        }

        private void RegisterMediatorMessages() {

            Mediator.Instance.RegisterAsyncRequest(
                new DitherGuiderMessageHandle(async (DitherGuiderMessage msg) => {
                    return await Dither(msg.Token);
                })
            );

            Mediator.Instance.RegisterRequest(
                new GuideStepHistoryCountMessageHandle((GuideStepHistoryCountMessage msg) => {
                    return ChangeGuideSteps(msg.GuideSteps, msg.HistoryType);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new PauseGuiderMessageHandle(async (PauseGuiderMessage msg) => {
                    if (msg.Pause) {
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
            if (Guider?.Connected == true) {
                var result = await Guider?.AutoSelectGuideStar();
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return result;
            } else {
                return false;
            }
        }

        private async Task<bool> Pause(CancellationToken token) {
            if (Guider?.Connected == true) {
                return await Guider?.Pause(true, token);
            } else {
                return false;
            }
        }

        private async Task<bool> Resume(CancellationToken token) {
            if (Guider?.Connected == true) {
                await Guider?.Pause(false, token);
                await Utility.Utility.Wait(TimeSpan.FromSeconds(ProfileManager.Instance.ActiveProfile.GuiderSettings.SettleTime), token);
                return true;
            } else {
                return false;
            }
        }

        public int HistorySize {
            get {
                return ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2HistorySize;
            }
            set {
                ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2HistorySize = value;
                RaisePropertyChanged();
            }
        }

        public int MinimalHistorySize {
            get {
                return ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2HistorySize;
            }
            set {
                ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2MinimalHistorySize = value;
                RaisePropertyChanged();
            }
        }

        private static Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

        private bool ChangeGuideSteps(int historySize, GuideStepsHistoryType historyType) {
            AsyncObservableLimitedSizedStack<IGuideStep> collectionToChange = new AsyncObservableLimitedSizedStack<IGuideStep>(0);

            switch (historyType) {
                case GuideStepsHistoryType.GuideStepsLarge:
                    collectionToChange = GuideStepsHistory;
                    break;
                case GuideStepsHistoryType.GuideStepsMinimal:
                    collectionToChange = GuideStepsHistoryMinimal;
                    break;
            }

            collectionToChange.MaxSize = historySize;

            return true;
        }

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
                if (GuiderScale == GuiderScaleEnum.ARCSECONDS) {
                    ConvertStepToArcSec(step);
                }
                GuideStepsHistoryMinimal.Add(step);
                GuideStepsHistory.Add(step);
            }

            // really, that's how phd2 does it and I don't want to change it
            int n = GuideStepsHistory.Count;
            var items = GuideStepsHistory.Select(item => new Tuple<double, double>(item.RADistanceRaw, item.DecDistanceRaw));
            var itemsRa = items.Select(item => item.Item1);
            var itemsDec = items.Select(item => item.Item2);

            double sum_y2 = itemsRa.Sum(y => y * y);
            double sum_y = itemsRa.Sum(y => y);

            RMSRA = Math.Sqrt(n * sum_y2 - sum_y * sum_y) / n;

            sum_y2 = itemsDec.Sum(y => y * y);
            sum_y = itemsDec.Sum(y => y);

            RMSDec = Math.Sqrt(n * sum_y2 - sum_y * sum_y) / n;
            RMSTotal = Math.Sqrt((Math.Pow(RMSRA, 2) + Math.Pow(RMSDec, 2)));

            if(GuiderScale == GuiderScaleEnum.ARCSECONDS) {
                RMSRA *= PixelScale;
                RMSDec *= PixelScale;
                RMSTotal *= PixelScale;
            }
        }

        public double RMSRA {
            get {
                return _rmsRA;
            }
            set {
                _rmsRA = value;
                RaisePropertyChanged();
            }
        }

        public double RMSDec {
            get {
                return _rmsDec;
            }
            set {
                _rmsDec = value;
                RaisePropertyChanged();
            }
        }

        public double RMSTotal {
            get {
                return _rmsTotal;
            }
            set {
                _rmsTotal = value;
                RaiseAllPropertiesChanged();
            }
        }

        public string RMSRAText {
            get {
                return string.Format(Locale.Loc.Instance["LblPHD2RARMS"], RMSRA.ToString("0.00"));
            }
        }

        public string RMSDecText {
            get {
                return string.Format(Locale.Loc.Instance["LblPHD2DecRMS"], RMSDec.ToString("0.00"));
            }
        }

        public string RMSTotalText {
            get {
                return string.Format(Locale.Loc.Instance["LblPHD2TotalRMS"], RMSTotal.ToString("0.00"));
            }
        }

        double _rmsTotal;
        double _rmsRA;
        double _rmsDec;


        private void ConvertStepToArcSec(IGuideStep pixelStep) {
            // only displayed values are changed, not the raw ones
            pixelStep.RADistanceRawDisplay = pixelStep.RADistanceRaw * PixelScale;
            pixelStep.DecDistanceRawDisplay = pixelStep.DecDistanceRaw * PixelScale;
            pixelStep.RADistanceGuideDisplay = pixelStep.RADistanceGuide * PixelScale;
            pixelStep.DecDistanceGuideDisplay = pixelStep.DecDistanceGuide * PixelScale;
        }

        private void ConvertStepToPixels(IGuideStep arcsecStep) {
            arcsecStep.RADistanceRawDisplay = arcsecStep.RADistanceRaw / PixelScale;
            arcsecStep.DecDistanceRawDisplay = arcsecStep.DecDistanceRaw / PixelScale;
            arcsecStep.RADistanceGuideDisplay = arcsecStep.RADistanceGuide / PixelScale;
            arcsecStep.DecDistanceGuideDisplay = arcsecStep.DecDistanceGuide / PixelScale;
        }

        public GuiderScaleEnum GuiderScale {
            get {
                return ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2GuiderScale;
            }
            set {
                ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2GuiderScale = value;
                RaisePropertyChanged();
                foreach (IGuideStep s in GuideStepsHistory) {
                    if (GuiderScale == GuiderScaleEnum.ARCSECONDS) {
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
            if (Guider?.Connected == true) {
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
