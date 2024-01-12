#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace QHYCCD {

    public class QhySdk : IQhySdk {
        private const string DLLNAME = "qhyccd.dll";

        private object lockobj = new object();
        public IntPtr handle = IntPtr.Zero;

        private bool sdkIsInitialized = false;
        private byte refCount = 0;

        public static readonly Lazy<IQhySdk> _instance = new Lazy<IQhySdk>(() => new QhySdk());

        public static IQhySdk Instance => _instance.Value;

        private QhySdk() {
            if (!sdkIsInitialized && handle == IntPtr.Zero && refCount == 0) {
                Logger.Trace($"QhyccdSdk: Loading QHY SDK, refCount={refCount}, sdkIsInitialized={sdkIsInitialized}");
                DllLoader.LoadDll(Path.Combine("QHYCCD", DLLNAME));
            }
        }

        #region Call Wrappers

        public void InitSdk() {
            lock (lockobj) {
                Logger.Trace($"QhyccdSdk: Initializing QHY SDK, refCount={refCount}, sdkIsInitialized={sdkIsInitialized}");

                if (!sdkIsInitialized && handle == IntPtr.Zero && refCount == 0) {
                    CheckReturn(InitQHYCCDResource(), MethodBase.GetCurrentMethod());
                    sdkIsInitialized = true;
                }
            }
        }

        public void ReleaseSdk() {
            lock (lockobj) {
                Logger.Trace($"QhyccdSdk: Releasing QHY SDK, refCount={refCount}");

                if (sdkIsInitialized && handle == IntPtr.Zero && refCount == 0) {
                    CheckReturn(ReleaseQHYCCDResource(), MethodBase.GetCurrentMethod());
                    sdkIsInitialized = false;
                }
            }
        }

        public void Open(StringBuilder id) {
            lock (lockobj) {
                Logger.Trace($"QhyccdSdk: Opening {id}, refCount={refCount}");

                if (handle == IntPtr.Zero && refCount == 0 && sdkIsInitialized) {
                    handle = OpenQHYCCD(id);

                    if (handle == IntPtr.Zero) {
                        throw new QHYCameraException("Unable to open camera", MethodBase.GetCurrentMethod(), new object[] { id });
                    }
                }

                refCount++;
                Logger.Trace($"QhyccdSdk: Opened {id}, refCount={refCount}");
            }
        }

        public void Close() {
            lock (lockobj) {
                Logger.Trace($"QhyccdSdk: Closing camera, refCount={refCount}");

                if (handle != IntPtr.Zero && refCount == 1) {
                    CheckReturn(CloseQHYCCD(handle), MethodBase.GetCurrentMethod(), handle);
                    handle = IntPtr.Zero;
                }

                refCount--;
                Logger.Trace($"QhyccdSdk: Closed camera, refCount={refCount}");
            }
        }

        public void InitCamera() {
            lock (lockobj) {
                CheckReturn(InitQHYCCD(handle), MethodBase.GetCurrentMethod(), handle);
            }
        }

        public uint Scan() {
            lock (lockobj) {
                return ScanQHYCCD();
            }
        }

        public uint SetBinMode(uint binX, uint binY) {
            lock (lockobj) {
                return SetQHYCCDBinMode(handle, binX, binY);
            }
        }

        public uint SetBitsMode(uint bitDepth) {
            lock (lockobj) {
                return SetQHYCCDBitsMode(handle, bitDepth);
            }
        }

        public uint SetStreamMode(byte mode) {
            lock (lockobj) {
                return SetQHYCCDStreamMode(handle, mode);
            }
        }

        public uint SetDebayerOnOff(bool onoff) {
            lock (lockobj) {
                return SetQHYCCDDebayerOnOff(handle, onoff);
            }
        }

        public uint GetReadMode(ref uint mode) {
            return GetQHYCCDReadMode(handle, ref mode);
        }

        public uint SetReadMode(uint mode) {
            lock (lockobj) {
                return SetQHYCCDReadMode(handle, mode);
            }
        }

        public uint GetNumberOfReadModes(ref uint numModes) {
            return GetQHYCCDNumberOfReadModes(handle, ref numModes);
        }

        public uint GetReadModeName(uint mode, StringBuilder modeName) {
            return GetQHYCCDReadModeName(handle, mode, modeName);
        }

        public uint ControlTemp(double targetTemp) {
            lock (lockobj) {
                return ControlQHYCCDTemp(handle, targetTemp);
            }
        }

        public uint ControlShutter(byte shutterState) {
            lock (lockobj) {
                return ControlQHYCCDShutter(handle, shutterState);
            }
        }

        public void GetId(uint index, StringBuilder id) {
            CheckReturn(GetQHYCCDId(index, id), MethodBase.GetCurrentMethod(), index, new object[] { id });
        }

        public void GetModel(StringBuilder id, StringBuilder model) {
            CheckReturn(GetQHYCCDModel(id, model), MethodBase.GetCurrentMethod(), new object[] { model });
        }

        public uint GetParamMinMaxStep(CONTROL_ID controlId, ref double min, ref double max, ref double step) {
            lock (lockobj) {
                return GetQHYCCDParamMinMaxStep(handle, controlId, ref min, ref max, ref step);
            }
        }

        public uint GetChipInfo(ref double chipW, ref double chipH, ref uint imageX, ref uint imageY, ref double pixelX, ref double pixelY, ref uint bpp) {
            return GetQHYCCDChipInfo(handle, ref chipW, ref chipH, ref imageX, ref imageY, ref pixelX, ref pixelY, ref bpp);
        }

        public uint GetEffectiveArea(ref uint startX, ref uint startY, ref uint imageX, ref uint imageY) {
            return GetQHYCCDEffectiveArea(handle, ref startX, ref startY, ref imageX, ref imageY);
        }

        public uint SetResolution(uint x, uint y, uint xSize, uint ySize) {
            lock (lockobj) {
                return SetQHYCCDResolution(handle, x, y, xSize, ySize);
            }
        }

        public uint ExpSingleFrame() {
            lock (lockobj) {
                return ExpQHYCCDSingleFrame(handle);
            }
        }

        public uint GetSingleFrame(ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] byte[] rawArray) {
            lock (lockobj) {
                return GetQHYCCDSingleFrame(handle, ref sizeX, ref sizeY, ref bpp, ref channels, rawArray);
            }
        }

        public uint GetSingleFrame(ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] ushort[] rawArray) {
            lock (lockobj) {
                return GetQHYCCDSingleFrame(handle, ref sizeX, ref sizeY, ref bpp, ref channels, rawArray);
            }
        }

        public uint CancelExposingAndReadout() {
            lock (lockobj) {
                return CancelQHYCCDExposingAndReadout(handle);
            }
        }

        public uint GetExposureRemaining() {
            return GetQHYCCDExposureRemaining(handle);
        }

        public uint GetPressure(ref double hpa) {
            return GetQHYCCDPressure(handle, ref hpa);
        }

        public uint GetHumidity(ref double rh) {
            return GetQHYCCDHumidity(handle, ref rh);
        }

        public bool IsCfwPlugged() {
            if (IsQHYCCDCFWPlugged(handle) == QHYCCD_SUCCESS) {
                return true;
            }

            return false;
        }

        public uint GetCfwStatus(byte[] status) {
            return GetQHYCCDCFWStatus(handle, status);
        }

        public uint SendOrderToCfw(string order, int length) {
            lock (lockobj) {
                return SendOrder2QHYCCDCFW(handle, order, length);
            }
        }

        #endregion Call Wrappers

        #region Utility Methods

        public bool IsControl(CONTROL_ID type) {
            bool result = false;

            if (IsQHYCCDControlAvailable(handle, type) == QHYCCD_SUCCESS) {
                Logger.Debug($"QHYCCD: Control {type} exists");
                result = true;
            } else {
                Logger.Debug($"QHYCCD: Control Value {type} is not available");
            }

            return result;
        }

        public double GetControlValue(CONTROL_ID type) {
            double rv;

            if ((rv = GetQHYCCDParam(handle, type)) != QHYCCD_ERROR) {
                Logger.Trace($"QHYCCD: Control {type} = {rv}");
                return rv;
            } else {
                Logger.Error($"QHYCCD: Failed to Get value for control {type}");
                return QHYCCD_ERROR;
            }
        }

        public bool SetControlValue(CONTROL_ID type, double value) {
            lock (lockobj) {
                if (SetQHYCCDParam(handle, type, value) == QHYCCD_SUCCESS) {
                    Logger.Debug($"QHYCCD: Setting Control {type} to {value}");
                    return true;
                } else {
                    Logger.Warning($"QHYCCD: Failed to Set Control {type} with value {value}");
                    return false;
                }
            }
        }

        public BAYER_ID GetBayerType() {
            return (BAYER_ID)IsQHYCCDControlAvailable(handle, CONTROL_ID.CAM_COLOR);
        }

        public string GetSdkVersion() {
            uint year = 0, month = 0, day = 0, subday = 0;
            CheckReturn(GetQHYCCDSDKVersion(ref year, ref month, ref day, ref subday), MethodBase.GetCurrentMethod());

            return year.ToString() + "-" + month.ToString() + "-" + day.ToString() + "-" + subday.ToString();
        }

        public string GetFwVersion() {
            string version = "N/A";
            byte[] buf = new byte[10];

            if (GetQHYCCDFWVersion(handle, buf) != QHYCCD_ERROR) {
                int ver = buf[0] >> 4;
                if (ver < 9) {
                    version = Convert.ToString(ver + 16) + "-" + Convert.ToString(buf[0] & -241) + "-" + Convert.ToString(buf[1]);
                } else {
                    version = Convert.ToString(ver) + "-" + Convert.ToString(buf[0] & -241) + "-" + Convert.ToString(buf[1]);
                }
            }

            return version;
        }

        public string GetFpgaVersion() {
            string version = "N/A";
            byte[] buf = new byte[4];

            for (byte i = 0; i <= 3; i++) {
                if (GetQHYCCDFPGAVersion(handle, i, buf) != QHYCCD_ERROR) {
                    if (i > 0) {
                        version += ", ";
                    }

                    version = i + ": " + Convert.ToString(buf[0]) + "-" + Convert.ToString(buf[1]) + "-" + Convert.ToString(buf[2]) + "-" + Convert.ToString(buf[3]);
                } else {
                    break;
                }
            }

            return version;
        }

        // Added for Live view/recording
        public uint BeginQHYCCDLive() {
            lock (lockobj) {
                return BeginQHYCCDLive(handle);
            }
        }

        // These two methods are identical on the C side, they just have different pointer types (they're ABI compatible).
        public uint GetQHYCCDLiveFrame(ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] byte[] rawArray) {
            lock (lockobj) {
                return GetQHYCCDLiveFrame(handle, ref sizeX, ref sizeY, ref bpp, ref channels, rawArray);
            }
        }

        public uint GetQHYCCDLiveFrame(ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] ushort[] rawArray) {
            lock (lockobj) {
                return GetQHYCCDLiveFrame(handle, ref sizeX, ref sizeY, ref bpp, ref channels, rawArray);
            }
        }

        public uint StopQHYCCDLive() {
            lock (lockobj) {
                return StopQHYCCDLive(handle);
            }
        }

        public uint GetQHYCCDMemLength() {
            return GetQHYCCDMemLength(handle);
        }

        public uint SetQHYCCDGPSVCOXFreq(ushort freq) {
            lock (lockobj) {
                return SetQHYCCDGPSVCOXFreq(handle, freq);
            }
        }

        public uint SetQHYCCDGPSLedCalMode(byte mode) {
            lock (lockobj) {
                return SetQHYCCDGPSLedCalMode(handle, mode);
            }
        }

        public uint SetQHYCCDGPSMasterSlave(byte mode) {
            lock (lockobj) {
                return SetQHYCCDGPSMasterSlave(handle, mode);
            }
        }

        public void SetQHYCCDGPSPOSA(uint pos, byte width) {
            lock (lockobj) {
                SetQHYCCDGPSPOSA(handle, pos, width);
            }
        }

        public void SetQHYCCDGPSPOSB(uint pos, byte width) {
            lock (lockobj) {
                SetQHYCCDGPSPOSB(handle, pos, width);
            }
        }

        public uint GetQHYCCDPreciseExposureInfo(ref uint pixelPeriod, ref uint linePeriod, ref uint framePeriod, ref uint clocksPerLine, ref uint linesPerFrame, ref uint actualExposureTime, ref byte isLongExposureMode) {
            lock (lockobj) {
                return GetQHYCCDPreciseExposureInfo(handle, ref pixelPeriod, ref linePeriod, ref framePeriod, ref clocksPerLine, ref linesPerFrame, ref actualExposureTime, ref isLongExposureMode);
            }
        }

        public uint GetQHYCCDRollingShutterEndOffset(uint row, ref double offset) {
            lock (lockobj) {
                return GetQHYCCDRollingShutterEndOffset(handle, row, ref offset);
            }
        }

        #endregion Utility Methods

        #region Exception Generator

        private static void CheckReturn(uint code, MethodBase callingMethod, params object[] parameters) {
            switch (code) {
                case QHYCCD_SUCCESS:
                    break;

                case QHYCCD_ERROR:
                    throw new QHYCameraException("QHY SDK returned an error status", callingMethod, parameters);
                default:
                    throw new ArgumentOutOfRangeException("QHY SDK returned an unknown error");
            }
        }

        public class QHYCameraException : Exception {

            public QHYCameraException(string message, MethodBase callingMethod, object[] parameters) : base(CreateMessage(message, callingMethod, parameters)) {
            }

            private static string CreateMessage(string message, MethodBase callingMethod, object[] parameters) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error '" + message + "' from call to ");
                sb.Append("QHY" + callingMethod.Name + "(");
                var paramNames = callingMethod.GetParameters().Select(x => x.Name);
                foreach (var line in paramNames.Zip(parameters, (s, o) => string.Format("{0}={1}, ", s, o))) {
                    sb.Append(line);
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(")");
                return sb.ToString();
            }
        }

        #endregion Exception Generator

        #region QHY SDK constants

        /// <summary>
        /// SDK return code: Error
        /// </summary>
        public const uint QHYCCD_ERROR = 0xFFFFFFFF;

        /// <summary>
        /// SDK return code: Success
        /// </summary>
        public const uint QHYCCD_SUCCESS = 0;

        /// <summary>
        /// SDK return code: Wait 200ms
        /// </summary>
        public const uint QHYCCD_DELAY_200MS = 0x2000;

        /// <summary>
        /// SDK return code: Wait 200ms
        /// </summary>
        public const uint QHYCCD_READ_DIRECTLY = 0x2001;

        /// <summary>
        /// Sensor bayer mask pattern
        /// <seealso cref="LibQHYCCD.CONTROL_ID.CAM_COLOR"/>
        /// </summary>
        public enum BAYER_ID {

            /// <summary>
            /// GBRG
            /// </summary>
            BAYER_GB = 1,

            /// <summary>
            /// GRBG
            /// </summary>
            BAYER_GR,

            /// <summary>
            /// BGGR
            /// </summary>
            BAYER_BG,

            /// <summary>
            /// RGGB
            /// </summary>
            BAYER_RG
        };

        /// <summary>
        /// QHYCCD Camera parameter control IDs.
        /// These are sourced from qhyccd.h provided by the QHY SDK distribution.
        /// </summary>
        public enum CONTROL_ID {

            /// <summary>
            /// Image Brightness
            /// </summary>
            CONTROL_BRIGHTNESS = 0,

            /// <summary>
            /// Image Contrast
            /// </summary>
            CONTROL_CONTRAST,

            /// <summary>
            /// White Balance - Red
            /// </summary>
            CONTROL_WBR,

            /// <summary>
            /// White Balance - Blue
            /// </summary>
            CONTROL_WBB,

            /// <summary>
            /// White Balance - Green
            /// </summary>
            CONTROL_WBG,

            /// <summary>
            /// Screen Gamma
            /// </summary>
            CONTROL_GAMMA,

            /// <summary>
            /// Camera Gain
            /// </summary>
            CONTROL_GAIN,

            /// <summary>
            /// Camera Offset
            /// </summary>
            CONTROL_OFFSET,

            /// <summary>
            /// Exposure Time (microseconds)
            /// </summary>
            CONTROL_EXPOSURE,

            /// <summary>
            /// USB Transfer Speed
            /// <list type="table">
            /// <listheader><term>Value</term><description>Description</description></listheader>
            /// <item><term>0</term><description>Slow</description></item>
            /// <item><term>1</term><description>Medium</description></item>
            /// <item><term>2</term><description>Fast</description></item>
            /// </list>
            /// </summary>
            CONTROL_SPEED,

            /// <summary>
            /// Bit depth of the image retreieved from the camera
            /// </summary>
            CONTROL_TRANSFERBIT,

            /// <summary>
            /// Number of channels in the image
            /// </summary>
            CONTROL_CHANNELS,

            /// <summary>
            /// USB bandwidth
            /// </summary>
            CONTROL_USBTRAFFIC,

            /// <summary>
            /// Row denoise
            /// </summary>
            CONTROL_ROWNOISERE,

            /// <summary>
            /// Sensor temperature (Celcius)
            /// </summary>
            CONTROL_CURTEMP,

            /// <summary>
            /// Current Thermoelectric Cooler (TEC) duty cycle (power level)
            /// </summary>
            CONTROL_CURPWM,

            /// <summary>
            /// Set TEC duty cycle (power level)
            /// </summary>
            CONTROL_MANULPWM,

            /// <summary>
            /// Has filter wheel control port
            /// </summary>
            CONTROL_CFWPORT,

            /// <summary>
            /// Has integrated active cooling
            /// </summary>
            CONTROL_COOLER,

            /// <summary>
            /// Has ST4 guide port
            /// </summary>
            CONTROL_ST4PORT,

            /// <summary>
            /// Color sensor bayer pattern
            /// <seealso cref="LibQHYCCD.BAYER_ID"/>
            /// </summary>
            CAM_COLOR,

            /// <summary>
            /// Has 1x1 bin mode
            /// </summary>
            CAM_BIN1X1MODE,

            /// <summary>
            /// Has 2x2 bin mode
            /// </summary>
            CAM_BIN2X2MODE,

            /// <summary>
            /// Has 3x3 bin mode
            /// </summary>
            CAM_BIN3X3MODE,

            /// <summary>
            /// Has 4x4 bin mode
            /// </summary>
            CAM_BIN4X4MODE,

            /// <summary>
            /// Has mechanical shutter
            /// </summary>
            CAM_MECHANICALSHUTTER,

            /// <summary>
            /// Has trigger interface
            /// </summary>
            CAM_TRIGER_INTERFACE,

            /// <summary>
            /// Has TEC protection circuit
            /// </summary>
            CAM_TECOVERPROTECT_INTERFACE,

            /// <summary>
            /// Has signal clamp circuit
            /// </summary>
            CAM_SINGNALCLAMP_INTERFACE,

            /// <summary>
            /// Has fine tone
            /// </summary>
            CAM_FINETONE_INTERFACE,

            /// <summary>
            /// Has shutter motor heater
            /// </summary>
            CAM_SHUTTERMOTORHEATING_INTERFACE,

            /// <summary>
            /// Has calibration interface
            /// </summary>
            CAM_CALIBRATEFPN_INTERFACE,

            /// <summary>
            /// Has sensor temperature interface
            /// </summary>
            CAM_CHIPTEMPERATURESENSOR_INTERFACE,

            /// <summary>
            /// Has USB readout interface
            /// </summary>
            CAM_USBREADOUTSLOWEST_INTERFACE,

            /// <summary>
            /// Signifies camera is capable of 8bit output
            /// </summary>
            CAM_8BITS,

            /// <summary>
            /// Signifies camera is capable of 16bit output
            /// </summary>
            CAM_16BITS,

            /// <summary>
            /// Has integrated GPS
            /// </summary>
            CAM_GPS,

            /// <summary>
            /// Camera can ignore overscan area
            /// </summary>
            CAM_IGNOREOVERSCAN_INTERFACE,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            QHYCCD_3A_AUTOBALANCE,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            QHYCCD_3A_AUTOEXPOSURE,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            QHYCCD_3A_AUTOFOCUS,

            /// <summary>
            /// Sensor amplifier glow control
            /// list type="table">
            /// <listheader><term>Value</term><description>Description</description></listheader>
            /// <item><term>0</term><description>Automatic</description></item>
            /// <item><term>1</term><description>On</description></item>
            /// <item><term>1</term><description>Off</description></item>
            /// </list>
            /// </summary>
            CONTROL_AMPV,

            /// <summary>
            /// Virtual camera
            /// list type="table">
            /// <listheader><term>Value</term><description>Description</description></listheader>
            /// <item><term>0</term><description>Off</description></item>
            /// <item><term>1</term><description>On</description></item>
            /// </list>
            /// </summary>
            CONTROL_VCAM,

            /// <summary>
            /// Set view mode
            /// </summary>
            CAM_VIEW_MODE,

            /// <summary>
            /// Get filter wheel slot count
            /// </summary>
            CONTROL_CFWSLOTSNUM,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            IS_EXPOSING_DONE,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            ScreenStretchB,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            ScreenStretchW,

            /// <summary>
            /// DDR memory buffer control
            /// list type="table">
            /// <listheader><term>Value</term><description>Description</description></listheader>
            /// <item><term>0</term><description>Off</description></item>
            /// <item><term>1</term><description>On</description></item>
            /// </list>
            /// </summary>
            CONTROL_DDR,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            CAM_LIGHT_PERFORMANCE_MODE,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            CAM_QHY5II_GUIDE_MODE,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            DDR_BUFFER_CAPACITY,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            DDR_BUFFER_READ_THRESHOLD,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            DefaultGain,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            DefaultOffset,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            OutputDataActualBits,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            OutputDataAlignment,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            CAM_SINGLEFRAMEMODE,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            CAM_LIVEVIDEOMODE,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            CAM_IS_COLOR,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            hasHardwareFrameCounter,

            /// <summary>
            /// Unknown/Undocumented
            /// </summary>
            CONTROL_MAX_ID_Error,

            /// <summary>
            /// Sensor chamber huidity sensor
            /// </summary>
            CAM_HUMIDITY,

            /// <summary>
            /// Sensor chamber air prsssure sensor
            /// </summary>
            CAM_PRESSURE,

            /// <summary>
            /// Sensor chamber vacuum pump
            /// </summary>
            CONTROL_VACUUM_PUMP,

            /// <summary>
            /// air cycle pump for sensor drying
            /// </summary>
            CONTROL_SensorChamberCycle_PUMP,

            /// <summary>
            /// camera supports 32bit output
            /// </summary>
            CAM_32BITS,

            /// <summary>
            /// Sensor working status [0:init  1:good  2:checkErr  3:monitorErr 8:good 9:powerChipErr]  410 461 411 600 268 [Eris board]
            /// </summary>
            CAM_Sensor_ULVO_Status,

            /// <summary>
            /// Sensor phase retraining for 2020, 4040/PRO，6060, 42PRO
            /// </summary>
            CAM_SensorPhaseReTrain,

            /// <summary>
            /// 2410 461 411 600 268 for now
            /// </summary>
            CAM_InitConfigFromFlash,

            /// <summary>
            /// check if camera has multiple triger mode
            /// </summary>
            CAM_TRIGER_MODE,

            /// <summary>
            /// check if camera support triger out function
            /// </summary>
            CAM_TRIGER_OUT,

            /// <summary>
            /// check if camera support burst mode
            /// </summary>
            CAM_BURST_MODE,

            /// <summary>
            /// for OEM-600
            /// </summary>
            CAM_SPEAKER_LED_ALARM,

            /// <summary>
            /// for _QHY5III178C Celestron, SDK have to feed this dog or it go reset
            /// </summary>
            CAM_WATCH_DOG_FPGA,

            /// <summary>
            /// check if camera has bin6x6 mode
            /// </summary>
            CAM_BIN6X6MODE,

            /// <summary>
            /// check if camera has bin8x8 mode
            /// </summary>
            CAM_BIN8X8MODE,

            /// <summary>
            /// Show GPS LED tab on sharpCap
            /// </summary>
            CAM_GlobalSensorGPSLED,

            /// <summary>
            /// Process image
            /// </summary>
            CONTROL_ImgProc,

            /// <summary>
            /// Remove single RBI
            /// </summary>
            CONTROL_RemoveRBI,

            /// <summary>
            /// image stabilization
            /// </summary>
            CONTROL_GlobalReset,

            /// <summary>
            /// QHY undocumented
            /// </summary>
            CONTROL_FrameDetect,

            /// <summary>
            /// Supports the conversion between db and gain
            /// </summary>
            CAM_GainDBConversion,

            /// <summary>
            /// QHY undocumented
            /// </summary>
            CAM_CurveSystemGain,

            /// <summary>
            /// QHY undocumented
            /// </summary>
            CAM_CurveFullWell,

            /// <summary>
            /// QHY undocumented
            /// </summary>
            CAM_CurveReadoutNoise,
        };

        /// <summary>
        /// For setting QHY camera exposure mode
        /// <seealso cref="SetQHYCCDStreamMode(IntPtr, byte)"/>
        /// </summary>
        public enum QHYCCD_CAMERA_MODE : byte {

            /// <summary>
            /// Single exposure mode
            /// </summary>
            SINGLE_EXPOSURE = 0,

            /// <summary>
            /// Video stream (live) mode
            /// </summary>
            VIDEO_STREAM
        };

        /// <summary>
        /// For setting on-camera image processing
        /// </summary>
        public enum QHY_IMAGE_PROC : byte {
            NOPROC = 0,
            ROTATION180,
            ROTATION90L,
            ROTATION90R,
            MIRRORH,
            MIRRORV,
        }

        #endregion QHY SDK constants

        #region DLL Imports

        [DllImport(DLLNAME, EntryPoint = "CancelQHYCCDExposing", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint CancelQHYCCDExposing(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "CancelQHYCCDExposingAndReadout", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint CancelQHYCCDExposingAndReadout(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "CloseQHYCCD", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint CloseQHYCCD(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "ControlQHYCCDShutter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint ControlQHYCCDShutter(IntPtr handle, byte shutterState);

        [DllImport(DLLNAME, EntryPoint = "ControlQHYCCDTemp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint ControlQHYCCDTemp(IntPtr handle, double targetTemp);

        [DllImport(DLLNAME, EntryPoint = "ExpQHYCCDSingleFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint ExpQHYCCDSingleFrame(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "IsQHYCCDCFWPlugged", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint IsQHYCCDCFWPlugged(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDCFWStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDCFWStatus(IntPtr handle, [In, Out] byte[] cfwStatus);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDCameraStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDCameraStatus(IntPtr handle, [In, Out] byte status);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDChipInfo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDChipInfo(IntPtr handle, ref double chipw, ref double chiph, ref uint imagew, ref uint imageh, ref double pixelw, ref double pixelh, ref uint bpp);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDEffectiveArea", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDEffectiveArea(IntPtr handle, ref uint startx, ref uint starty, ref uint sizex, ref uint sizey);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDExposureRemaining", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDExposureRemaining(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDFWVersion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDFWVersion(IntPtr handle, [Out] byte[] verBuf);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDId", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDId(uint index, StringBuilder id);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDModel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDModel(StringBuilder id, StringBuilder model);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDParam", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe double GetQHYCCDParam(IntPtr handle, CONTROL_ID controlid);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDParamMinMaxStep", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDParamMinMaxStep(IntPtr handle, CONTROL_ID controlid, ref double min, ref double max, ref double step);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDNumberOfReadModes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDNumberOfReadModes(IntPtr handle, ref uint num_modes);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDReadModeResolution", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDReadModeResolution(IntPtr handle, uint mode, ref uint width, ref uint height);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDReadModeName", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDReadModeName(IntPtr handle, uint mode, StringBuilder mode_name);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDReadMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDReadMode(IntPtr handle, ref uint mode);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDSDKVersion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDSDKVersion(ref uint year, ref uint month, ref uint day, ref uint subday);

        // These two methods are identical on the C side, they just have different pointer types (they're ABI compatible).
        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDSingleFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDSingleFrame(IntPtr handle, ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] byte[] rawArray);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDSingleFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDSingleFrame(IntPtr handle, ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] ushort[] rawArray);

        [DllImport(DLLNAME, EntryPoint = "InitQHYCCD", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint InitQHYCCD(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "InitQHYCCDResource", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint InitQHYCCDResource();

        [DllImport(DLLNAME, EntryPoint = "IsQHYCCDControlAvailable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint IsQHYCCDControlAvailable(IntPtr handle, CONTROL_ID controlid);

        [DllImport(DLLNAME, EntryPoint = "OpenQHYCCD", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe IntPtr OpenQHYCCD(StringBuilder id);

        [DllImport(DLLNAME, EntryPoint = "ReleaseQHYCCDResource", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint ReleaseQHYCCDResource();

        [DllImport(DLLNAME, EntryPoint = "ScanQHYCCD", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint ScanQHYCCD();

        [DllImport(DLLNAME, EntryPoint = "SendOrder2QHYCCDCFW", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SendOrder2QHYCCDCFW(IntPtr handle, string order, int length);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDBinMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDBinMode(IntPtr handle, uint wbin, uint hbin);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDBitsMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDBitsMode(IntPtr handle, uint bits);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDDebayerOnOff", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDDebayerOnOff(IntPtr handle, bool onoff);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDParam", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDParam(IntPtr handle, CONTROL_ID controlid, double value);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDReadMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDReadMode(IntPtr handle, uint mode);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDResolution", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDResolution(IntPtr handle, uint startx, uint starty, uint sizex, uint sizey);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDStreamMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDStreamMode(IntPtr handle, byte mode);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDFPGAVersion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDFPGAVersion(IntPtr handle, byte fpga_index, [Out] byte[] buf);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDHumidity", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDHumidity(IntPtr handle, ref double rh);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDPressure", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDPressure(IntPtr handle, ref double pressure);

        // Added for Live view/recording
        [DllImport(DLLNAME, EntryPoint = "BeginQHYCCDLive", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint BeginQHYCCDLive(IntPtr handle);

        // These two methods are identical on the C side, they just have different pointer types (they're ABI compatible).
        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDLiveFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDLiveFrame(IntPtr handle, ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] byte[] rawArray);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDLiveFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDLiveFrame(IntPtr handle, ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] ushort[] rawArray);

        [DllImport(DLLNAME, EntryPoint = "StopQHYCCDLive", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint StopQHYCCDLive(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDMemLength", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDMemLength(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDGPSVCOXFreq", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDGPSVCOXFreq(IntPtr handle, ushort freq);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDGPSLedCalMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDGPSLedCalMode(IntPtr handle, byte mode);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDGPSMasterSlave", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint SetQHYCCDGPSMasterSlave(IntPtr handle, byte mode);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDGPSPOSA", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe void SetQHYCCDGPSPOSA(IntPtr handle, uint pos, byte width);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDGPSPOSB", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe void SetQHYCCDGPSPOSB(IntPtr handle, uint pos, byte width);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDPreciseExposureInfo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDPreciseExposureInfo(IntPtr handle, ref uint pixelPeriod, ref uint linePeriod, ref uint framePeriod, ref uint clocksPerLine, ref uint linesPerFrame, ref uint actualExposureTime, ref byte isLongExposureMode);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDRollingShutterEndOffset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint GetQHYCCDRollingShutterEndOffset(IntPtr handle, uint row, ref double offset);

        #endregion DLL Imports

        #region NINA constants

        /// <summary>
        /// TEC delay loop
        /// </summary>
        public const int QHYCCD_COOLER_DELAY = 1000;

        /// <summary>
        /// TEC delay loop
        /// </summary>
        public const int QHYCCD_SENSORSTATS_DELAY = 10000;

        /// <summary>
        /// Length of QHY camera Id strings.
        /// This is the string returned by GetQHYCCDId() and comprises the camera's
        /// model number and serial number, currently not to exceed 32 characters in length.
        /// </summary>
        public const int QHYCCD_ID_LEN = 32;

        /// <summary>
        /// Values for tracking the current camera state within NINA
        /// </summary>
        public enum QHYCCD_CAMERA_STATE {
            IDLE = 0,
            EXPOSING,
            DOWNLOADING,
            ERROR
        };

        #endregion NINA constants

        #region Camera Information

        /// <summary>
        /// Structure for keeping camera instance information in NINA.
        /// </summary>
        public struct QHYCCD_CAMERA_INFO {

            /// <summary>
            /// Sensor bayer pattern
            /// <seealso cref="QHYCCDSDK.BAYER_ID"/>
            /// </summary>
            public BAYER_ID BayerPattern;

            /// <summary>
            /// Pixel bit depth
            /// </summary>
            public uint Bpp;

            /// <summary>
            /// The camera's current state (managed by NINA, not by SDK)
            /// <seealso cref="QHYCCDSDK.QHYCCD_CAMERA_STATE"/>
            /// </summary>
            public CameraStates CamState;

            /// <summary>
            /// Physical width of the sensor (in mm)
            /// </summary>
            public double ChipX;

            /// <summary>
            /// Physical height of the sensor (in mm)
            /// </summary>
            public double ChipY;

            /// <summary>
            /// TEC on/off status
            /// </summary>
            public bool CoolerOn;

            /// <summary>
            /// Maximum TEC duty cycle
            /// </summary>
            public double CoolerPwmMax;

            /// <summary>
            /// Minimum TEC duty cycle
            /// </summary>
            public double CoolerPwmMin;

            /// <summary>
            /// Minimum TEC duty cycle increment
            /// </summary>
            public double CoolerPwmStep;

            /// <summary>
            /// Desired TEC set point (Celcius)
            /// </summary>
            public double CoolerTargetTemp;

            /// <summary>
            /// Current camera bin mode
            /// </summary>
            public short CurBin;

            /// <summary>
            /// The currently active sensor array infomation
            /// </summary>
            public QHYCCD_SENSOR_AREA CurImage;

            /// <summary>
            /// The sensor's full dimensions
            /// </summary>
            public QHYCCD_SENSOR_AREA FullArea;

            /// <summary>
            /// The sensor's Effective Area dimensions
            /// </summary>
            public QHYCCD_SENSOR_AREA EffectiveArea;

            /// <summary>
            /// Maximum shutter speed
            /// </summary>
            public double ExpMax;

            /// <summary>
            /// Minimum shutter speed
            /// </summary>
            public double ExpMin;

            /// <summary>
            /// Minimum shutter speed increment
            /// </summary>
            public double ExpStep;

            /// <summary>
            /// Does SDK misreport the gain?
            /// <seealso cref="NINA.Model.MyCamera.QHYCamera.QuirkUnreliableGain"/>
            /// </summary>
            public bool HasUnreliableGain;

            /// <summary>
            /// Maximum sensor gain
            /// </summary>
            public int GainMax;

            /// <summary>
            /// Minimum sensor gain
            /// </summary>
            public int GainMin;

            /// <summary>
            /// Minimum sensor gain increment
            /// </summary>
            public double GainStep;

            /// <summary>
            /// Internally-stored gain setting
            /// <seealso cref="NINA.Model.MyCamera.QHYCamera.QuirkUnreliableGain"/>
            /// </summary>
            public double CurGain;

            /// <summary>
            /// Camera has temperature sensor?
            /// </summary>
            public bool HasChipTemp;

            /// <summary>
            /// Camera has active cooling?
            /// </summary>
            public bool HasCooler;

            /// <summary>
            /// Sensor gain can be set?
            /// </summary>
            public bool HasGain;

            /// <summary>
            /// Sensor offset can be set?
            /// </summary>
            public bool HasOffset;

            /// <summary>
            /// Camera has readout speeds?
            /// </summary>
            public bool HasReadoutSpeed;

            /// <summary>
            /// Camera has mechanical shutter?
            /// </summary>
            public bool HasShutter;

            /// <summary>
            /// Camera capable of governing USB bandwidth?
            /// </summary>
            public bool HasUSBTraffic;

            /// <summary>
            /// The camera's model name, including unique identifier
            /// </summary>
            public StringBuilder Id;

            /// <summary>
            /// Image array size (bytes)
            /// </summary>
            public uint ImageSize;

            /// <summary>
            /// Camera index number
            /// </summary>
            public uint Index;

            /// <summary>
            /// Does SDK misreport the offset?
            /// <seealso cref="NINA.Model.MyCamera.QHYCamera.QuirkInflatedOffset"/>
            /// </summary>
            public int InflatedOff;

            /// <summary>
            /// Is a color sensor?
            /// </summary>
            public bool IsColorCam;

            /// <summary>
            /// The camera's model name
            /// </summary>
            public StringBuilder Model;

            /// <summary>
            /// Maximum sensor offset
            /// </summary>
            public int OffMax;

            /// <summary>
            /// Minimum sensor offset
            /// </summary>
            public int OffMin;

            /// <summary>
            /// Minimum sensor offset increment
            /// </summary>
            public double OffStep;

            /// <summary>
            /// Physical pixel width (microns)
            /// </summary>
            public double PixelX;

            /// <summary>
            /// Physical pixel height (microns)
            /// </summary>
            public double PixelY;

            /// <summary>
            /// List of readout mode names
            /// </summary>
            public IList<string> ReadoutModes;

            /// <summary>
            /// Maximum readout speed
            /// </summary>
            public uint ReadoutSpeedMax;

            /// <summary>
            /// Minimum readout speed
            /// </summary>
            public uint ReadoutSpeedMin;

            /// <summary>
            /// Minimum readout speed increment
            /// </summary>
            public uint ReadoutSpeedStep;

            /// <summary>
            /// List of support bin modes
            /// </summary>
            public List<int> SupportedBins;

            /// <summary>
            /// Maximum USB bandwidth
            /// </summary>
            public double USBMax;

            /// <summary>
            /// Minimum USB bandwidth
            /// </summary>
            public double USBMin;

            /// <summary>
            /// Minimum USB bandwidth increment
            /// </summary>
            public double USBStep;

            /// <summary>
            /// Camera firmware version
            /// </summary>
            public string FirmwareVersion;

            /// <summary>
            /// Camera FPGA version
            /// </summary>
            public string FPGAVersion;

            /// <summary>
            /// QHY SDK version
            /// </summary>
            public string SdkVersion;

            /// <summary>
            /// QHY USB driver version
            /// </summary>
            public string UsbDriverVersion;

            /// <summary>
            /// Has a sensor chamber air pressure sensor?
            /// </summary>
            public bool HasSensorAirPressure;

            /// <summary>
            /// Sensor chamber air pressure
            /// </summary>
            public double SensorAirPressure;

            /// <summary>
            /// Has a sensor chamber humidity sensor?
            /// </summary>
            public bool HasSensorHumidity;

            /// <summary>
            /// Sensor chamber humidity
            /// </summary>
            public double SensorHumidity;
        };

        #endregion Camera Information

        #region Sensor Dimension Information

        /// <summary>
        /// Information about the sensor's resolution
        /// </summary>
        public struct QHYCCD_SENSOR_AREA {

            /// <summary>
            /// Origin pixel X
            /// </summary>
            public uint StartX;

            /// <summary>
            /// Origin pixel Y
            /// </summary>
            public uint StartY;

            /// <summary>
            /// Array size in pixels (X axis)
            /// </summary>
            public uint SizeX;

            /// <summary>
            /// Array size in pixels (Y axis)
            /// </summary>
            public uint SizeY;
        }

        #endregion Sensor Dimension Information

        #region Filter Wheel Information

        /// <summary>
        /// Structure for keeping filter wheel instance information
        /// </summary>
        public struct QHYCCD_FILTER_WHEEL_INFO {

            /// <summary>
            /// The filter wheel's unique name
            /// </summary>
            public StringBuilder Id;

            /// <summary>
            /// The filter wheel's full name, including camera model name
            /// </summary>
            public string Name;

            /// <summary>
            /// The number of postions the filter wheel has
            /// </summary>
            public uint Positions;
        }

        #endregion Filter Wheel Information
    }
}