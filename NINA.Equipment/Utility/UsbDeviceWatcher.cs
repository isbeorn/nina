using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Utility {
    public class UsbDeviceWatcher : IUsbDeviceWatcher {
        private Dictionary<string, UsbDeviceInfo> _currentDevices;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitoringTask;
        private const int POLL_INTERVAL_MS = 1000; // Check for device changes every second

        public event EventHandler<UsbDeviceEventArgs> DeviceInserted;
        public event EventHandler<UsbDeviceEventArgs> DeviceRemoved;

        public UsbDeviceWatcher() {
        }

        public void Start() {
            try {
                _currentDevices = GetUsbDevices();
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Start monitoring task
                _monitoringTask = Task.Run(() => MonitorDeviceChanges(_cancellationTokenSource.Token));
                
                Logger.Info("USB Device Watcher started");
            } catch (Exception ex) {
                Logger.Error("An error occurred while starting USB Device Watcher", ex);
            }
        }

        public void Stop() {
            try {
                _cancellationTokenSource?.Cancel();
                _monitoringTask?.Wait(TimeSpan.FromSeconds(2));
                _cancellationTokenSource?.Dispose();
                Logger.Info("USB Device Watcher stopped");
            } catch (Exception ex) {
                Logger.Error("An error occurred while stopping USB Device Watcher", ex);
            }
        }

        private async Task MonitorDeviceChanges(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    await Task.Delay(POLL_INTERVAL_MS, cancellationToken);
                    
                    var newDevices = GetUsbDevices();
                    
                    // Check for inserted devices
                    foreach (var device in newDevices.Values) {
                        if (!_currentDevices.ContainsKey(device.DeviceId)) {
                            Logger.Info($"USB device inserted: {device.Name}");
                            DeviceInserted?.Invoke(this, new UsbDeviceEventArgs(device));
                        }
                    }
                    
                    // Check for removed devices
                    foreach (var device in _currentDevices.Values) {
                        if (!newDevices.ContainsKey(device.DeviceId)) {
                            Logger.Info($"USB device removed: {device.Name}");
                            DeviceRemoved?.Invoke(this, new UsbDeviceEventArgs(device));
                        }
                    }
                    
                    _currentDevices = newDevices;
                } catch (TaskCanceledException) {
                    // Expected when stopping
                    break;
                } catch (Exception ex) {
                    Logger.Error("Error monitoring USB devices", ex);
                }
            }
        }

        private Dictionary<string, UsbDeviceInfo> GetUsbDevices() {
            var devices = new Dictionary<string, UsbDeviceInfo>();
            try {
                // On Linux, enumerate USB devices from /sys/bus/usb/devices
                var usbDevicesPath = "/sys/bus/usb/devices";
                
                if (!Directory.Exists(usbDevicesPath)) {
                    Logger.Warning($"USB devices path not found: {usbDevicesPath}");
                    return devices;
                }

                var deviceDirs = Directory.GetDirectories(usbDevicesPath)
                    .Where(d => {
                        var name = Path.GetFileName(d);
                        // Filter for actual USB devices (e.g., 1-1, 1-1.1, etc.) not root hubs
                        return name.Contains('-') && !name.Contains(':');
                    });

                foreach (var deviceDir in deviceDirs) {
                    try {
                        var deviceId = Path.GetFileName(deviceDir);
                        
                        // Read device attributes from sysfs
                        var idVendorPath = Path.Combine(deviceDir, "idVendor");
                        var idProductPath = Path.Combine(deviceDir, "idProduct");
                        var manufacturerPath = Path.Combine(deviceDir, "manufacturer");
                        var productPath = Path.Combine(deviceDir, "product");
                        var serialPath = Path.Combine(deviceDir, "serial");
                        
                        // Check if this is a valid USB device (has idVendor and idProduct)
                        if (!File.Exists(idVendorPath) || !File.Exists(idProductPath)) {
                            continue;
                        }
                        
                        var idVendor = ReadSysfsFile(idVendorPath);
                        var idProduct = ReadSysfsFile(idProductPath);
                        var manufacturer = ReadSysfsFile(manufacturerPath);
                        var product = ReadSysfsFile(productPath);
                        var serial = ReadSysfsFile(serialPath);
                        
                        // Create a unique device ID similar to Windows format
                        var pnpDeviceId = $"USB\\VID_{idVendor}&PID_{idProduct}";
                        if (!string.IsNullOrEmpty(serial)) {
                            pnpDeviceId += $"\\{serial}";
                        }
                        
                        var name = !string.IsNullOrEmpty(product) ? product : $"USB Device {idVendor}:{idProduct}";
                        var description = name;
                        var status = "OK"; // On Linux, if device exists in sysfs, it's working
                        
                        devices[deviceId] = new UsbDeviceInfo(
                            deviceId,
                            pnpDeviceId,
                            description,
                            name,
                            manufacturer ?? "N/A",
                            "N/A", // service not applicable on Linux
                            status
                        );
                    } catch (Exception ex) {
                        Logger.Debug($"Error reading device info from {deviceDir}: {ex.Message}");
                    }
                }
            } catch (Exception ex) {
                Logger.Error("An error occurred while retrieving USB device information", ex);
            }
            return devices;
        }

        private string ReadSysfsFile(string path) {
            try {
                if (File.Exists(path)) {
                    return File.ReadAllText(path).Trim();
                }
            } catch {
                // Ignore read errors
            }
            return string.Empty;
        }

        public void Dispose() {
            Stop();
        }
    }
}
