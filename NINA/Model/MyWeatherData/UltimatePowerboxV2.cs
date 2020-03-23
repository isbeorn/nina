using NINA.Locale;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyWeatherData {

    public class UltimatePowerboxV2 : BaseINPC, IWeatherData {
        private readonly IProfileService _profileService;
        private const string AUTO = "AUTO";
        public IPegasusDevice Sdk { get; set; } = PegasusDevice.Instance;

        public UltimatePowerboxV2(IProfileService profileService) {
            _profileService = profileService;
            PortName = profileService?.ActiveProfile?.SwitchSettings?.Upbv2PortName ?? AUTO;
        }

        public string PortName {
            get => _profileService.ActiveProfile.SwitchSettings.Upbv2PortName;
            set {
                _profileService.ActiveProfile.SwitchSettings.Upbv2PortName = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => false;
        public string Id => "07bbbbfe-effa-441b-b14b-6088a59a3fde";
        public string Name => "Ultimate Powerbox V2";
        public string Category => "Pegasus Astro";
        private bool _connected;

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        private string _description;

        public string Description {
            get => _description;
            set {
                _description = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo => "Serial driver for devices with firmware >= v1.3 (July 2019)";
        public string DriverVersion => "1.0";

        public async Task<bool> Connect(CancellationToken token) {
            if (!Sdk.InitializeSerialPort(PortName, this)) return false;
            if (Connected) return true;
            return await Task.Run(() => {
                try {
                    var command = new FirmwareVersionCommand();
                    var response = Sdk.SendCommand<FirmwareVersionResponse>(command);
                    if (!response.IsValid) {
                        Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                                     $"Command was: {command} Response was: {response}.");
                        Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
                        Connected = false;
                        return Connected;
                    }

                    Description =
                        $"Ultimate Powerbox V2 on port {PortName}. Firmware version: {response.FirmwareVersion}";
                    Connected = true;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Connected = false;
                }

                RaiseAllPropertiesChanged();
                return Connected;
            }, token);
        }

        public void Disconnect() {
            if (!Connected) return;
            Connected = false;
            Sdk.Dispose(this);
        }

        public void SetupDialog() {
            throw new NotImplementedException();
        }

        public double AveragePeriod {
            get => double.NaN;
            set { }
        }

        public double CloudCover => double.NaN;

        private StatusResponse GetStatus() {
            try {
                var command = new StatusCommand();
                var response = Sdk.SendCommand<StatusResponse>(command);
                if (response.IsValid) return response;
                Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                             $"Command was: {command} Response was: {response}.");
                Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
                return null;
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        public double DewPoint {
            get {
                if (!Connected) return double.NaN;
                var status = GetStatus();
                return status?.DewPoint ?? double.NaN;
            }
        }

        public double Humidity {
            get {
                if (!Connected) return double.NaN;
                var status = GetStatus();
                return status?.Humidity ?? double.NaN;
            }
        }

        public double Pressure => double.NaN;
        public double RainRate => double.NaN;
        public double SkyBrightness => double.NaN;
        public double SkyQuality => double.NaN;
        public double SkyTemperature => double.NaN;
        public double StarFWHM => double.NaN;

        public double Temperature {
            get {
                if (!Connected) return double.NaN;
                var status = GetStatus();
                return status?.Temperature ?? double.NaN;
            }
        }

        public double WindDirection => double.NaN;
        public double WindGust => double.NaN;
        public double WindSpeed => double.NaN;
    }
}