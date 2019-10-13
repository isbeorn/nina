using NINA.Utility;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace AlnitakAstrosystemsSDK {

    public static class LIBAlnitak {

        public enum DeviceType { UNKNOWN, FlatManXL, FlatManL, FlatMan, FlipMask, FlipFlat };

        public enum CoverState { UNKNOWN, NotOpenClosed, Closed, Open, TimedOut };

        public struct DeviceInfo {

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

        public static List<string> ScanForDevices() {
            List<string> result = new List<string>();

            foreach (string s in SerialPort.GetPortNames()) {
                SerialPort _serialPort = GetSerialPort(s);

                try {
                    _serialPort.Open();
                    _serialPort.Write(">POOO\r");
                    string rv = _serialPort.ReadLine();
                    if (isAlnitakDevice(rv, 'P')) {
                        string info = $"{rv.Substring(2, 2)};{_serialPort.PortName}";
                        result.Add(info);
                        Logger.Debug($"LIBAlnitak: Found device: {info}");
                    } else {
                        Logger.Debug($"LIBAlnitak: Found something else: {rv}");
                    }
                } catch (TimeoutException) {
                    Logger.Debug($"LIBAlnitak: timed out for port : {_serialPort.PortName}");
                } finally {
                    _serialPort.Close();
                }
            }
            return result;
        }

        public static CoverState GetCoverState(string portName) {
            SerialPort _serialPort = GetSerialPort(portName);
            CoverState result = CoverState.UNKNOWN;
            try {
                _serialPort.Open();
                _serialPort.Write(">SOOO\r");
                string rv = _serialPort.ReadLine();
                if (isAlnitakDevice(rv, 'S')) {
                    switch (rv[6]) {
                        case '0':
                            result = CoverState.NotOpenClosed;
                            break;

                        case '1':
                            result = CoverState.Closed;
                            break;

                        case '2':
                            result = CoverState.Open;
                            break;

                        case '3':
                            result = CoverState.TimedOut;
                            break;
                    }
                    Logger.Debug($"LIBAlnitak: Cover is: {result}");
                } else {
                    Logger.Debug($"LIBAlnitak: Something else: {rv}");
                }
            } catch (TimeoutException) {
                Logger.Debug($"LIBAlnitak: timed out for port : {_serialPort.PortName}");
            } finally {
                _serialPort.Close();
            }
            return result;
        }

        public static bool GetLightOn(string portName) {
            SerialPort _serialPort = GetSerialPort(portName);
            bool result = false;
            try {
                _serialPort.Open();
                _serialPort.Write(">SOOO\r");
                string rv = _serialPort.ReadLine();
                if (isAlnitakDevice(rv, 'S')) {
                    switch (rv[5]) {
                        case '0':
                            result = false;
                            break;

                        case '1':
                            result = true;
                            break;
                    }
                    Logger.Debug($"LIBAlnitak: Light is: {(result ? "On" : "Off")}");
                } else {
                    Logger.Debug($"LIBAlnitak: Something else: {rv}");
                }
            } catch (TimeoutException) {
                Logger.Debug($"LIBAlnitak: timed out for port : {_serialPort.PortName}");
            } finally {
                _serialPort.Close();
            }
            return result;
        }

        public static void SetLightOn(string portName, bool on) {
            SerialPort _serialPort = GetSerialPort(portName);
            try {
                string command = on ? ">LOOO\r" : ">DOOO\r";
                _serialPort.Open();
                _serialPort.Write(command);
                _serialPort.ReadLine();
                Logger.Debug($"LIBAlnitak: Turning light: {(on ? "On" : "Off")}");
            } catch (TimeoutException) {
                Logger.Debug($"LIBAlnitak: timed out for port : {_serialPort.PortName}");
            } finally {
                _serialPort.Close();
            }
        }

        public static bool GetMotorOn(string portName) {
            SerialPort _serialPort = GetSerialPort(portName);
            bool result = false;
            try {
                _serialPort.Open();
                _serialPort.Write(">SOOO\r");
                string rv = _serialPort.ReadLine();
                if (isAlnitakDevice(rv, 'S')) {
                    switch (rv[4]) {
                        case '0':
                            result = false;
                            break;

                        case '1':
                            result = true;
                            break;
                    }
                    Logger.Debug($"LIBAlnitak: Motor is: {(result ? "On" : "Off")}");
                } else {
                    Logger.Debug($"LIBAlnitak: Something else: {rv}");
                }
            } catch (TimeoutException) {
                Logger.Debug($"LIBAlnitak: timed out for port : {_serialPort.PortName}");
            } finally {
                _serialPort.Close();
            }
            return result;
        }

        public static void OpenCloseCover(string portName, bool open) {
            SerialPort _serialPort = GetSerialPort(portName);
            try {
                string command = open ? ">OOOO\r" : ">COOO\r";
                _serialPort.Open();
                _serialPort.Write(command);
                _serialPort.ReadLine();
                Logger.Debug($"LIBAlnitak: Setting cover to: {(open ? "Open" : "Close")}");
            } catch (TimeoutException) {
                Logger.Debug($"LIBAlnitak: timed out for port : {_serialPort.PortName}");
            } finally {
                _serialPort.Close();
            }
        }

        public static uint GetFWrev(string portName) {
            SerialPort _serialPort = GetSerialPort(portName);
            uint result = 0;
            try {
                _serialPort.Open();
                _serialPort.Write(">VOOO\r");
                string rv = _serialPort.ReadLine();
                if (isAlnitakDevice(rv, 'V')) {
                    result = UInt32.Parse(rv.Substring(4, 3));
                    Logger.Debug($"LIBAlnitak: Firmware Version: {result}");
                } else {
                    Logger.Debug($"LIBAlnitak: Something else: {rv}");
                }
            } catch (TimeoutException) {
                Logger.Debug($"LIBAlnitak: timed out for port : {_serialPort.PortName}");
            } finally {
                _serialPort.Close();
            }

            return result;
        }

        private static SerialPort GetSerialPort(string portName) {
            SerialPort _serialPort = new SerialPort();
            _serialPort.PortName = portName;
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.NewLine = "\n";
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            return _serialPort;
        }

        private static bool isAlnitakDevice(string rv, char command) {
            if (rv == null || rv.Length != 7) {
                return false;
            }
            if (rv[0] != '*' || rv[1] != command) {
                return false;
            }
            if (rv[2] != '1' && rv[2] != '9') {
                return false;
            }
            if (rv[3] != '0' &&
                rv[3] != '5' &&
                rv[3] != '8' &&
                rv[3] != '9') {
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