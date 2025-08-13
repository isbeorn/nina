using NINA.Equipment.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces {
    public interface IUsbDeviceWatcher : IDisposable {
        void Start();
        void Stop();
        event EventHandler<UsbDeviceEventArgs> DeviceInserted;
        event EventHandler<UsbDeviceEventArgs> DeviceRemoved;
    }

    public class UsbDeviceInfo {
        public UsbDeviceInfo(string deviceId, string pnpDeviceId, string description, string name, string manufacturer, string service, string status) {
            DeviceId = deviceId;
            PnpDeviceId = pnpDeviceId;
            Description = description;
            Name = name;
            Manufacturer = manufacturer;
            Service = service;
            Status = status;
        }

        public string DeviceId { get; }
        public string PnpDeviceId { get; }
        public string Description { get; }
        public string Name { get; }
        public string Manufacturer { get; }
        public string Service { get; }
        public string Status { get; }

        public override string ToString() {
            return $"DeviceId: {DeviceId}, PnpDeviceId: {PnpDeviceId}, Description: {Description}, Name: {Name}, Manufacturer: {Manufacturer}, Service: {Service}, Status: {Status}";
        }
    }

    public class UsbDeviceEventArgs : EventArgs {
        public UsbDeviceInfo DeviceInfo { get; }
        public UsbDeviceEventArgs(UsbDeviceInfo deviceInfo) {
            DeviceInfo = deviceInfo;
        }
    }
}
