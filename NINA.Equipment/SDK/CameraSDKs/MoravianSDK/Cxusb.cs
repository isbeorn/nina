using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MoravianCameraSDK
{
    static class Cxusb
    {
        #region GetBooleanParameter Indexes

        public const uint gbpConnected = 0;
        public const uint gbpSubFrame = 1;
        public const uint gbpReadModes = 2;
        public const uint gbpShutter = 3;
        public const uint gbpCooler = 4;
        public const uint gbpFan = 5;
        public const uint gbpFilters = 6;
        public const uint gbpGuide = 7;
        public const uint gbpWindowHeating = 8;
        public const uint gbpPreflash = 9;
        public const uint gbpAsymmetricBinning = 10;
        public const uint gbpMicrometerFilterOffsets = 11;
        public const uint gbpPowerUtilization = 12;
        public const uint gbpGain = 13;
        public const uint gbpElectronicShutter = 14;
        public const uint gbpGPS = 16;
        public const uint gbpContinuousExposures = 17;
        public const uint gbpTrigger = 18;
        public const uint gbpConfigured = 127;
        public const uint gbpRGB = 128;
        public const uint gbpCMY = 129;
        public const uint gbpCMYG = 130;
        public const uint gbpDebayerXOdd = 131;
        public const uint gbpDebayerYOdd = 132;
        public const uint gbpInterlaced = 256;

        #endregion

        #region GetIntegerParameter Indexes

        public const uint gipCameraId = 0;
        public const uint gipChipW = 1;
        public const uint gipChipD = 2;
        public const uint gipPixelW = 3;
        public const uint gipPixelD = 4;
        public const uint gipMaxBinningX = 5;
        public const uint gipMaxBinningY = 6;
        public const uint gipReadModes = 7;
        public const uint gipFilters = 8;
        public const uint gipMinimalExposure = 9;
        public const uint gipMaximalExposure = 10;
        public const uint gipMaximalMoveTime = 11;
        public const uint gipDefaultReadMode = 12;
        public const uint gipPreviewReadMode = 13;
        public const uint gipMaxWindowHeating = 14;
        public const uint gipMaxFan = 15;
        public const uint gipMaxGain = 16;
        public const uint gipMaxPossiblePixelValue = 17;
        public const uint gipFirmwareMajor = 128;
        public const uint gipFirmwareMinor = 129;
        public const uint gipFirmwareBuild = 130;
        public const uint gipDriverMajor = 131;
        public const uint gipDriverMinor = 132;
        public const uint gipDriverBuild = 133;
        public const uint gipFlashMajor = 134;
        public const uint gipFlashMinor = 135;
        public const uint gipFlashBuild = 136;

        #endregion

        #region GetStringParameter Indexes

        public const uint gspCameraDescription = 0;
        public const uint gspManufacturer = 1;
        public const uint gspCameraSerial = 2;
        public const uint gspChipDescription = 3;

        #endregion

        #region GetValue Indexes

        public const uint gvChipTemperature = 0;
        public const uint gvHotTemperature = 1;
        public const uint gvCameraTemperature = 2;
        public const uint gvEnvironmentTemperature = 3;
        public const uint gvSupplyVoltage = 10;
        public const uint gvPowerUtilization = 11;
        public const uint gvADCGain = 20;

        #endregion

        const string CameraDriverDllName = "cxusb.dll";

        #region Camera Enumeration / Connection

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EnumCallBack(uint CameraId);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Enumerate(EnumCallBack cb);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr Initialize(uint CameraId);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Release(UIntPtr Handle);

        #endregion

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterNotifyHWND(UIntPtr Handle, IntPtr HWND);

        #region Getting Values

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool GetBooleanParameter(UIntPtr Handle, uint Index, out bool Value);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool GetIntegerParameter(UIntPtr Handle, uint Index, out int Value);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool GetStringParameter(UIntPtr Handle, uint Index, int String_HIGH, StringBuilder String);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool GetValue(UIntPtr Handle, uint Index, out float Value);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool EnumerateReadModes(UIntPtr Handle, int Index, int String_HIGH, StringBuilder String);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool EnumerateFilters(UIntPtr Handle, uint Index, int String_HIGH, StringBuilder String, out uint Color);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool EnumerateFilters2(UIntPtr Handle, uint Index, int String_HIGH, StringBuilder String, out uint Color, out int Offset);

        #endregion

        #region Setting Values

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool SetReadMode(UIntPtr Handle, int Mode);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool SetBinning(UIntPtr Handle, uint x, uint y);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern void SetGain(UIntPtr Handle, uint gain);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool SetFilter(UIntPtr Handle, uint Index);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool ReinitFilterWheel(UIntPtr Handle);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetTemperature(UIntPtr Handle, float Temperature);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetTemperatureRamp(UIntPtr Handle, float TemperatureRamp);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SetFan(UIntPtr Handle, byte Speed);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool SetWindowHeating(UIntPtr Handle, bool On);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool SetPreflash(UIntPtr Handle, double PreflashTime, uint ClearNum);

        #endregion

        #region Image Handling

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool Open(UIntPtr Handle);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool Close(UIntPtr Handle);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool StartExposure(UIntPtr Handle, double ExpTime, bool UseShutter, int x, int y, int w, int d);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool StartExposureTrigger(UIntPtr Handle, double ExpTime, bool UseShutter, int x, int y, int w, int d);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool AbortExposure(UIntPtr Handle, bool DownloadFlag);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool ImageReady(UIntPtr Handle, out bool Ready);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool ReadImage(UIntPtr Handle, uint BufferLen, [Out] ushort[] BufferAdr);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool ReadImageExposure(UIntPtr Handle, uint BufferLen, [Out] ushort[] BufferAdr);

        #endregion

        #region Misc.

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool MoveTelescope(UIntPtr Handle, short RADurationMs, short DecDurationMs);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool MoveInProgress(UIntPtr Handle, out bool Moving);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool GetImageTimeStamp(UIntPtr Handle, out int Year, out int Month, out int Day, out int Hour, out int Minute, out double Second);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)] // BOOLEAN (unsigned char) return
        public static extern bool GetGPSData(UIntPtr Handle, out double Lat, out double Lon, out double MSL, out int Year, out int Month, out int Day, out int Hour, out int Minute, out double Second, out uint Satellites, out bool Fix);

        [DllImport(CameraDriverDllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void GetLastErrorString(UIntPtr Handle, int String_HIGH, StringBuilder String);

        #endregion
    }
}
