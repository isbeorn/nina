using NINA.Utility;
using System;
using System.IO.Ports;

namespace AlnitakAstrosystemsSDK {

    public static class LIBAlnitak {

        public enum DeviceType { UNKNOWN, FlatManXL, FlatManL, FlatMan, FlipMask, FlipFlat };

        public enum CoverState { NotOpenClosed, Closed, Open, TimedOut };

        public static AsyncObservableCollection<DeviceInfo> devices = new AsyncObservableCollection<DeviceInfo>();

        public struct DeviceInfo {

            public DeviceInfo(DeviceType deviceType, string portName) {
                this.Model = deviceType.ToString();
                this.Id = $"{deviceType} on {portName}";
                this.portName = portName;
                this.brightness = 0;
                this.lightOn = false;
                this.motorOn = false;
                this.coverState = CoverState.TimedOut;
                this.FWrev = 0;
            }

            /// <summary>
            /// The flat field device's model name
            /// </summary>
            public string Model;

            /// <summary>
            /// The flat field device's id
            /// </summary>
            public string Id;

            /// <summary>
            /// The com port the device is connected to
            /// </summary>
            public string portName;

            /// <summary>
            /// The brightness of the device
            /// </summary>
            public uint brightness;

            public uint minBrightness => 0;
            public uint maxBrightness => 255;

            /// <summary>
            /// true if the light is on
            /// </summary>
            public bool lightOn;

            /// <summary>
            /// true if the motor is running
            /// </summary>
            public bool motorOn;

            /// <summary>
            /// The open / closed state of the cover
            /// </summary>
            public CoverState coverState;

            /// <summary>
            /// The open / closed state of the cover
            /// </summary>
            public uint FWrev;
        }

        public static bool Ping() {
            SerialPort _serialPort = new SerialPort();

            foreach (string s in SerialPort.GetPortNames()) {
                _serialPort.PortName = s;
                _serialPort.BaudRate = 9600;
                _serialPort.Parity = Parity.None;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Handshake = Handshake.None;
                _serialPort.NewLine = "\n";
                _serialPort.ReadTimeout = 500;
                _serialPort.WriteTimeout = 500;

                _serialPort.Open();

                try {
                    _serialPort.Write(">POOO\r");
                    string rv = _serialPort.ReadLine();
                    if (isAlnitakDevice(rv)) {
                        DeviceType type = getDeviceType(Int32.Parse(rv.Substring(2, 2)));
                        DeviceInfo info = new DeviceInfo(type, _serialPort.PortName);
                        devices.Add(info);
                        Logger.Debug($"LIBAlnitak: Found device: {info.Id}");
                    } else {
                        Logger.Debug($"LIBAlnitak: Found something else: {rv}");
                    }
                } catch (TimeoutException) {
                    Logger.Debug($"LIBAlnitak: timed out for port : {_serialPort.PortName}");
                } finally {
                    _serialPort.Close();
                }
            }
            return false;
        }

        private static bool isAlnitakDevice(string pingReturn) {
            if (pingReturn[0] != '*' || pingReturn[1] != 'P') {
                return false;
            }
            if (pingReturn[2] != '1' && pingReturn[2] != '9') {
                return false;
            }
            if (pingReturn[3] != '0' &&
                pingReturn[3] != '5' &&
                pingReturn[3] != '8' &&
                pingReturn[3] != '9') {
                return false;
            }
            if (pingReturn[4] != 'O' ||
                pingReturn[5] != 'O' ||
                pingReturn[6] != 'O') {
                return false;
            }
            return true;
        }

        private static DeviceType getDeviceType(int value) {
            switch (value) {
                case 10:
                    return DeviceType.FlatManXL;

                case 15:
                    return DeviceType.FlatManL;

                case 19:
                    return DeviceType.FlatMan;

                case 98:
                    return DeviceType.FlipMask;

                case 99:
                    return DeviceType.FlipFlat;

                default:
                    return DeviceType.UNKNOWN;
            }
        }
    }
}