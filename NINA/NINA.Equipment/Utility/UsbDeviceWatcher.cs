using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Management;

namespace NINA.Equipment.Utility {
    public class UsbDeviceWatcher : IUsbDeviceWatcher {
        private ManagementEventWatcher _insertWatcher;
        private ManagementEventWatcher _removeWatcher;
        private Dictionary<string, UsbDeviceInfo> _currentDevices;

        public event EventHandler<UsbDeviceEventArgs> DeviceInserted;
        public event EventHandler<UsbDeviceEventArgs> DeviceRemoved;

        public UsbDeviceWatcher() {
        }

        public void Start() {
            try {
                _currentDevices = GetUsbDevices();
                var insertQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
                _insertWatcher = new ManagementEventWatcher(insertQuery);
                _insertWatcher.EventArrived += (s, e) => HandleDeviceInserted();

                var removeQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
                _removeWatcher = new ManagementEventWatcher(removeQuery);
                _removeWatcher.EventArrived += (s, e) => HandleDeviceRemoved();

                _insertWatcher.Start();
                _removeWatcher.Start();
            } catch (Exception ex) {
                Logger.Error("An error occurred while starting USB Device Watcher", ex);
            }
        }

        public void Stop() {
            try {
                _insertWatcher?.Stop();
                _removeWatcher?.Stop();
                _insertWatcher?.Dispose();
                _removeWatcher?.Dispose();
            } catch (Exception ex) {
                Logger.Error("An error occurred while stopping USB Device Watcher", ex);
            }
        }

        private void HandleDeviceInserted() {
            var newDevices = GetUsbDevices();
            foreach (var device in newDevices.Values) {
                if (!_currentDevices.ContainsKey(device.DeviceId)) {
                    DeviceInserted?.Invoke(this, new UsbDeviceEventArgs(device));
                }
            }
            _currentDevices = newDevices;
        }

        private void HandleDeviceRemoved() {
            var newDevices = GetUsbDevices();
            foreach (var device in _currentDevices.Values) {
                if (!newDevices.ContainsKey(device.DeviceId)) {
                    DeviceRemoved?.Invoke(this, new UsbDeviceEventArgs(device));
                }
            }
            _currentDevices = newDevices;
        }

        private Dictionary<string, UsbDeviceInfo> GetUsbDevices() {
            var devices = new Dictionary<string, UsbDeviceInfo>();
            try {
                using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity")) {
                    foreach (var device in searcher.Get()) {
                        string deviceId, pnpDeviceId, description, name, manufacturer, service, status;

                        try { deviceId = device["DeviceID"]?.ToString(); } catch { deviceId = "N/A"; }
                        try { pnpDeviceId = device["PNPDeviceID"]?.ToString(); } catch { pnpDeviceId = "N/A"; }
                        try { description = device["Description"]?.ToString(); } catch { description = "N/A"; }
                        try { name = device["Name"]?.ToString(); } catch { name = "N/A"; }
                        try { manufacturer = device["Manufacturer"]?.ToString(); } catch { manufacturer = "N/A"; }
                        try { service = device["Service"]?.ToString(); } catch { service = "N/A"; }
                        try { status = device["Status"]?.ToString(); } catch { status = "N/A"; }

                        if (deviceId != "N/A") {
                            if(deviceId.ToUpper().StartsWith("USB")) {
                                devices[deviceId] = new UsbDeviceInfo(deviceId, pnpDeviceId, description, name, manufacturer, service, status);
                            }                            
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error("An error occurred while retrieving USB device information", ex);
            }
            return devices;
        }

        public void Dispose() {
            Stop();
        }
    }
}