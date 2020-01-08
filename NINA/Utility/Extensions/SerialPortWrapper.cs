#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace NINA.Utility.Extensions {

    public class SerialPortProvider : ISerialPortProvider {
        /**
         * Below are the device IDs that are currently known
         * Arduino Uno:                        DeviceID = \\DESKTOP - UV2EJQU\root\cimv2: Win32_PnPEntity.DeviceID = "USB\\VID_2341&PID_0043\\55834323633351D0E041" On USB Serial Device(COM11)
         * Arduino Leonardo:                   DeviceID = \\DESKTOP - UV2EJQU\root\cimv2: Win32_PnPEntity.DeviceID = "USB\\VID_2341&PID_8036&MI_00\\7&4A255E4&0&0000" On USB Serial Device(COM8)
         * Optec USB/Serial cable:             DeviceID = \\DESKTOP - UV2EJQU\root\cimv2: Win32_PnPEntity.DeviceID = "FTDIBUS\\VID_0403+PID_6001+OP2CGIIAA\\0000" On USB Serial Port(COM4)
         * Flat Fielder:                       DeviceID = \\DESKTOP - UV2EJQU\root\cimv2: Win32_PnPEntity.DeviceID = "FTDIBUS\\VID_0403+PID_6001+A82I5L7VA\\0000" On USB Serial Port(COM3)
         * Pegasus Astro Ultimate Powerbox V2: DeviceID = \\DESKTOP - UV2EJQU\root\cimv2: Win32_PnPEntity.DeviceID = "FTDIBUS\\VID_0403+PID_6015+UPB248E11MA\\0000" On USB Serial Port(COM5)
         **/

        private const string ALL_SERIAL_PORTS_QUERY =
            "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"";

        private readonly Dictionary<string, bool> dtrEnableValue;

        public SerialPortProvider() {
            dtrEnableValue = new Dictionary<string, bool>();
            var searcher = new ManagementObjectSearcher(ALL_SERIAL_PORTS_QUERY);
            foreach (var entry in searcher.Get()) {
                var com = Regex.Match((string)entry["Name"], @"COM\d+");
                var leonardoMatch = Regex.Match((string)entry["DeviceID"], @"USB\\VID_2341&PID_8036&MI_00");
                dtrEnableValue.Add(com.Value, leonardoMatch.Length > 0);
                Logger.Debug($"Found {entry} on {entry["Name"]}");
            }
        }

        public ISerialPort GetSerialPort(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One,
            Handshake handShake = Handshake.None, bool dtrEnable = false, string newLine = "\n", int readTimeout = 500, int writeTimeout = 500) {
            if (string.IsNullOrEmpty(portName)) return null;
            var dtr = dtrEnable || dtrEnableValue[portName];
            return new SerialPortWrapper {
                PortName = portName,
                BaudRate = baudRate,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits,
                Handshake = handShake,
                DtrEnable = dtr,
                NewLine = newLine,
                ReadTimeout = readTimeout,
                WriteTimeout = writeTimeout
            };
        }

        public ReadOnlyCollection<string> GetPortNames(string deviceQuery = null, bool addDivider = true, bool addGenericPorts = true) {
            var result = new List<string>();
            try {
                if (deviceQuery != null) { result.AddRange(GetComPortsForQuery(deviceQuery).OrderBy(s => s)); }
                if (addDivider) { result.Add("----"); }
                if (addGenericPorts) {
                    foreach (var portName in GetComPortsForQuery(ALL_SERIAL_PORTS_QUERY).OrderBy(s => s)) {
                        if (!result.Contains(portName)) result.Add(portName);
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                result = SerialPort.GetPortNames().OrderBy(s => s).ToList();
            }
            return new ReadOnlyCollection<string>(result);
        }

        private IEnumerable<string> GetComPortsForQuery(string query) {
            var result = new List<string>();
            var searcher = new ManagementObjectSearcher(query);
            foreach (var entry in searcher.Get()) {
                var match = Regex.Match((string)entry["Name"], @"COM\d+");
                result.Add(match.Value);
                Logger.Debug($"Found {entry} on {entry["Name"]}");
            }
            return result;
        }
    }

    public interface ISerialPortProvider {

        ISerialPort GetSerialPort(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8,
            StopBits stopBits = StopBits.One, Handshake handShake = Handshake.None, bool dtrEnable = false,
            string newLine = "\n", int readTimeout = 500, int writeTimeout = 500);

        ReadOnlyCollection<string> GetPortNames(string deviceQuery = null, bool addDivider = true,
            bool addGenericPorts = true);
    }

    public interface ISerialPort : IDisposable {
        string PortName { get; set; }
        bool DtrEnable { get; set; }

        void Write(string value);

        string ReadLine();

        void Open();

        void Close();
    }

    public sealed class SerialPortWrapper : ISerialPort {
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
        public bool DtrEnable { get => _serialPort.DtrEnable; set => _serialPort.DtrEnable = value; }
        public string NewLine { get => _serialPort.NewLine; set => _serialPort.NewLine = value; }
        public int ReadTimeout { get => _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }
        public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }

        public void Close() => _serialPort.Close();

        public void Dispose() => _serialPort.Dispose();

        public void Open() => _serialPort.Open();

        public string ReadLine() => _serialPort.ReadLine();

        public void Write(string value) => _serialPort.Write(value);
    }
}