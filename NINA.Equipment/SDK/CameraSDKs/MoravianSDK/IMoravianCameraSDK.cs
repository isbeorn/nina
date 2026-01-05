using System;
using System.Collections.Generic;
using System.Text;

namespace MoravianCameraSDK {

    public interface IMoravianCameraSDK {
        UIntPtr Initialize(uint cameraId);
        void Release(UIntPtr handle);
        void RegisterNotifyHWND(UIntPtr handle, IntPtr hwnd);
        bool GetBooleanParameter(UIntPtr handle, MoravianBooleanParameter index, out bool value);
        bool GetIntegerParameter(UIntPtr handle, MoravianIntegerParameter index, out int value);
        bool GetStringParameter(UIntPtr handle, MoravianStringParameter index, int maxLen, StringBuilder sb);
        bool GetValue(UIntPtr handle, MoravianValueParameter index, out float value);
        bool EnumerateReadModes(UIntPtr handle, int index, int maxLen, StringBuilder sb);
        bool EnumerateFilters(UIntPtr handle, uint index, int maxLen, StringBuilder sb, out uint color);
        bool EnumerateFilters2(UIntPtr handle, uint index, int maxLen, StringBuilder sb, out uint color, out int offset);
        bool SetReadMode(UIntPtr handle, int mode);
        bool SetBinning(UIntPtr handle, uint x, uint y);
        void SetGain(UIntPtr handle, uint gain);
        bool SetFilter(UIntPtr handle, uint index);
        void SetTemperature(UIntPtr handle, float temperature);
        void SetTemperatureRamp(UIntPtr handle, float ramp);
        bool SetFan(UIntPtr handle, byte speed);
        bool SetWindowHeating(UIntPtr handle, bool on);
        bool SetPreflash(UIntPtr handle, double preflashTime, uint clearNum);
        bool MoveTelescope(UIntPtr handle, short raDurationMs, short decDurationMs);
        bool MoveInProgress(UIntPtr handle, out bool moving);
        void GetLastErrorString(UIntPtr handle, int maxLen, StringBuilder sb);
    }

    public interface IMoravianCameraTimingExposure {
        bool StartExposure(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d);
        bool StartExposureTrigger(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d);
        bool AbortExposure(UIntPtr handle, bool downloadFlag);
        bool ImageReady(UIntPtr handle, out bool ready);
        bool ReadImage(UIntPtr handle, uint bufferLen, ushort[] buffer);
        bool ReadImageExposure(UIntPtr handle, uint bufferLen, ushort[] buffer);

    }

    public interface IMoravianComputerTimingExposure {
        bool BeginExposure(UIntPtr handle, bool useShutter);
        bool EndExposure(UIntPtr handle, bool useShutter, bool abortData);
        bool GetImage(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);
        bool GetImage8b(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);
        bool GetImage16b(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);
        bool GetImageExposure(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);
        bool GetImageExposure8b(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);
        bool GetImageExposure16b(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);
    }

    public interface IMoravianGPS {
        bool GetImageTimeStamp(UIntPtr handle, out int year, out int month, out int day, out int hour, out int minute, out double second);
        bool GetGPSData(UIntPtr handle,
            out double lat, out double lon, out double msl,
            out int year, out int month, out int day, out int hour, out int minute, out double second,
            out uint satellites, out bool fix);
    }

    public interface IMoravianConfigurable {
        void Configure(UIntPtr handle, IntPtr parentHwnd);
    }

    public interface IMoravianManualTimingExposure {
        bool ClearSensor(UIntPtr handle);
        bool OpenShutter(UIntPtr handle);
        bool CloseShutter(UIntPtr handle);
    }

    public enum MoravianBooleanParameter : uint {
        gbpConnected = 0,
        gbpSubFrame = 1,
        gbpReadModes = 2,
        gbpShutter = 3,
        gbpCooler = 4,
        gbpFan = 5,
        gbpFilters = 6,
        gbpGuide = 7,
        gbpWindowHeating = 8,
        gbpPreflash = 9,
        gbpAsymmetricBinning = 10,
        gbpMicrometerFilterOffsets = 11,
        gbpPowerUtilization = 12,
        gbpGain = 13,
        gbpElectronicShutter = 14,
        gbpGPS = 16,
        gbpContinuousExposures = 17,
        gbpTrigger = 18,
        gbpConfigured = 127,
        gbpRGB = 128,
        gbpCMY = 129,
        gbpCMYG = 130,
        gbpDebayerXOdd = 131,
        gbpDebayerYOdd = 132,
        gbpInterlaced = 256
    }

    public enum MoravianIntegerParameter : uint {
        gipCameraId = 0,
        gipChipW = 1,
        gipChipD = 2,
        gipPixelW = 3,
        gipPixelD = 4,
        gipMaxBinningX = 5,
        gipMaxBinningY = 6,
        gipReadModes = 7,
        gipFilters = 8,
        gipMinimalExposure = 9,
        gipMaximalExposure = 10,
        gipMaximalMoveTime = 11,
        gipDefaultReadMode = 12,
        gipPreviewReadMode = 13,
        gipMaxWindowHeating = 14,
        gipMaxFan = 15,
        gipMaxGain = 16,
        gipMaxPossiblePixelValue = 17,
        gipLineTime = 18,
        gipBiasPixelValue = 19,
        gipFirmwareMajor = 128,
        gipFirmwareMinor = 129,
        gipFirmwareBuild = 130,
        gipDriverMajor = 131,
        gipDriverMinor = 132,
        gipDriverBuild = 133,
        gipFlashMajor = 134,
        gipFlashMinor = 135,
        gipFlashBuild = 136
    }

    public enum MoravianStringParameter : uint {
        gspCameraDescription = 0,
        gspManufacturer = 1,
        gspCameraSerial = 2,
        gspChipDescription = 3
    }

    public enum MoravianValueParameter : uint {
        gvChipTemperature = 0,
        gvHotTemperature = 1,
        gvCameraTemperature = 2,
        gvEnvironmentTemperature = 3,
        gvSupplyVoltage = 10,
        gvPowerUtilization = 11,
        gvADCGain = 20
    }
}