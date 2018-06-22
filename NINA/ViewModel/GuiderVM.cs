using NINA.Model.MyGuider;
using NINA.Model;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.Utility.Profile;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class GuiderVM : DockableVM {

        public GuiderVM(IProfileService profileService) : base(profileService) {
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

            GuideStepsHistory = new GuideStepsHistory(HistorySize);

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
                await Utility.Utility.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.GuiderSettings.SettleTime), token);
                return true;
            } else {
                return false;
            }
        }

        public int HistorySize {
            get {
                return profileService.ActiveProfile.GuiderSettings.PHD2HistorySize;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.PHD2HistorySize = value;
                GuideStepsHistory.HistorySize = value;
                RaisePropertyChanged();
            }
        }

        private static Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

        private void ResetGraphValues() {
            GuideStepsHistory.Clear();
        }

        private async Task<bool> Connect() {
            ResetGraphValues();
            Guider = new PHD2Guider(profileService);
            Guider.PropertyChanged += Guider_PropertyChanged;
            return await Guider.Connect();
        }

        private bool Disconnect() {
            ResetGraphValues();
            var discon = Guider.Disconnect();
            Guider = null;
            return discon;
        }

        private void Guider_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "PixelScale") {
                GuideStepsHistory.PixelScale = Guider.PixelScale;
            }
            if (e.PropertyName == "GuideStep") {
                var step = Guider.GuideStep;

                GuideStepsHistory.AddGuideStep(step);
            }
        }

        public GuiderScaleEnum GuiderScale {
            get {
                return profileService.ActiveProfile.GuiderSettings.PHD2GuiderScale;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.PHD2GuiderScale = value;
                GuideStepsHistory.Scale = value;
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

        private GuideStepsHistory guideStepsHistory;

        public GuideStepsHistory GuideStepsHistory {
            get {
                return guideStepsHistory;
            }
            private set {
                guideStepsHistory = value;
                RaisePropertyChanged();
            }
        }

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

        public ICommand ConnectGuiderCommand { get; private set; }

        public ICommand DisconnectGuiderCommand { get; private set; }
    }
}