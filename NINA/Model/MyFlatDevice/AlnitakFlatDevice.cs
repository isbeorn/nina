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

        private static class Command
        {
            public const string Ping = ">POOO\r";
            public const string Open = ">OOOO\r";
            public const string Close = ">COOO\r";
            public const string LightOn = ">LOOO\r";
            public const string LightOff = ">DOOO\r";
            public const string SetBrightness = ">B{0}\r";
            public const string GetBrightness = ">JOOO\r";
            public const string State = ">SOOO\r";
            public const string Version = ">VOOO\r";

            public const char PingReturn = 'P';
            public const char OpenReturn = 'O';
            public const char CloseReturn = 'C';
            public const char LightOnReturn = 'L';
            public const char LightOffReturn = 'D';
            public const char SetBrightnessReturn = 'B';
            public const char GetBrightnessReturn = 'J';
            public const char StateReturn = 'S';
            public const char VersionReturn = 'V';
        };

        public AlnitakFlatDevice(string portName)
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
                var response = SendCommand(Command.State);
                if (!IsValidResponse(response, Command.StateReturn))
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.State} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                    return CoverState.Unknown;
                }
                switch (response[6])
                {
                    case '0':
                        return CoverState.NotOpenClosed;

                    case '1':
                        return CoverState.Closed;

                    case '2':
                        return CoverState.Open;

                    default:
                        return CoverState.Unknown;
                }
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
                var response = SendCommand(Command.State);
                if (!IsValidResponse(response, Command.StateReturn))
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.State} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                    return false;
                }
                switch (response[5])
                {
                    case '1':
                        return true;

                    default:
                        return false;
                }
            }
            set {
                if (Connected)
                {
                    string response;
                    if (value)
                    {
                        response = SendCommand(Command.LightOn);
                        if (!IsValidResponse(response, Command.LightOnReturn))
                        {
                            Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.LightOn} Response was: {response}.");
                            Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                        }
                    }
                    else
                    {
                        response = SendCommand(Command.LightOff);
                        if (!IsValidResponse(response, Command.LightOffReturn))
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
                var response = SendCommand(Command.GetBrightness);
                if (!IsValidResponse(response, Command.GetBrightnessReturn))
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.GetBrightness} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                }
                else
                {
                    result = int.Parse(response.Substring(4, 3));
                }
                return result;
            }
            set {
                if (Connected)
                {
                    if (value < MinBrightness || value > MaxBrightness) { return; }
                    var response = SendCommand(Command.SetBrightness.Replace("{0}", value.ToString("000")));
                    if (!IsValidResponse(response, Command.SetBrightnessReturn))
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
                var response = SendCommand(Command.Close);
                if (!IsValidResponse(response, Command.CloseReturn))
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
                var response = SendCommand(Command.Ping);
                if (!IsValidResponse(response, Command.PingReturn))
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.Ping} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                }
                _description = GetDescription(response);

                response = SendCommand(Command.Version);
                if (!IsValidResponse(response, Command.VersionReturn))
                {
                    Logger.Error($"Invalid response from flat device on port {_serialPort.PortName}. Command was: {Command.Version} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblInvalidResponseFlatDevice"]);
                }

                _description += $" Firmware version: {response.Substring(4, 3)}";
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
                var response = SendCommand(Command.Open);
                if (!IsValidResponse(response, Command.OpenReturn))
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

        private bool IsValidResponse(string response, char command)
        {
            if (response == null || response.Length != 7) { return false; }
            if (response[0] != '*' || response[1] != command) { return false; }
            try
            {
                var deviceId = int.Parse(response.Substring(2, 2));
                if (deviceId != 10 &&
                    deviceId != 15 &&
                    deviceId != 19 &&
                    deviceId != 98 &&
                    deviceId != 99)
                {
                    return false;
                }

                switch (command)
                {
                    case 'P':
                    case 'O':
                    case 'C':
                    case 'L':
                    case 'D':
                        if (!response.Substring(4, 3).Equals("OOO"))
                        {
                            return false;
                        }
                        break;

                    case 'B':
                    case 'J':
                        var value = int.Parse(response.Substring(4, 3));
                        if (value < 0 || value > 255)
                        {
                            return false;
                        }
                        break;

                    case 'S':
                        if (response[4] != '0' && response[4] != '1') { return false; }
                        if (response[5] != '0' && response[5] != '1') { return false; }
                        if (response[6] != '0' &&
                            response[6] != '1' &&
                            response[6] != '2' &&
                            response[6] != '3')
                        {
                            return false;
                        }
                        break;

                    case 'V':
                        _ = int.Parse(response.Substring(4, 3));
                        break;

                    default:
                        return false;
                }
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
            return true;
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

        private string GetDescription(string response)
        {
            string result;
            switch (int.Parse(response.Substring(2, 2)))
            {
                case 10:
                    result = $"Flat-Man_XL on port {_serialPort.PortName}.";
                    break;

                case 15:
                    result = $"Flat-Man_L on port {_serialPort.PortName}.";
                    break;

                case 19:
                    result = $"Flat-Man on port {_serialPort.PortName}.";
                    break;

                case 98:
                    result = $"Flip-Mask/Remote Dust Cover on port {_serialPort.PortName}.";
                    break;

                case 99:
                    result = $"Flip-Flat on port {_serialPort.PortName}.";
                    break;

                default:
                    result = $"Unknown device on port {_serialPort.PortName}.";
                    break;
            }
            return result;
        }

        private bool IsMotorRunning()
        {
            var response = SendCommand(Command.State);
            if (!IsValidResponse(response, Command.StateReturn))
            {
                return false;
            }
            switch (response[4])
            {
                case '1':
                    return true;

                default:
                    return false;
            }
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