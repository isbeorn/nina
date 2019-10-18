using NINA.Utility;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace NINA.Model.MyFlatDevice
{
    public static class AlnitakDevices
    {
        public static List<string> GetDevices()
        {
            var result = new List<string>();
            foreach (var portName in SerialPort.GetPortNames())
            {
                var serialPort = new SerialPort
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

                try
                {
                    serialPort.Open();
                    Logger.Debug($"AlnitakFlatDevice: command : {Command.Ping}");
                    serialPort.Write(Command.Ping);
                    var response = new Response(serialPort.ReadLine(), Command.PingReturn);
                    Logger.Debug($"AlnitakFlatDevice: response : {response}");
                    if (!response.IsValid)
                    {
                        continue;
                    }
                    result.Add($"{response.Name};{serialPort.PortName}");
                }
                catch (TimeoutException)
                {
                    Logger.Debug($"AlnitakFlatDevice: timed out for port : {serialPort.PortName}");
                }
                finally
                {
                    serialPort.Close();
                }
            }

            return result;
        }
    }

    public static class Command
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
    }

    public class Response
    {
        public bool IsValid { get; }
        public CoverState CoverState { get; set; }
        public string Name { get; set; }
        public int Brightness { get; set; }
        public bool MotorRunning { get; set; }
        public bool LightOn { get; set; }
        public int FirmwareVersion { get; set; }

        public Response(string response, char command)
        {
            if (!IsStructureValid(response, command))
            {
                IsValid = false;
                return;
            }

            if (!ParseDeviceId(response))
            {
                IsValid = false;
                return;
            }

            switch (command)
            {
                case Command.PingReturn:
                case Command.OpenReturn:
                case Command.CloseReturn:
                case Command.LightOnReturn:
                case Command.LightOffReturn:
                    if (!IsOoo(response))
                    {
                        IsValid = false;
                        return;
                    }

                    break;

                case Command.SetBrightnessReturn:
                case Command.GetBrightnessReturn:
                    if (!ParseBrightness(response, command))
                    {
                        IsValid = false;
                        return;
                    }

                    break;

                case Command.StateReturn:
                    if (!ParseState(response, command))
                    {
                        IsValid = false;
                        return;
                    }

                    break;

                case Command.VersionReturn:
                    if (!ParseFirmwareVersion(response, command))
                    {
                        IsValid = false;
                        return;
                    }

                    break;

                default:
                    IsValid = false;
                    return;
            }

            IsValid = true;
        }

        private static bool IsStructureValid(string response, char command)
        {
            if (response == null || response.Length != 7) return false;
            return response[0] == '*' && response[1] == command;
        }

        private bool ParseDeviceId(string response)
        {
            try
            {
                switch (int.Parse(response.Substring(2, 2)))
                {
                    case 10:
                        Name = "Flat-Man_XL";
                        return true;

                    case 15:
                        Name = "Flat-Man_L";
                        return true;

                    case 19:
                        Name = "Flat-Man";
                        return true;

                    case 98:
                        Name = "Flip-Mask/Remote Dust Cover";
                        return true;

                    case 99:
                        Name = "Flip-Flat";
                        return true;

                    default:
                        Name = "Unknown device";
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsOoo(string response)
        {
            return response.Substring(4, 3).Equals("OOO");
        }

        private bool ParseBrightness(string response, char command)
        {
            try
            {
                var value = int.Parse(response.Substring(4, 3));
                if (value < 0 || value > 255)
                {
                    return false;
                }

                Brightness = value;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool ParseState(string response, char command)
        {
            switch (response[4])
            {
                case '0':
                    MotorRunning = false;
                    break;

                case '1':
                    MotorRunning = true;
                    break;

                default:
                    return false;
            }

            switch (response[5])
            {
                case '0':
                    LightOn = false;
                    break;

                case '1':
                    LightOn = true;
                    break;

                default:
                    return false;
            }

            switch (response[6])
            {
                case '0':
                    CoverState = CoverState.NotOpenClosed;
                    break;

                case '1':
                    CoverState = CoverState.Closed;
                    break;

                case '2':
                    CoverState = CoverState.Open;
                    break;

                case '3':
                    CoverState = CoverState.Unknown;
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool ParseFirmwareVersion(string response, char command)
        {
            try
            {
                FirmwareVersion = int.Parse(response.Substring(4, 3));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}