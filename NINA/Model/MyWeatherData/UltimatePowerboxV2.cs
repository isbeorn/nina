using NINA.Locale;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility.SerialCommunication;

namespace NINA.Model.MyWeatherData {

    public class UltimatePowerboxV2 : BaseINPC, IWeatherData {
        private readonly IProfileService _profileService;
        private const string AUTO = "AUTO";
        public IPegasusDevice Sdk { get; set; } = PegasusDevice.Instance;

        public UltimatePowerboxV2(IProfileService profileService) {
            _profileService = profileService;
            PortName = profileService?.ActiveProfile?.SwitchSettings?.Upbv2PortName ?? AUTO;
        }

        private void LogAndNotify(ICommand command, InvalidDeviceResponseException ex) {
            Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                         $"Command was: {command} Response was: {ex.Message}.");
            Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
        }

        private void HandlePortClosed(ICommand command, SerialPortClosedException ex) {
            Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
            Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
            Disconnect();
            RaiseAllPropertiesChanged();
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
            return await Task.Run(async () => {
                var statusCommand = new StatusCommand();
                try {
                    _ = await Sdk.SendCommand<StatusResponse>(statusCommand);
                    Connected = true;
                    Description = $"Ultimate Powerbox V2 on port {PortName}. Firmware version: ";
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(statusCommand, ex);
                    Sdk.Dispose(this);
                    Connected = false;
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(statusCommand, ex);
                    Connected = false;
                }
                if (!Connected) return false;

                var fwCommand = new FirmwareVersionCommand();
                try {
                    var response = await Sdk.SendCommand<FirmwareVersionResponse>(fwCommand);
                    Description += $"{response.FirmwareVersion}";
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(fwCommand, ex);
                    Description += Loc.Instance["LblNoValidFirmwareVersion"];
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(fwCommand, ex);
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

        public double DewPoint {
            get {
                if (!Connected) return double.NaN;
                var command = new StatusCommand();
                try {
                    var response = Sdk.SendCommand<StatusResponse>(command).Result;
                    return response.DewPoint;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                return double.NaN;
            }
        }

        public double Humidity {
            get {
                if (!Connected) return double.NaN;
                var command = new StatusCommand();
                try {
                    var response = Sdk.SendCommand<StatusResponse>(command).Result;
                    return response.Humidity;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                return double.NaN;
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
                var command = new StatusCommand();
                try {
                    var response = Sdk.SendCommand<StatusResponse>(command).Result;
                    return response.Temperature;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                return double.NaN;
            }
        }

        public double WindDirection => double.NaN;
        public double WindGust => double.NaN;
        public double WindSpeed => double.NaN;
    }
}