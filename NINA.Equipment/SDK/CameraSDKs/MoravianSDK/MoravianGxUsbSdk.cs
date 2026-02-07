using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MoravianCameraSDK {
    public class MoravianGxUsbSdk : IMoravianCameraSDK, IMoravianComputerTimingExposure, IMoravianManualTimingExposure {
        static MoravianGxUsbSdk() {
            DllLoader.LoadDll(Path.Combine("Moravian", "gXusb.dll"));
        }

        private static readonly Lock global = new();
        private readonly Lock local = new();

        public static List<uint> Scan() {
            using var scope = global.EnterScope();
            var list = new List<uint>();
            void CallBack(uint cameraId) {
                if (cameraId == 0) return;
                list.Add(cameraId);
            }
            Gxusb.Enumerate(CallBack);
            return list;
        }

        public UIntPtr Initialize(uint cameraId) {
            using var scope = global.EnterScope();
            return Gxusb.Initialize(cameraId);
        }

        public void Release(UIntPtr handle) {
            using var scope = global.EnterScope();
            Gxusb.Release(handle);
        }

        public void RegisterNotifyHWND(UIntPtr handle, IntPtr hwnd) {
            using var scope = local.EnterScope();
            Gxusb.RegisterNotifyHWND(handle, hwnd);
        }

        public bool GetBooleanParameter(UIntPtr handle, MoravianBooleanParameter index, out bool value) {
            using var scope = local.EnterScope();
            return Gxusb.GetBooleanParameter(handle, (uint)index, out value);
        }

        public bool GetIntegerParameter(UIntPtr handle, MoravianIntegerParameter index, out int value) {
            using var scope = local.EnterScope();
            return Gxusb.GetIntegerParameter(handle, (uint)index, out value);
        }

        public bool GetStringParameter(UIntPtr handle, MoravianStringParameter index, int maxLen, StringBuilder sb) {
            using var scope = local.EnterScope();
            return Gxusb.GetStringParameter(handle, (uint)index, maxLen, sb);
        }

        public bool GetValue(UIntPtr handle, MoravianValueParameter index, out float value) {
            using var scope = local.EnterScope();
            return Gxusb.GetValue(handle, (uint)index, out value);
        }

        public bool EnumerateReadModes(UIntPtr handle, int index, int maxLen, StringBuilder sb) {
            using var scope = local.EnterScope();
            return Gxusb.EnumerateReadModes(handle, index, maxLen, sb);
        }

        public bool EnumerateFilters(UIntPtr handle, uint index, int maxLen, StringBuilder sb, out uint color) {
            using var scope = local.EnterScope();
            return Gxusb.EnumerateFilters(handle, index, maxLen, sb, out color);
        }

        public bool EnumerateFilters2(UIntPtr handle, uint index, int maxLen, StringBuilder sb, out uint color, out int offset) {
            using var scope = local.EnterScope();
            return Gxusb.EnumerateFilters2(handle, index, maxLen, sb, out color, out offset);
        }

        public bool SetReadMode(UIntPtr handle, int mode) {
            using var scope = local.EnterScope();
            return Gxusb.SetReadMode(handle, mode);
        }

        public bool SetBinning(UIntPtr handle, uint x, uint y) {
            using var scope = local.EnterScope();
            return Gxusb.SetBinning(handle, x, y);
        }

        public void SetGain(UIntPtr handle, uint gain) {
            using var scope = local.EnterScope();
            Gxusb.SetGain(handle, gain);
        }

        public bool SetFilter(UIntPtr handle, uint index) {
            using var scope = local.EnterScope();
            return Gxusb.SetFilter(handle, index);
        }

        public bool ReinitFilterWheel(UIntPtr handle) {
            using var scope = local.EnterScope();
            return Gxusb.ReinitFilterWheel(handle);
        }

        public void SetTemperature(UIntPtr handle, float temperature) {
            using var scope = local.EnterScope();
            Gxusb.SetTemperature(handle, temperature);
        }

        public void SetTemperatureRamp(UIntPtr handle, float ramp) {
            using var scope = local.EnterScope();
            Gxusb.SetTemperatureRamp(handle, ramp);
        }

        public bool SetFan(UIntPtr handle, byte speed) {
            using var scope = local.EnterScope();
            return Gxusb.SetFan(handle, speed);
        }

        public bool SetWindowHeating(UIntPtr handle, bool on) {
            using var scope = local.EnterScope();
            return Gxusb.SetWindowHeating(handle, on);
        }

        public bool SetPreflash(UIntPtr handle, double preflashTime, uint clearNum) {
            using var scope = local.EnterScope();
            return Gxusb.SetPreflash(handle, preflashTime, clearNum);
        }

        public bool MoveTelescope(UIntPtr handle, short raDurationMs, short decDurationMs) {
            using var scope = local.EnterScope();
            return Gxusb.MoveTelescope(handle, raDurationMs, decDurationMs);
        }

        public bool MoveInProgress(UIntPtr handle, out bool moving) {
            using var scope = local.EnterScope();
            return Gxusb.MoveInProgress(handle, out moving);
        }

        public void GetLastErrorString(UIntPtr handle, int maxLen, StringBuilder sb) {
            using var scope = local.EnterScope();
            Gxusb.GetLastErrorString(handle, maxLen, sb);
        }

        public bool OpenShutter(UIntPtr handle) {
            using var scope = local.EnterScope();
            return Gxusb.Open(handle);
        }

        public bool CloseShutter(UIntPtr handle) {
            using var scope = local.EnterScope();
            return Gxusb.Close(handle);
        }

        public bool ClearSensor(UIntPtr handle) {
            using var scope = local.EnterScope();
            return Gxusb.ClearSensor(handle);
        }

        public bool BeginExposure(UIntPtr handle, bool useShutter) {
            using var scope = local.EnterScope();
            return Gxusb.BeginExposure(handle, useShutter);
        }

        public bool EndExposure(UIntPtr handle, bool useShutter, bool abortData) {
            using var scope = local.EnterScope();
            return Gxusb.EndExposure(handle, useShutter, abortData);
        }

        public bool GetImage(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer) {
            using var scope = local.EnterScope();
            return Gxusb.GetImage(handle, x, y, w, d, bufferLen, buffer);
        }

        public bool GetImage8b(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer) {
            using var scope = local.EnterScope();
            return Gxusb.GetImage8b(handle, x, y, w, d, bufferLen, buffer);
        }

        public bool GetImage16b(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer) {
            using var scope = local.EnterScope();
            return Gxusb.GetImage16b(handle, x, y, w, d, bufferLen, buffer);
        }

        public bool GetImageExposure(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer) {
            using var scope = local.EnterScope();
            return Gxusb.GetImageExposure(handle, expTime, useShutter, x, y, w, d, bufferLen, buffer);
        }

        public bool GetImageExposure8b(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer) {
            using var scope = local.EnterScope();
            return Gxusb.GetImageExposure8b(handle, expTime, useShutter, x, y, w, d, bufferLen, buffer);
        }

        public bool GetImageExposure16b(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer) {
            using var scope = local.EnterScope();
            return Gxusb.GetImageExposure16b(handle, expTime, useShutter, x, y, w, d, bufferLen, buffer);
        }
    }
}
