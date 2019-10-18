using NINA.Profile;
using NINA.Utility;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NINA.Utility.Notification;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    public class AlnitakFlatDevice : BaseINPC, IFlatDevice {
        private ISerialPort _serialPort;

        public AlnitakFlatDevice(string name, IProfileService profileService) {
            SetupSerialPort(name.Split(';')[1]);
        }

        public AlnitakFlatDevice(string portName) {
            SetupSerialPort(portName);
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
                var response = new StateResponse(SendCommand(command));
                if (response.IsValid) return response.CoverState;
                Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                             $"Command was: {command} Response was: {response}.");
                return CoverState.Unknown;
            }
        }

        public int MaxBrightness => 255;

        public int MinBrightness => 0;

        public bool LightOn {
            get {
                if (!Connected) {
                    return false;
                }
                var command = new StateCommand();
                var response = new StateResponse(SendCommand(command));
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    return false;
                }

                return response.LightOn;
            }
            set {
                if (Connected) {
                    if (value) {
                        var command = new LightOnCommand();
                        var response = new LightOnResponse(SendCommand(command));
                        if (!response.IsValid) {
                            Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                         $"Command was: {command} Response was: {response}.");
                        }
                    } else {
                        var command = new LightOffCommand();
                        var response = new LightOffResponse(SendCommand(command));
                        if (!response.IsValid) {
                            Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                         $"Command was: {command} Response was: {response}.");
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
                var response = new GetBrightnessResponse(SendCommand(command));
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                } else {
                    result = response.Brightness;
                }
                return result;
            }
            set {
                if (Connected) {
                    if (value < MinBrightness || value > MaxBrightness) { return; }
                    var command = new SetBrightnessCommand(value);
                    var response = new SetBrightnessResponse(SendCommand(command));
                    if (!response.IsValid) {
                        Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                     $"Command was: {command} Response was: {response}.");
                        Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                    }
                }
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => false;

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
            return await Task.Run(() => {
                var command = new CloseCommand();
                var response = new CloseResponse(SendCommand(command));
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
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
                var response = new FirmwareVersionResponse(SendCommand(command));
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                    Connected = false;
                    return Connected;
                }

                Name = response.Name;
                _description = $"{response.Name} on port {_serialPort.PortName}. Firmware version: {response.FirmwareVersion}";

                RaiseAllPropertiesChanged();
                return Connected;
            }, ct);
        }

        public void Disconnect() {
            Connected = false;
        }

        public async Task<bool> Open(CancellationToken ct) {
            return await Task.Run(() => {
                var command = new OpenCommand();
                var response = new OpenResponse(SendCommand(command));
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                    return false;
                }
                while (IsMotorRunning()) {
                    Thread.Sleep(300);
                }
                return CoverState == CoverState.Open;
            }, ct);
        }

        public void SetupDialog() {
        }

        private string SendCommand(Command command) {
            var result = string.Empty;

            try {
                _serialPort.Open();
                Logger.Debug($"AlnitakFlatDevice: command : {command}");
                _serialPort.Write(command.CommandString);
                result = _serialPort.ReadLine();
                Logger.Debug($"AlnitakFlatDevice: response : {result}");
            } catch (TimeoutException) {
                Logger.Debug($"AlnitakFlatDevice: timed out for port : {_serialPort.PortName}");
            } finally {
                _serialPort.Close();
            }
            return result;
        }

        private bool IsMotorRunning() {
            var response = new StateResponse(SendCommand(new StateCommand()));
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