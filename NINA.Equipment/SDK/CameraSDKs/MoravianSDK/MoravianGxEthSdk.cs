using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MoravianCameraSDK {
    public class MoravianGxEthSdk : IMoravianCameraSDK, IMoravianCameraTimingExposure, IMoravianGPS, IMoravianConfigurable {
        static MoravianGxEthSdk() {
            DllLoader.LoadDll(Path.Combine("Moravian", "gXeth.dll"));
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
            Gxeth.Enumerate(CallBack);
            return list;
        }

        public UIntPtr Initialize(uint cameraId) {
            using var scope = global.EnterScope();
            return Gxeth.Initialize(cameraId);
        }

        public void Release(UIntPtr handle) {
            using var scope = global.EnterScope();
            Gxeth.Release(handle);
        }

        public void RegisterNotifyHWND(UIntPtr handle, IntPtr hwnd) {
            using var scope = local.EnterScope();
            Gxeth.RegisterNotifyHWND(handle, hwnd);
        }

        public bool GetBooleanParameter(UIntPtr handle, MoravianBooleanParameter index, out bool value) {
            using var scope = local.EnterScope();
            return Gxeth.GetBooleanParameter(handle, (uint)index, out value);
        }

        public bool GetIntegerParameter(UIntPtr handle, MoravianIntegerParameter index, out int value) {
            using var scope = local.EnterScope();
            return Gxeth.GetIntegerParameter(handle, (uint)index, out value);
        }

        public bool GetStringParameter(UIntPtr handle, MoravianStringParameter index, int maxLen, StringBuilder sb) {
            using var scope = local.EnterScope();
            return Gxeth.GetStringParameter(handle, (uint)index, maxLen, sb);
        }

        public bool GetValue(UIntPtr handle, MoravianValueParameter index, out float value) {
            using var scope = local.EnterScope();
            return Gxeth.GetValue(handle, (uint)index, out value);
        }

        public bool EnumerateReadModes(UIntPtr handle, int index, int maxLen, StringBuilder sb) {
            using var scope = local.EnterScope();
            return Gxeth.EnumerateReadModes(handle, index, maxLen, sb);
        }

        public bool EnumerateFilters(UIntPtr handle, uint index, int maxLen, StringBuilder sb, out uint color) {
            using var scope = local.EnterScope();
            return Gxeth.EnumerateFilters(handle, index, maxLen, sb, out color);
        }

        public bool EnumerateFilters2(UIntPtr handle, uint index, int maxLen, StringBuilder sb, out uint color, out int offset) {
            using var scope = local.EnterScope();
            return Gxeth.EnumerateFilters2(handle, index, maxLen, sb, out color, out offset);
        }

        public bool SetReadMode(UIntPtr handle, int mode) {
            using var scope = local.EnterScope();
            return Gxeth.SetReadMode(handle, mode);
        }

        public bool SetBinning(UIntPtr handle, uint x, uint y) {
            using var scope = local.EnterScope();
            return Gxeth.SetBinning(handle, x, y);
        }

        public void SetGain(UIntPtr handle, uint gain) {
            using var scope = local.EnterScope();
            Gxeth.SetGain(handle, gain);
        }

        public bool SetFilter(UIntPtr handle, uint index) {
            using var scope = local.EnterScope();
            return Gxeth.SetFilter(handle, index);
        }

        public bool ReinitFilterWheel(UIntPtr handle) {
            using var scope = local.EnterScope();
            return Gxeth.ReinitFilterWheel(handle);
        }

        public void SetTemperature(UIntPtr handle, float temperature) {
            using var scope = local.EnterScope();
            Gxeth.SetTemperature(handle, temperature);
        }

        public void SetTemperatureRamp(UIntPtr handle, float ramp) {
            using var scope = local.EnterScope();
            Gxeth.SetTemperatureRamp(handle, ramp);
        }

        public bool SetFan(UIntPtr handle, byte speed) {
            using var scope = local.EnterScope();
            return Gxeth.SetFan(handle, speed);
        }

        public bool SetWindowHeating(UIntPtr handle, bool on) {
            using var scope = local.EnterScope();
            return Gxeth.SetWindowHeating(handle, on);
        }

        public bool SetPreflash(UIntPtr handle, double preflashTime, uint clearNum) {
            using var scope = local.EnterScope();
            return Gxeth.SetPreflash(handle, preflashTime, clearNum);
        }

        public bool MoveTelescope(UIntPtr handle, short raDurationMs, short decDurationMs) {
            using var scope = local.EnterScope();
            return Gxeth.MoveTelescope(handle, raDurationMs, decDurationMs);
        }

        public bool MoveInProgress(UIntPtr handle, out bool moving) {
            using var scope = local.EnterScope();
            return Gxeth.MoveInProgress(handle, out moving);
        }

        public void GetLastErrorString(UIntPtr handle, int maxLen, StringBuilder sb) {
            using var scope = local.EnterScope();
            Gxeth.GetLastErrorString(handle, maxLen, sb);
        }

        public bool OpenShutter(UIntPtr handle) {
            using var scope = local.EnterScope();
            return Gxeth.Open(handle);
        }

        public bool CloseShutter(UIntPtr handle) {
            using var scope = local.EnterScope();
            return Gxeth.Close(handle);
        }

        public bool StartExposure(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d) {
            using var scope = local.EnterScope();
            return Gxeth.StartExposure(handle, expTime, useShutter, x, y, w, d);
        }

        public bool StartExposureTrigger(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d) {
            using var scope = local.EnterScope();
            return Gxeth.StartExposureTrigger(handle, expTime, useShutter, x, y, w, d);
        }

        public bool AbortExposure(UIntPtr handle, bool downloadFlag) {
            using var scope = local.EnterScope();
            return Gxeth.AbortExposure(handle, downloadFlag);
        }

        public bool ImageReady(UIntPtr handle, out bool ready) {
            using var scope = local.EnterScope();
            return Gxeth.ImageReady(handle, out ready);
        }

        public bool ReadImage(UIntPtr handle, uint bufferLen, ushort[] buffer) {
            using var scope = local.EnterScope();
            return Gxeth.ReadImage(handle, bufferLen, buffer);
        }

        public bool ReadImageExposure(UIntPtr handle, uint bufferLen, ushort[] buffer) {
            using var scope = local.EnterScope();
            return Gxeth.ReadImageExposure(handle, bufferLen, buffer);
        }

        public bool GetImageTimeStamp(nuint handle, out int year, out int month, out int day, out int hour, out int minute, out double second) {
            using var scope = local.EnterScope();
            return Gxeth.GetImageTimeStamp(handle, out year, out month, out day, out hour, out minute, out second);
        }

        public bool GetGPSData(nuint handle, out double lat, out double lon, out double msl, out int year, out int month, out int day, out int hour, out int minute, out double second, out uint satellites, out bool fix) {
            using var scope = local.EnterScope();
            return Gxeth.GetGPSData(handle, out lat, out lon, out msl, out year, out month, out day, out hour, out minute, out second, out satellites, out fix);
        }

        public void Configure(nuint handle, nint parentHwnd) {
            using var scope = local.EnterScope();
            Gxeth.Configure(handle, parentHwnd);
        }
    }
}
