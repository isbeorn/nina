#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;

namespace NINA.Core.Utility.SerialCommunication {

    public class SerialPortProvider : ISerialPortProvider {
        /**
         * Below are the device IDs that are currently known
         * Arduino Uno R3:                     DeviceID = USB VID:PID=2341:0043
         * Arduino Leonardo:                   DeviceID = USB VID:PID=2341:8036
         * Optec USB/Serial cable:             DeviceID = USB VID:PID=0403:6001
         * Flat Fielder:                       DeviceID = USB VID:PID=0403:6001
         * Pegasus Astro Ultimate Powerbox V2: DeviceID = USB VID:PID=0403:6015
         * 
         * On Linux, serial ports are typically:
         * - /dev/ttyUSB* for USB-to-serial adapters (FTDI, etc.)
         * - /dev/ttyACM* for CDC ACM devices (Arduino, etc.)
         * - /dev/ttyS* for built-in serial ports
         **/

        private readonly Dictionary<string, bool> dtrEnableValue;

        public SerialPortProvider() {
            dtrEnableValue = new Dictionary<string, bool>();
            
            // On Linux, detect Arduino Leonardo devices that need DTR enabled
            // Leonardo has VID:PID = 2341:8036
            try {
                var ports = EnumerateSerialPortsWithDeviceInfo();
                foreach (var (portName, vendorId, productId) in ports) {
                    // Arduino Leonardo needs DTR enabled
                    bool needsDtr = (vendorId == "2341" && productId == "8036");
                    if (!dtrEnableValue.ContainsKey(portName)) {
                        dtrEnableValue.Add(portName, needsDtr);
                        Logger.Debug($"Found serial port {portName} (VID:PID={vendorId}:{productId}, DTR={needsDtr})");
                    }
                }
            } catch (Exception ex) {
                Logger.Debug($"Error enumerating serial ports with device info: {ex.Message}");
            }
        }

        private List<(string portName, string vendorId, string productId)> EnumerateSerialPortsWithDeviceInfo() {
            var result = new List<(string, string, string)>();
            
            // On Linux, get device info from /sys/class/tty/*/device
            var ttyClassPath = "/sys/class/tty";
            if (!Directory.Exists(ttyClassPath)) {
                return result;
            }

            foreach (var ttyDir in Directory.GetDirectories(ttyClassPath)) {
                var ttyName = Path.GetFileName(ttyDir);
                
                // Only consider USB serial ports and ACM devices
                if (!ttyName.StartsWith("ttyUSB") && !ttyName.StartsWith("ttyACM")) {
                    continue;
                }
                
                var portName = $"/dev/{ttyName}";
                
                // Try to read USB vendor and product IDs
                var devicePath = Path.Combine(ttyDir, "device");
                if (Directory.Exists(devicePath)) {
                    // Navigate up to find the USB device
                    var usbDevicePath = FindUsbDeviceParent(devicePath);
                    if (usbDevicePath != null) {
                        var vendorId = ReadSysfsFile(Path.Combine(usbDevicePath, "idVendor"));
                        var productId = ReadSysfsFile(Path.Combine(usbDevicePath, "idProduct"));
                        result.Add((portName, vendorId, productId));
                    } else {
                        result.Add((portName, "", ""));
                    }
                } else {
                    result.Add((portName, "", ""));
                }
            }
            
            return result;
        }

        private string FindUsbDeviceParent(string devicePath) {
            // Walk up the directory tree to find the USB device
            var currentPath = devicePath;
            for (int i = 0; i < 5; i++) { // Limit depth to prevent infinite loops
                if (File.Exists(Path.Combine(currentPath, "idVendor")) && 
                    File.Exists(Path.Combine(currentPath, "idProduct"))) {
                    return currentPath;
                }
                
                var parentPath = Path.GetDirectoryName(currentPath);
                if (parentPath == currentPath || string.IsNullOrEmpty(parentPath)) {
                    break;
                }
                currentPath = parentPath;
            }
            return null;
        }

        private string ReadSysfsFile(string path) {
            try {
                if (File.Exists(path)) {
                    return File.ReadAllText(path).Trim();
                }
            } catch {
                // Ignore errors
            }
            return string.Empty;
        }

        public ISerialPort GetSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits,
            Handshake handShake, bool dtrEnable, string newLine, int readTimeout, int writeTimeout) {
            if (string.IsNullOrEmpty(portName)) return null;
            dtrEnableValue.TryGetValue(portName, out var dtrEnableForLeonardo);
            var dtr = dtrEnable || dtrEnableForLeonardo;
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
                // On Linux, enumerate serial ports from /dev
                var deviceSpecificPorts = new List<string>();
                
                if (deviceQuery != null) {
                    // Parse device query if it contains VID/PID filter
                    // Example: "USB\\VID_0403&PID_6001" -> filter by VID=0403, PID=6001
                    var vidMatch = Regex.Match(deviceQuery, @"VID_([0-9A-Fa-f]{4})");
                    var pidMatch = Regex.Match(deviceQuery, @"PID_([0-9A-Fa-f]{4})");
                    
                    if (vidMatch.Success || pidMatch.Success) {
                        var targetVid = vidMatch.Success ? vidMatch.Groups[1].Value.ToLower() : null;
                        var targetPid = pidMatch.Success ? pidMatch.Groups[1].Value.ToLower() : null;
                        
                        var portsWithInfo = EnumerateSerialPortsWithDeviceInfo();
                        foreach (var (portName, vendorId, productId) in portsWithInfo) {
                            bool matches = true;
                            if (targetVid != null && vendorId.ToLower() != targetVid) matches = false;
                            if (targetPid != null && productId.ToLower() != targetPid) matches = false;
                            
                            if (matches) {
                                deviceSpecificPorts.Add(portName);
                            }
                        }
                    }
                    
                    result.AddRange(deviceSpecificPorts.OrderBy(s => s));
                }
                
                if (addDivider && deviceSpecificPorts.Any()) { 
                    result.Add("----"); 
                }
                
                if (addGenericPorts) {
                    // Get all available serial ports
                    var allPorts = SerialPort.GetPortNames().OrderBy(s => s);
                    foreach (var portName in allPorts) {
                        if (!result.Contains(portName)) {
                            result.Add(portName);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                result = SerialPort.GetPortNames().OrderBy(s => s).ToList();
            }
            return new ReadOnlyCollection<string>(result);
        }
    }
}
