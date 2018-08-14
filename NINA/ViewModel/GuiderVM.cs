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
using NINA.ViewModel.Interfaces;
using System.Collections.Generic;

namespace NINA.ViewModel {

    internal class GuiderVM : DockableVM, IGuiderVM {

        public GuiderVM(IProfileService profileService, GuiderMediator guiderMediator) : base(profileService) {
            Title = "LblGuider";
            ContentId = nameof(GuiderVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["GuiderSVG"];

            this.guiderMediator = guiderMediator;
            this.guiderMediator.RegisterHandler(this);

            ConnectGuiderCommand = new AsyncCommand<bool>(
                async () =>
                    await Task.Run<bool>(() => Connect()),
                (object o) =>
                    !(Guider?.Connected == true)
            );
            DisconnectGuiderCommand = new RelayCommand((object o) => Disconnect(), (object o) => Guider?.Connected == true);

            GuideStepsHistory = new GuideStepsHistory(HistorySize);
        }

        public enum GuideStepsHistoryType {
            GuideStepsLarge,
            GuideStepsMinimal
        }

        /// <summary>
        /// Starts recording RMS until StopRMSRecording is called
        /// </summary>
        /// <returns>Handle for the recording rms session</returns>
        public Guid StartRMSRecording() {
            var handle = Guid.NewGuid();
            var rms = new RMS();
            rms.SetScale(GuideStepsHistory.PixelScale);
            recordedRMS.Add(handle, rms);
            return handle;
        }

        /// <summary>
        /// Stops and returns RMS for the given rms session handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>recorded RMS</returns>
        public RMS StopRMSRecording(Guid handle) {
            if (recordedRMS.ContainsKey(handle)) {
                var rms = recordedRMS[handle];
                recordedRMS.Remove(handle);
                return rms;
            } else {
                return null;
            }
        }

        public async Task<bool> AutoSelectGuideStar(CancellationToken token) {
            if (Guider?.Connected == true) {
                var result = await Guider?.AutoSelectGuideStar();
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return result;
            } else {
                return false;
            }
        }

        public async Task<bool> PauseGuiding(CancellationToken token) {
            if (Guider?.Connected == true) {
                return await Guider?.Pause(true, token);
            } else {
                return false;
            }
        }

        public async Task<bool> ResumeGuiding(CancellationToken token) {
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

        public async Task<bool> Connect() {
            ResetGraphValues();
            Guider = new PHD2Guider(profileService);
            Guider.PropertyChanged += Guider_PropertyChanged;

            var connected = await Guider.Connect();

            GuiderInfo = new GuiderInfo {
                Connected = connected
            };
            BroadcastGuiderInfo();

            return connected;
        }

        public void Disconnect() {
            ResetGraphValues();
            Guider.Disconnect();
            Guider = null;
            GuiderInfo = DeviceInfo.CreateDefaultInstance<GuiderInfo>();
            BroadcastGuiderInfo();
        }

        private GuiderInfo guiderInfo;

        public GuiderInfo GuiderInfo {
            get {
                if (guiderInfo == null) {
                    guiderInfo = DeviceInfo.CreateDefaultInstance<GuiderInfo>();
                }
                return guiderInfo;
            }
            set {
                guiderInfo = value;
                RaisePropertyChanged();
            }
        }

        private void BroadcastGuiderInfo() {
            this.guiderMediator.Broadcast(GuiderInfo);
        }

        private void Guider_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "PixelScale") {
                GuideStepsHistory.PixelScale = Guider.PixelScale;
            }
            if (e.PropertyName == "GuideStep") {
                var step = Guider.GuideStep;

                GuideStepsHistory.AddGuideStep(step);

                foreach (RMS rms in recordedRMS.Values) {
                    rms.AddDataPoint(step.RADistanceRaw, step.DecDistanceRaw);
                }
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

        public async Task<bool> StartGuiding(CancellationToken token) {
            if (Guider?.Connected == true) {
                return await Guider.StartGuiding(token);
            } else {
                return false;
            }
        }

        public async Task<bool> StopGuiding(CancellationToken token) {
            if (Guider?.Connected == true) {
                return await Guider.StopGuiding(token);
            } else {
                return false;
            }
        }

        public async Task<bool> Dither(CancellationToken token) {
            if (Guider?.Connected == true) {
                Mediator.Instance.Request(new StatusUpdateMessage() { Status = new Model.ApplicationStatus() { Status = Locale.Loc.Instance["LblDither"], Source = Title } });
                await Guider?.Dither(token);
                Mediator.Instance.Request(new StatusUpdateMessage() { Status = new Model.ApplicationStatus() { Status = string.Empty, Source = Title } });
                return true;
            } else {
                Disconnect();
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

        private Dictionary<Guid, RMS> recordedRMS = new Dictionary<Guid, RMS>();

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

        private GuiderMediator guiderMediator;

        public ICommand ConnectGuiderCommand { get; private set; }

        public ICommand DisconnectGuiderCommand { get; private set; }
    }
}