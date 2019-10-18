using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice
{
    public class AlnitakFlatDevice : BaseINPC, IFlatDevice
    {
        private ISerialPort _serialPort;

        public AlnitakFlatDevice(string name, IProfileService profileService)
        {
            SetupSerialPort(name.Split(';')[1]);
        }

        public AlnitakFlatDevice(string portName)
        {
            SetupSerialPort(portName);
        }

        private void SetupSerialPort(string portName)
        {
            _serialPort = new SerialPortWrapper
            {
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
                if (!Connected)
                {
                    return CoverState.Unknown;
                }
                var response = new Response(SendCommand(Command.State), Command.StateReturn);
                if (response.IsValid) return response.CoverState;
                Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.State} Response was: {response}.");
                Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                return CoverState.Unknown;
            }
        }

        public int MaxBrightness => 255;

        public int MinBrightness => 0;

        public bool LightOn {
            get {
                if (!Connected)
                {
                    return false;
                }
                var response = new Response(SendCommand(Command.State), Command.StateReturn);
                if (!response.IsValid)
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.State} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                    return false;
                }

                return response.LightOn;
            }
            set {
                if (Connected)
                {
                    if (value)
                    {
                        var response = new Response(SendCommand(Command.LightOn), Command.LightOnReturn);
                        if (!response.IsValid)
                        {
                            Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.LightOn} Response was: {response}.");
                            Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                        }
                    }
                    else
                    {
                        var response = new Response(SendCommand(Command.LightOff), Command.LightOffReturn);
                        if (!response.IsValid)
                        {
                            Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.LightOff} Response was: {response}.");
                            Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                        }
                    }
                }
                RaisePropertyChanged();
            }
        }

        public int Brightness {
            get {
                var result = 0;
                if (!Connected)
                {
                    return result;
                }
                var response = new Response(SendCommand(Command.GetBrightness), Command.GetBrightnessReturn);
                if (!response.IsValid)
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.GetBrightness} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                }
                else
                {
                    result = response.Brightness;
                }
                return result;
            }
            set {
                if (Connected)
                {
                    if (value < MinBrightness || value > MaxBrightness) { return; }
                    var response = new Response(SendCommand(Command.SetBrightness.Replace("{0}", value.ToString("000"))), Command.SetBrightnessReturn);
                    if (!response.IsValid)
                    {
                        Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. " +
                                     $"Command" +
                                     $" was: {Command.SetBrightness.Replace("{0}", value.ToString("000"))} Response was: {response}.");
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

        public async Task<bool> Close(CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                var response = new Response(SendCommand(Command.Close), Command.CloseReturn);
                if (!response.IsValid)
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.Close} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                    return false;
                }
                while (IsMotorRunning())
                {
                    Thread.Sleep(300);
                }
                return CoverState == CoverState.Closed;
            }, ct);
        }

        public async Task<bool> Connect(CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                Connected = true;
                var response = new Response(SendCommand(Command.Version), Command.VersionReturn);
                if (!response.IsValid)
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.Version} Response was: {response}.");
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

        public void Disconnect()
        {
            Connected = false;
        }

        public async Task<bool> Open(CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                var response = new Response(SendCommand(Command.Open), Command.OpenReturn);
                if (!response.IsValid)
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.Open} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                    return false;
                }
                while (IsMotorRunning())
                {
                    Thread.Sleep(300);
                }
                return CoverState == CoverState.Open;
            }, ct);
        }

        public void SetupDialog()
        {
        }

        private string SendCommand(string command)
        {
            var result = string.Empty;

            try
            {
                _serialPort.Open();
                Logger.Debug($"AlnitakFlatDevice: command : {command}");
                _serialPort.Write(command);
                result = _serialPort.ReadLine();
                Logger.Debug($"AlnitakFlatDevice: response : {result}");
            }
            catch (TimeoutException)
            {
                Logger.Debug($"AlnitakFlatDevice: timed out for port : {_serialPort.PortName}");
            }
            finally
            {
                _serialPort.Close();
            }
            return result;
        }

        private bool IsMotorRunning()
        {
            var response = new Response(SendCommand(Command.State), Command.StateReturn);
            return response.IsValid && response.MotorRunning;
        }
    }

    //below is for testing purposes only
    public interface ISerialPort
    {
        string PortName { get; set; }

        void Write(string value);

        string ReadLine();

        void Open();

        void Close();
    }

    public class SerialPortWrapper : ISerialPort
    {
        private readonly SerialPort _serialPort;

        public SerialPortWrapper()
        {
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