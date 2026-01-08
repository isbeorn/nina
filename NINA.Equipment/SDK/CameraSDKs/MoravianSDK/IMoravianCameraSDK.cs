using System;
using System.Collections.Generic;
using System.Text;

namespace MoravianCameraSDK {

    public interface IMoravianCameraSDK {

        /// <summary>
        /// Initializes a camera instance and returns a handle identifying the driver instance.
        /// The driver is designed to handle multiple cameras at once; the handle can represent
        /// a pointer-sized value (32/64-bit).
        /// </summary>
        /// <param name="cameraId">
        /// Camera identifier as returned by Enumerate (or otherwise known to the application).
        /// </param>
        /// <returns>
        /// Handle to the camera instance. If initialization fails because the camera is not connected
        /// or already in use by another application, the driver returns INVALID_HANDLE_VALUE
        /// </returns>
        UIntPtr Initialize(uint cameraId);

        /// <summary>
        /// Releases the camera handle. No other function (except Enumerate/Initialize) may be called
        /// after Release. Calling Release also unregisters notifications (equivalent to RegisterNotifyHWND(NULL)).
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        void Release(UIntPtr handle);

        /// <summary>
        /// Registers a window handle (HWND) to receive camera connect/disconnect notifications
        /// as Windows messages. Passing NULL disables notifications. Calling Release disables notifications.
        /// Notification messages are WM_CAMERA_CONNECT (1034) and WM_CAMERA_DISCONNECT (1035).
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="hwnd">Window handle to receive notifications, or IntPtr.Zero to unregister.</param>
        void RegisterNotifyHWND(UIntPtr handle, IntPtr hwnd);

        /// <summary>
        /// Returns a boolean capability/flag depending on <paramref name="index"/>.
        /// Returns false if the driver does not understand the passed index.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="index">Boolean parameter index (e.g., gbpConnected, gbpShutter, gbpGPS, etc.).</param>
        /// <param name="value">Returned value.</param>
        /// <returns>True if successful; otherwise false (unsupported index or failure).</returns>
        bool GetBooleanParameter(UIntPtr handle, MoravianBooleanParameter index, out bool value);

        /// <summary>
        /// Returns an integer parameter depending on <paramref name="index"/>.
        /// Returns false if the driver does not understand the passed index.
        /// Some values (e.g., maximum pixel value, bias value) may depend on current read mode and binning,
        /// and should be queried after SetReadMode/SetBinning.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="index">Integer parameter index (e.g., chip width/height, max binning, exposure limits, versions, etc.).</param>
        /// <param name="value">Returned value.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool GetIntegerParameter(UIntPtr handle, MoravianIntegerParameter index, out int value);

        /// <summary>
        /// Returns a string parameter depending on <paramref name="index"/>.
        /// The driver copies the string to <paramref name="sb"/> and must not overflow the buffer.
        /// The PDF specifies passing the highest character index of the buffer (0-based); in this C# wrapper
        /// <paramref name="maxLen"/> is treated as the effective capacity/limit for the provided StringBuilder.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="index">String parameter index (camera description, manufacturer, serial, chip description).</param>
        /// <param name="maxLen">Maximum length/capacity for the output buffer.</param>
        /// <param name="sb">Output buffer receiving the string.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool GetStringParameter(UIntPtr handle, MoravianStringParameter index, int maxLen, StringBuilder sb);

        /// <summary>
        /// Returns a floating-point value reflecting the current camera state (e.g., CCD temperature).
        /// Unlike GetBoolean/Integer/StringParameter, GetValue typically requires communication with the camera.
        /// Returns false if the driver does not understand the passed index.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="index">Value parameter index (temperatures, supply voltage, power utilization, ADC gain, etc.).</param>
        /// <param name="value">Returned value.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool GetValue(UIntPtr handle, MoravianValueParameter index, out float value);

        /// <summary>
        /// Enumerates available read modes. The caller passes an index starting at 0 and increments until the call returns false.
        /// The mode description is returned in <paramref name="sb"/> similarly to GetStringParameter.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="index">0-based read mode index.</param>
        /// <param name="maxLen">Maximum length/capacity for the output buffer.</param>
        /// <param name="sb">Output buffer receiving the mode description.</param>
        /// <returns>True if an item exists for the index; false when enumeration is complete or unsupported.</returns>
        bool EnumerateReadModes(UIntPtr handle, int index, int maxLen, StringBuilder sb);

        /// <summary>
        /// Enumerates filters provided by the camera (if supported). The caller passes an index starting at 0 and increments until false is returned.
        /// The driver provides a filter description and a Windows color hint suitable for drawing the filter name.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="index">0-based filter index.</param>
        /// <param name="maxLen">Maximum length/capacity for the output buffer.</param>
        /// <param name="sb">Output buffer receiving the filter description.</param>
        /// <param name="color">Windows color hint for the filter.</param>
        /// <returns>True if an item exists for the index; false when enumeration is complete or unsupported.</returns>
        bool EnumerateFilters(UIntPtr handle, uint index, int maxLen, StringBuilder sb, out uint color);

        /// <summary>
        /// Same as EnumerateFilters, but returns an additional focuser offset for the filter.
        /// Offset units can be micrometers or focuser-specific units (steps). If micrometers are used,
        /// GetBooleanParameter(gbpMicrometerFilterOffsets) should return true.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="index">0-based filter index.</param>
        /// <param name="maxLen">Maximum length/capacity for the output buffer.</param>
        /// <param name="sb">Output buffer receiving the filter description.</param>
        /// <param name="color">Windows color hint for the filter.</param>
        /// <param name="offset">Focuser shift for the filter.</param>
        /// <returns>True if an item exists for the index; false when enumeration is complete or unsupported.</returns>
        bool EnumerateFilters2(UIntPtr handle, uint index, int maxLen, StringBuilder sb, out uint color, out int offset);

        /// <summary>
        /// Sets the required read mode.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="mode">Read mode index.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool SetReadMode(UIntPtr handle, int mode);

        /// <summary>
        /// Sets the required read binning. If the camera does not support binning, this call has no effect.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="x">Horizontal binning factor.</param>
        /// <param name="y">Vertical binning factor.</param>
        /// <returns>True if successful; otherwise false (depending on driver implementation).</returns>
        bool SetBinning(UIntPtr handle, uint x, uint y);

        /// <summary>
        /// Sets the required gain. The gain value is camera-dependent (often a register value).
        /// The valid range is 0..gipMaxGain (from GetIntegerParameter).
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="gain">Gain register value.</param>
        void SetGain(UIntPtr handle, uint gain);

        /// <summary>
        /// Selects the required filter. If the camera is not equipped with a filter wheel, this call has no effect.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="index">Filter index.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool SetFilter(UIntPtr handle, uint index);

        /// <summary>
        /// The filter wheel performs the initialization, during which the zero filter position is found and set.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool ReinitFilterWheel(UIntPtr handle);

        /// <summary>
        /// Sets the required chip temperature in degrees Celsius. If the camera has no cooler, this call has no effect.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="temperature">Target chip temperature (°C).</param>
        void SetTemperature(UIntPtr handle, float temperature);

        /// <summary>
        /// Sets the maximum temperature change speed in degrees Celsius per minute. If the camera has no cooler, this call has no effect.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="ramp">Ramp speed (°C/min).</param>
        void SetTemperatureRamp(UIntPtr handle, float ramp);

        /// <summary>
        /// Sets the fan rotation speed (if supported). Maximum value should be read using GetIntegerParameter(gipMaxFan).
        /// If only on/off is supported, max is 1 (on) and 0 means off.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="speed">Fan speed value.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool SetFan(UIntPtr handle, byte speed);

        /// <summary>
        /// Sets CCD window heating intensity (if supported). Maximum value should be read using GetIntegerParameter(gipMaxWindowHeating).
        /// If only on/off is supported, max is 1 (on) and 0 means off.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="on">Heating control (implementation may treat this as on/off).</param>
        bool SetWindowHeating(UIntPtr handle, bool on);

        /// <summary>
        /// Controls CCD preflash (if supported). PreflashTime defines how long the internal LED is on (seconds),
        /// and ClearNum defines how many sensor clears follow the preflash.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="preflashTime">Preflash time (seconds).</param>
        /// <param name="clearNum">Number of clears after preflash.</param>
        bool SetPreflash(UIntPtr handle, double preflashTime, uint clearNum);

        /// <summary>
        /// Initiates telescope movement via the camera autoguider port (if present) for the specified durations in milliseconds.
        /// Sign defines direction in each axis. Maximum length is approximately 32.7 seconds.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="raDurationMs">R.A. movement duration in milliseconds (signed; direction by sign).</param>
        /// <param name="decDurationMs">Dec movement duration in milliseconds (signed; direction by sign).</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool MoveTelescope(UIntPtr handle, short raDurationMs, short decDurationMs);

        /// <summary>
        /// Returns whether a movement initiated by MoveTelescope is still in progress.
        /// If the camera is not equipped with an autoguider port, this call has no effect.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="moving">True if movement is still in progress.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool MoveInProgress(UIntPtr handle, out bool moving);

        /// <summary>
        /// Returns a failure description string after a driver call fails (returns false).
        /// The string is returned similarly to GetStringParameter.
        /// </summary>
        /// <param name="handle">Camera instance handle.</param>
        /// <param name="maxLen">Maximum length/capacity for the output buffer.</param>
        /// <param name="sb">Output buffer receiving the error description.</param>
        void GetLastErrorString(UIntPtr handle, int maxLen, StringBuilder sb);
    }

    public interface IMoravianCameraTimingExposure {

        /// <summary>
        /// Starts an exposure using the camera-timing asynchronous interface.
        /// The driver accepts exposure time (seconds), whether to operate shutter (light vs dark), and sub-frame coordinates.
        /// If sub-frame read is not supported, x and y must be 0 and w and d must be the full sensor dimensions.
        /// </summary>
        bool StartExposure(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d);

        /// <summary>
        /// Starts an exposure that waits for a hardware trigger input (if supported).
        /// If the camera is not equipped with trigger input, this behaves like StartExposure (starts immediately).
        /// Use GetBooleanParameter(gbpTrigger) to test trigger availability.
        /// </summary>
        bool StartExposureTrigger(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d);

        /// <summary>
        /// Aborts an exposure started by StartExposure/StartExposureTrigger before the exposure time expires.
        /// The downloadFlag indicates whether the image should still be digitized for later ReadImage,
        /// or discarded.
        /// </summary>
        bool AbortExposure(UIntPtr handle, bool downloadFlag);

        /// <summary>
        /// Queries whether the image from the last started exposure is ready to be read.
        /// Recommended usage is to wait approximately expTime in the application and only then poll ImageReady,
        /// instead of busy-loop polling for the entire exposure time.
        /// </summary>
        bool ImageReady(UIntPtr handle, out bool ready);

        /// <summary>
        /// Reads the exposed image after ImageReady returns ready=true.
        /// Expected format is a w*d matrix of 16-bit pixels copied into the caller-provided buffer.
        /// bufferLen is in bytes (not pixels) and must be large enough for the image, otherwise the call fails.
        /// </summary>
        bool ReadImage(UIntPtr handle, uint bufferLen, ushort[] buffer);

        /// <summary>
        /// Reads the exposed image like ReadImage, but instructs the camera to immediately start the subsequent exposure
        /// for continuous (serial) read operation. If the camera does not support serial read, the call returns false.
        /// Use GetBooleanParameter(gbpContinuousExposures) to test support.
        /// The last image of the sequence should be read using ReadImage, or the series can be aborted using AbortExposure.
        /// </summary>
        bool ReadImageExposure(UIntPtr handle, uint bufferLen, ushort[] buffer);
    }

    public interface IMoravianComputerTimingExposure {

        /// <summary>
        /// Begins an exposure in the computer-timing interface.
        /// If this optional function is implemented, it is used instead of ClearSensor and Open.
        /// UseShutter indicates whether shutter operation is required (light vs dark/bias).
        /// </summary>
        bool BeginExposure(UIntPtr handle, bool useShutter);

        /// <summary>
        /// Ends an exposure in the computer-timing interface.
        /// If present, this function complements BeginExposure and is called instead of Close.
        /// AbortData must be true if the exposure was canceled and no image will be read.
        /// </summary>
        bool EndExposure(UIntPtr handle, bool useShutter, bool abortData);

        /// <summary>
        /// Reads an image from the camera (computer-timing interface).
        /// Returns a w*d matrix of 16-bit pixels copied into the caller-provided buffer.
        /// bufferLen is in bytes (not pixels) and must be large enough for the image, otherwise the call fails.
        /// If sub-frame read is not supported, x and y must be 0 and w and d must be the full sensor dimensions.
        /// </summary>
        bool GetImage(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);

        /// <summary>
        /// Reads an image in 8-bit format, if supported by the camera and the active read mode.
        /// Using the 16-bit variants may force internal expansion from 8-bit to 16-bit, which costs time.
        /// If 8-bit is not supported/active, this call returns false without performing the exposure/read.
        /// </summary>
        bool GetImage8b(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);

        /// <summary>
        /// Equivalent to GetImage (16-bit pixels). In the official API the *16b variants are aliases to the default calls.
        /// </summary>
        bool GetImage16b(UIntPtr handle, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);

        /// <summary>
        /// Convenience function to perform an exposure (camera counts time) and read the image in one call.
        /// Returns a w*d matrix of 16-bit pixels copied into the caller-provided buffer.
        /// bufferLen is in bytes (not pixels) and must be large enough.
        /// </summary>
        bool GetImageExposure(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);

        /// <summary>
        /// 8-bit variant of GetImageExposure, if supported by the camera and active read mode.
        /// If not supported/active, returns false without performing the exposure.
        /// </summary>
        bool GetImageExposure8b(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);

        /// <summary>
        /// Equivalent to GetImageExposure (16-bit pixels). In the official API the *16b variants are aliases to the default calls.
        /// </summary>
        bool GetImageExposure16b(UIntPtr handle, double expTime, bool useShutter, int x, int y, int w, int d, uint bufferLen, ushort[] buffer);
    }

    public interface IMoravianManualTimingExposure {

        /// <summary>
        /// Performs detector clearing, necessary before each exposure when the optional BeginExposure/EndExposure
        /// functions are not available in the computer-timing interface.
        /// </summary>
        bool ClearSensor(UIntPtr handle);

        /// <summary>
        /// Opens the camera shutter. If the camera has no mechanical shutter, this function has no effect.
        /// </summary>
        bool OpenShutter(UIntPtr handle);

        /// <summary>
        /// Closes the camera shutter. If the camera has no mechanical shutter, this function has no effect.
        /// </summary>
        bool CloseShutter(UIntPtr handle);
    }

    public interface IMoravianGPS {

        /// <summary>
        /// Obtains the exposure start time of the last acquired image based on GPS time.
        /// Time information is available only after ReadImage/ReadImageExposure; the time remains valid
        /// until the next ReadImage/ReadImageExposure (timestamps are queued similarly to frames for FIFO operation).
        /// If the function returns false, the camera either has no GPS receiver or did not have valid time lock at exposure start.
        /// Presence of GPS can be tested using GetBooleanParameter(gbpGPS).
        /// </summary>
        bool GetImageTimeStamp(UIntPtr handle, out int year, out int month, out int day, out int hour, out int minute, out double second);

        /// <summary>
        /// Reads GPS location and current time at the moment of the call.
        /// Lat/Lon are decimal degrees (North/East positive; South/West negative). MSL is meters.
        /// Satellites indicates tracked satellites; fix indicates whether a position fix is available.
        /// Location updates roughly every minute; time is obtained with higher precision timing mechanism.
        /// If satellites are insufficient for precise time lock, time-related outputs are returned as zeros.
        /// </summary>
        bool GetGPSData(UIntPtr handle,
            out double lat, out double lon, out double msl,
            out int year, out int month, out int day, out int hour, out int minute, out double second,
            out uint satellites, out bool fix);
    }

    public interface IMoravianConfigurable {

        /// <summary>
        /// Optional configuration entry point. If the driver requires configuration (e.g., IP address for network cameras),
        /// it may open a dialog using the provided parent window handle. Some drivers accept a special handle value (e.g., -1)
        /// to configure the driver globally; see the official documentation for expected behavior.
        /// </summary>
        void Configure(UIntPtr handle, IntPtr parentHwnd);
    }

    public enum MoravianBooleanParameter : uint {
        /// <summary>TRUE if camera currently connected.</summary>
        gbpConnected = 0,
        /// <summary>TRUE if camera supports sub-frame read.</summary>
        gbpSubFrame = 1,
        /// <summary>TRUE if camera supports multiple read modes.</summary>
        gbpReadModes = 2,
        /// <summary>TRUE if camera is equipped with mechanical shutter.</summary>
        gbpShutter = 3,
        /// <summary>TRUE if camera is equipped with active CCD cooler.</summary>
        gbpCooler = 4,
        /// <summary>TRUE if camera fan can be controlled.</summary>
        gbpFan = 5,
        /// <summary>TRUE if camera controls filter wheel.</summary>
        gbpFilters = 6,
        /// <summary>TRUE if camera is capable to guide the telescope mount.</summary>
        gbpGuide = 7,
        /// <summary>TRUE if camera can control the CCD window heating.</summary>
        gbpWindowHeating = 8,
        /// <summary>TRUE if camera can use CCD preflash.</summary>
        gbpPreflash = 9,
        /// <summary>TRUE if camera horizontal and vertical binning can differ.</summary>
        gbpAsymmetricBinning = 10,
        /// <summary>TRUE if filter focusing offsets are expressed in micrometers.</summary>
        gbpMicrometerFilterOffsets = 11,
        /// <summary>TRUE if camera can return power utilization in GetValue.</summary>
        gbpPowerUtilization = 12,
        /// <summary>TRUE if camera can return used gain in GetValue.</summary>
        gbpGain = 13,
        /// <summary>TRUE if the sensor is equipped with electronic shutter.</summary>
        gbpElectronicShutter = 14,
        /// <summary>TRUE if the sensor is equipped with GPS receiver.</summary>
        gbpGPS = 16,
        /// <summary>TRUE if the camera is capable of serial (continuous) exposures.</summary>
        gbpContinuousExposures = 17,
        /// <summary>TRUE if the sensor is equipped with hardware trigger port.</summary>
        gbpTrigger = 18,
        /// <summary>TRUE if camera is configured.</summary>
        gbpConfigured = 127,
        /// <summary>TRUE if camera has Bayer RGBG filters on sensor.</summary>
        gbpRGB = 128,
        /// <summary>TRUE if camera has CMY filters on sensor.</summary>
        gbpCMY = 129,
        /// <summary>TRUE if camera has CMYG filters on sensor.</summary>
        gbpCMYG = 130,
        /// <summary>TRUE if camera Bayer mask starts on horizontal odd pixel.</summary>
        gbpDebayerXOdd = 131,
        /// <summary>TRUE if camera Bayer mask starts on vertical odd pixel.</summary>
        gbpDebayerYOdd = 132,
        /// <summary>TRUE if CCD detector is interlaced (else progressive).</summary>
        gbpInterlaced = 133
    }

    public enum MoravianIntegerParameter : uint {
        /// <summary>Identifier of the current camera.</summary>
        gipCameraId = 0,
        /// <summary>Sensor width in pixels.</summary>
        gipChipW = 1,
        /// <summary>Sensor depth (height) in pixels.</summary>
        gipChipD = 2,
        /// <summary>Sensor pixel width in nanometers.</summary>
        gipPixelW = 3,
        /// <summary>Sensor pixel depth (height) in nanometers.</summary>
        gipPixelD = 4,
        /// <summary>Maximum binning in horizontal direction.</summary>
        gipMaxBinningX = 5,
        /// <summary>Maximum binning in vertical direction.</summary>
        gipMaxBinningY = 6,
        /// <summary>Number of read modes offered by the camera.</summary>
        gipReadModes = 7,
        /// <summary>Number of filters offered by the camera.</summary>
        gipFilters = 8,
        /// <summary>Shortest exposure time in microseconds (µs).</summary>
        gipMinimalExposure = 9,
        /// <summary>Longest exposure time in milliseconds (ms).</summary>
        gipMaximalExposure = 10,
        /// <summary>Longest time to move the telescope in milliseconds (ms).</summary>
        gipMaximalMoveTime = 11,
        /// <summary>Read mode to be used as default.</summary>
        gipDefaultReadMode = 12,
        /// <summary>Read mode to be used for preview (fast read).</summary>
        gipPreviewReadMode = 13,
        /// <summary>Maximal value for SetWindowHeating call.</summary>
        gipMaxWindowHeating = 14,
        /// <summary>Maximal value for SetFan call.</summary>
        gipMaxFan = 15,
        /// <summary>Maximum value for SetGain call. </summary>
        gipMaxGain = 16,
        /// <summary>
        /// Maximum value of (saturated) pixel. May vary with read mode and binning; read after SetReadMode and SetBinning.
        /// </summary>
        gipMaxPossiblePixelValue = 17,
        /// <summary>
        /// Time to digitize one image line of rolling-shutter cameras equipped with GPS receiver.
        /// </summary>
        gipLineTime = 18,
        /// <summary>
        /// Minimum (bias) value of pixel. May vary with read mode and binning; read after SetReadMode and SetBinning.
        /// </summary>
        gipBiasPixelValue = 19,
        /// <summary>Camera firmware version (optional): major.</summary>
        gipFirmwareMajor = 128,
        /// <summary>Camera firmware version (optional): minor.</summary>
        gipFirmwareMinor = 129,
        /// <summary>Camera firmware version (optional): build.</summary>
        gipFirmwareBuild = 130,
        /// <summary>Driver version (optional): major.</summary>
        gipDriverMajor = 131,
        /// <summary>Driver version (optional): minor.</summary>
        gipDriverMinor = 132,
        /// <summary>Driver version (optional): build.</summary>
        gipDriverBuild = 133,
        /// <summary>Flash version (optional): major.</summary>
        gipFlashMajor = 134,
        /// <summary>Flash version (optional): minor.</summary>
        gipFlashMinor = 135,
        /// <summary>Flash version (optional): build.</summary>
        gipFlashBuild = 136
    }

    public enum MoravianStringParameter : uint {
        /// <summary>Camera description.</summary>
        gspCameraDescription = 0,
        /// <summary>Manufacturer name.</summary>
        gspManufacturer = 1,
        /// <summary>Camera serial number.</summary>
        gspCameraSerial = 2,
        /// <summary>Used CCD detector description.</summary>
        gspChipDescription = 3
    }

    public enum MoravianValueParameter : uint {
        /// <summary>Current temperature of the CCD detector in degrees Celsius.</summary>
        gvChipTemperature = 0,
        /// <summary>Current temperature of the cooler hot side in degrees Celsius.</summary>
        gvHotTemperature = 1,
        /// <summary>Current temperature inside the camera in degrees Celsius.</summary>
        gvCameraTemperature = 2,
        /// <summary>Current temperature of the environment air in degrees Celsius.</summary>
        gvEnvironmentTemperature = 3,
        /// <summary>Current voltage of the camera power supply.</summary>
        gvSupplyVoltage = 10,
        /// <summary>Current utilization of the CCD cooler (0 to 1).</summary>
        gvPowerUtilization = 11,
        /// <summary>Current gain of A/D converter in electrons/ADU.</summary>
        gvADCGain = 20
    }
}
