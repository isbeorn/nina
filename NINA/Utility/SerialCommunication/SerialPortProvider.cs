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

namespace NINA.Utility.SerialCommunication {

    public class SerialPortProvider : ISerialPortProvider {
        /**
         * Below are the device IDs that are currently known
         * Arduino Uno R3:                     DeviceID = \\DESKTOP - UV2EJQU\root\cimv2: Win32_PnPEntity.DeviceID = "USB\\VID_2341&PID_0043\\55834323633351D0E041" On USB Serial Device(COM11)
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

        public ISerialPort GetSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits,
            Handshake handShake, bool dtrEnable, string newLine, int readTimeout, int writeTimeout) {
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
}