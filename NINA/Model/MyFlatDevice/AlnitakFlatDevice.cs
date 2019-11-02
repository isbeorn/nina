using NINA.Profile;
using NINA.Utility;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NINA.Utility.Notification;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility.WindowService;

namespace NINA.Model.MyFlatDevice {

    public class AlnitakFlatDevice : BaseINPC, IFlatDevice {
        private ISerialPort _serialPort;
        private readonly IProfileService _profileService;

        public AlnitakFlatDevice(string name, IProfileService profileService) {
            _profileService = profileService;
            Name = name.Split(';')[0];
            PortName = name.Split(';')[1];
            SetupSerialPort(PortName);
        }

        private void SetupSerialPort(string portName) {
            _serialPort = new SerialPortWrapper {
                PortName = portName,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                NewLine = "\n",
                ReadTimeout = 500,
                WriteTimeout = 500
            };
        }

        public ISerialPort SerialPort {
            set => _serialPort = value;
        }

        public CoverState CoverState {
            get {
                if (!Connected) {
                    return CoverState.Unknown;
                }
                var command = new StateCommand();
                var response = SendCommand<StateResponse>(command);
                if (response.IsValid) return response.CoverState;
                Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                             $"Command was: {command} Response was: {response}.");
                Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                return CoverState.Unknown;
            }
        }

        public int MaxBrightness => 255;

        public int MinBrightness => 0;

        private bool _supportsOpenClose;

        public bool SupportsOpenClose {
            get => _supportsOpenClose;
            set {
                if (_supportsOpenClose == value) return;
                _supportsOpenClose = value;
                RaisePropertyChanged();
            }
        }

        public bool LightOn {
            get {
                if (!Connected) {
                    return false;
                }
                var command = new StateCommand();
                var response = SendCommand<StateResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    return false;
                }

                return response.LightOn;
            }
            set {
                if (Connected) {
                    if (value) {
                        var command = new LightOnCommand();
                        var response = SendCommand<LightOnResponse>(command);
                        if (!response.IsValid) {
                            Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                         $"Command was: {command} Response was: {response}.");
                            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }
                    } else {
                        var command = new LightOffCommand();
                        var response = SendCommand<LightOffResponse>(command);
                        if (!response.IsValid) {
                            Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                         $"Command was: {command} Response was: {response}.");
                            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }
                    }
                }
                RaisePropertyChanged();
            }
        }

        public int Brightness {
            get {
                var result = 0;
                if (!Connected) {
                    return result;
                }
                var command = new GetBrightnessCommand();
                var response = SendCommand<GetBrightnessResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                } else {
                    result = response.Brightness;
                }
                return result;
            }
            set {
                if (Connected) {
                    if (value < MinBrightness) {
                        value = MinBrightness;
                    }

                    if (value > MaxBrightness) {
                        value = MaxBrightness;
                    }
                    var command = new SetBrightnessCommand(value);
                    var response = SendCommand<SetBrightnessResponse>(command);
                    if (!response.IsValid) {
                        Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                     $"Command was: {command} Response was: {response}.");
                        Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    }
                }
                RaisePropertyChanged();
            }
        }

        public string PortName {
            get => _profileService.ActiveProfile.FlatDeviceSettings.PortName;
            set {
                _profileService.ActiveProfile.FlatDeviceSettings.PortName = value;
                SetupSerialPort(value);
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => true;

        private string _id;

        public string Id {
            get => _id;
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name {
            get => _name;
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public string Category => "Alnitak Astrosystems";

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

        public string DriverInfo => "Serial Driver written by Igor von Nyssen.";

        public string DriverVersion => "1.0";

        public async Task<bool> Close(CancellationToken ct) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(() => {
                var command = new CloseCommand();
                var response = SendCommand<CloseResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    return false;
                }
                while (IsMotorRunning()) {
                    Thread.Sleep(300);
                }
                return CoverState == CoverState.Closed;
            }, ct);
        }

        public async Task<bool> Connect(CancellationToken ct) {
            return await Task.Run(() => {
                Connected = true;
                var command = new FirmwareVersionCommand();
                var response = SendCommand<FirmwareVersionResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    Connected = false;
                    return Connected;
                }

                Name = response.Name;
                SupportsOpenClose = response.DeviceSupportsOpenClose;
                Description = $"{response.Name} on port {_serialPort.PortName}. Firmware version: {response.FirmwareVersion}";

                RaiseAllPropertiesChanged();
                return Connected;
            }, ct);
        }

        public void Disconnect() {
            Connected = false;
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        public async Task<bool> Open(CancellationToken ct) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(() => {
                var command = new OpenCommand();
                var response = SendCommand<OpenResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    return false;
                }
                while (IsMotorRunning()) {
                    Thread.Sleep(300);
                }
                return CoverState == CoverState.Open;
            }, ct);
        }

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Alnitak Flat Panel Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        private readonly SemaphoreSlim ssSendCommand = new SemaphoreSlim(1, 1);

        private T SendCommand<T>(Command command) where T : Response, new() {
            var result = string.Empty;
            ssSendCommand.Wait();
            try {
                _serialPort.Open();
                Logger.Debug($"AlnitakFlatDevice: command : {command}");
                _serialPort.Write(command.CommandString);
                result = _serialPort.ReadLine();
                Logger.Debug($"AlnitakFlatDevice: response : {result}");
            } catch (TimeoutException) {
                Logger.Error($"AlnitakFlatDevice: timed out for port : {_serialPort.PortName}");
            } catch (Exception ex) {
                Logger.Error($"AlnitakFlatDevice: Unexpected exception : {ex}");
            } finally {
                _serialPort.Close();
                ssSendCommand.Release();
            }
            return new T() { DeviceResponse = result };
        }

        private bool IsMotorRunning() {
            var response = SendCommand<StateResponse>(new StateCommand());
            return response.IsValid && response.MotorRunning;
        }
    }

    //below is for testing purposes only
    public interface ISerialPort {
        string PortName { get; set; }

        void Write(string value);

        string ReadLine();

        void Open();

        void Close();
    }

    public class SerialPortWrapper : ISerialPort {
        private readonly SerialPort _serialPort;

        public SerialPortWrapper() {
            _serialPort = new SerialPort();
        }

        public string PortName { get => _serialPort.PortName; set => _serialPort.PortName = value; }
        public int BaudRate { get => _serialPort.BaudRate; set => _serialPort.BaudRate = value; }
        public Parity Parity { get => _serialPort.Parity; set => _serialPort.Parity = value; }
        public int DataBits { get => _serialPort.DataBits; set => _serialPort.DataBits = value; }
        public StopBits StopBits { get => _serialPort.StopBits; set => _serialPort.StopBits = value; }
        public Handshake Handshake { get => _serialPort.Handshake; set => _serialPort.Handshake = value; }
        public string NewLine { get => _serialPort.NewLine; set => _serialPort.NewLine = value; }
        public int ReadTimeout { get => _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }
        public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }

        public void Close() => _serialPort.Close();

        public void Open() => _serialPort.Open();

        public string ReadLine() => _serialPort.ReadLine();

        public void Write(string value) => _serialPort.Write(value);
    }
}