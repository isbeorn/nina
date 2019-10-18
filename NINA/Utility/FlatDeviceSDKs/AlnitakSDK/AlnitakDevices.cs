using NINA.Model.MyFlatDevice;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public static class AlnitakDevices {

        public static List<string> GetDevices() {
            var result = new List<string>();
            foreach (var portName in SerialPort.GetPortNames()) {
                var serialPort = new SerialPort {
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

                try {
                    serialPort.Open();
                    var command = new PingCommand();
                    Logger.Debug($"AlnitakFlatDevice: command : {command}");
                    serialPort.Write(command.CommandString);
                    var response = new PingResponse(serialPort.ReadLine());
                    Logger.Debug($"AlnitakFlatDevice: response : {response}");
                    if (!response.IsValid) {
                        continue;
                    }
                    result.Add($"{response.Name};{serialPort.PortName}");
                } catch (TimeoutException) {
                    Logger.Debug($"AlnitakFlatDevice: timed out for port : {serialPort.PortName}");
                } finally {
                    serialPort.Close();
                }
            }

            return result;
        }
    }

    public abstract class Command {
        public string CommandString { get; protected set; }
    }

    public class PingCommand : Command {

        public PingCommand() {
            CommandString = ">POOO\r";
        }
    }

    public class OpenCommand : Command {

        public OpenCommand() {
            CommandString = ">OOOO\r";
        }
    }

    public class CloseCommand : Command {

        public CloseCommand() {
            CommandString = ">COOO\r";
        }
    }

    public class LightOnCommand : Command {

        public LightOnCommand() {
            CommandString = ">LOOO\r";
        }
    }

    public class LightOffCommand : Command {

        public LightOffCommand() {
            CommandString = ">DOOO\r";
        }
    }

    public class SetBrightnessCommand : Command {

        public SetBrightnessCommand(int brightness) {
            CommandString = $">B{brightness:000}\r";
        }
    }

    public class GetBrightnessCommand : Command {

        public GetBrightnessCommand() {
            CommandString = ">JOOO\r";
        }
    }

    public class StateCommand : Command {

        public StateCommand() {
            CommandString = ">SOOO\r";
        }
    }

    public class FirmwareVersionCommand : Command {

        public FirmwareVersionCommand() {
            CommandString = ">VOOO\r";
        }
    }

    public abstract class Response {
        public string Name { get; private set; }
        public bool IsValid { get; protected set; }

        protected Response(string response) {
            IsValid = true;
            if (response == null || response.Length != 7) {
                IsValid = false;
                return;
            }

            if (response[0] != '*') {
                IsValid = false;
                return;
            }

            if (!ParseDeviceId(response)) {
                IsValid = false;
            }
        }

        private bool ParseDeviceId(string response) {
            try {
                switch (int.Parse(response.Substring(2, 2))) {
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
            } catch (Exception) {
                return false;
            }
        }

        protected bool IsOoo(string response) {
            return response.Substring(4, 3).Equals("OOO");
        }
    }

    public class PingResponse : Response {

        public PingResponse(string response) : base(response) {
            if (response[1] != 'P' || !IsOoo(response)) {
                IsValid = false;
            }
        }
    }

    public class OpenResponse : Response {

        public OpenResponse(string response) : base(response) {
            if (response[1] != 'O' || !IsOoo(response)) {
                IsValid = false;
            }
        }
    }

    public class CloseResponse : Response {

        public CloseResponse(string response) : base(response) {
            if (response[1] != 'C' || !IsOoo(response)) {
                IsValid = false;
            }
        }
    }

    public class LightOnResponse : Response {

        public LightOnResponse(string response) : base(response) {
            if (response[1] != 'L' || !IsOoo(response)) {
                IsValid = false;
            }
        }
    }

    public class LightOffResponse : Response {

        public LightOffResponse(string response) : base(response) {
            if (response[1] != 'D' || !IsOoo(response)) {
                IsValid = false;
            }
        }
    }

    public abstract class BrightnessResponse : Response {
        public int Brightness { get; protected set; }

        protected BrightnessResponse(string response) : base(response) {
            if (!ParseBrightness(response)) {
                IsValid = false;
            }
        }

        protected bool ParseBrightness(string response) {
            try {
                var value = int.Parse(response.Substring(4, 3));
                if (value < 0 || value > 255) {
                    return false;
                }

                Brightness = value;
            } catch (Exception) {
                return false;
            }

            return true;
        }
    }

    public class SetBrightnessResponse : BrightnessResponse {

        public SetBrightnessResponse(string response) : base(response) {
            if (response[1] != 'B') {
                IsValid = false;
            }
        }
    }

    public class GetBrightnessResponse : BrightnessResponse {

        public GetBrightnessResponse(string response) : base(response) {
            if (response[1] != 'J') {
                IsValid = false;
            }
        }
    }

    public class StateResponse : Response {
        public bool MotorRunning { get; private set; }
        public bool LightOn { get; private set; }
        public CoverState CoverState { get; private set; }

        public StateResponse(string response) : base(response) {
            if (response[1] != 'S' || !ParseState(response)) {
                IsValid = false;
            }
        }

        private bool ParseState(string response) {
            switch (response[4]) {
                case '0':
                    MotorRunning = false;
                    break;

                case '1':
                    MotorRunning = true;
                    break;

                default:
                    return false;
            }

            switch (response[5]) {
                case '0':
                    LightOn = false;
                    break;

                case '1':
                    LightOn = true;
                    break;

                default:
                    return false;
            }

            switch (response[6]) {
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
    }

    public class FirmwareVersionResponse : Response {
        public int FirmwareVersion { get; private set; }

        public FirmwareVersionResponse(string response) : base(response) {
            if (response[1] != 'V' || !ParseFirmwareVersion(response)) {
                IsValid = false;
            }
        }

        private bool ParseFirmwareVersion(string response) {
            try {
                FirmwareVersion = int.Parse(response.Substring(4, 3));
                return true;
            } catch (Exception) {
                return false;
            }
        }
    }
}