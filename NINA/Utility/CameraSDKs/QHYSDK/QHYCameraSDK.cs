#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright (c) 2019 Dale Ghent <daleg@elemental.org> All rights reserved.
 */

#endregion "copyright"

using NINA.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace QHYCCD {

    public static class LibQHYCCD {
        private const string DLLNAME = "qhyccd.dll";

        static LibQHYCCD() {
            DllLoader.LoadDll(Path.Combine("QHYCCD", DLLNAME));

            N_InitQHYCCDResource();
        }

        #region "QHY SDK constants"

        /// <summary>
        /// SDK Return code: Error
        /// </summary>
        public const uint QHYCCD_ERROR = 0xFFFFFFFF;

        /// <summary>
        /// SDK eturn code: Success
        /// </summary>
        public const uint QHYCCD_SUCCESS = 0;

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
            DDR_BUFFER_READ_THRESHOLD
        };

        /// <summary>
        /// For setting QHY camera exposure mode
        /// <seealso cref="LibQHYCCD.SetQHYCCDStreamMode(IntPtr, byte)"/>
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

        #endregion "QHY SDK constants"

        #region "NINA constants"

        /// <summary>
        /// TEC delay loop
        /// </summary>
        public const int QHYCCD_COOLER_DELAY = 1000;

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

        #endregion "NINA constants"

        private static void CheckReturn(uint code, MethodBase callingMethod, params object[] parameters) {
            switch (code) {
                case LibQHYCCD.QHYCCD_SUCCESS:
                    break;

                case LibQHYCCD.QHYCCD_ERROR:
                    throw new QHYCameraException("QHY SDK returned an error status", callingMethod, parameters);
                default:
                    throw new ArgumentOutOfRangeException("QHY SDK returned an unknown error");
            }
        }

        [DllImport(DLLNAME, EntryPoint = "BeginQHYCCDLive", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint BeginQHYCCDLive(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "CancelQHYCCDExposing", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint CancelQHYCCDExposing(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "CancelQHYCCDExposingAndReadout", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint CancelQHYCCDExposingAndReadout(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "CloseQHYCCD", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint CloseQHYCCD(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "ControlQHYCCDGuide", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint ControlQHYCCDGuide(IntPtr handle, byte Direction, UInt16 PulseTime);

        [DllImport(DLLNAME, EntryPoint = "ControlQHYCCDShutter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint ControlQHYCCDShutter(IntPtr handle, byte targettemp);

        [DllImport(DLLNAME, EntryPoint = "ControlQHYCCDTemp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint ControlQHYCCDTemp(IntPtr handle, double targettemp);

        [DllImport(DLLNAME, EntryPoint = "ExpQHYCCDSingleFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint ExpQHYCCDSingleFrame(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "IsQHYCCDCFWPlugged", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint IsQHYCCDCFWPlugged(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDCFWStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDCFWStatus(IntPtr handle, [In, Out] byte[] cfwStatus);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDCameraStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDCameraStatus(IntPtr handle, [In, Out] byte status);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDChipInfo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDChipInfo(IntPtr handle, ref double chipw, ref double chiph, ref uint imagew, ref uint imageh, ref double pixelw, ref double pixelh, ref uint bpp);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDEffectiveArea", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDEffectiveArea(IntPtr handle, ref uint startx, ref uint starty, ref uint sizex, ref uint sizey);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDExposureRemaining", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDExposureRemaining(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDFWVersion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDFWVersion(IntPtr handle, [Out] byte[] verBuf);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDId", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDId(uint index, StringBuilder id);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDLiveFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDLiveFrame(IntPtr handle, ref uint w, ref uint h, ref uint bpp, ref uint channels, [Out] byte[] rawArray);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDMemLength", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDMemLength(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDModel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDModel(StringBuilder id, StringBuilder model);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDOverScanArea", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDOverScanArea(IntPtr handle, ref uint startx, ref uint starty, ref uint sizex, ref uint sizey);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDParam", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern double GetQHYCCDParam(IntPtr handle, CONTROL_ID controlid);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDParamMinMaxStep", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDParamMinMaxStep(IntPtr handle, CONTROL_ID controlid, ref double min, ref double max, ref double step);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDNumberOfReadModes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDNumberOfReadModes(IntPtr handle, ref uint num_modes);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDReadModeName", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDReadModeName(IntPtr handle, uint mode, StringBuilder mode_name);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDReadMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDReadMode(IntPtr handle, ref uint mode);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDSDKVersion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDSDKVersion(ref uint year, ref uint month, ref uint day, ref uint subday);

        // These two methods are identical on the C side, they just have different pointer types (they're ABI compatible).
        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDSingleFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDSingleFrame(IntPtr handle, ref uint w, ref uint h, ref uint bpp, ref uint channels, [Out] byte[] rawArray);

        [DllImport(DLLNAME, EntryPoint = "GetQHYCCDSingleFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint GetQHYCCDSingleFrame(IntPtr handle, ref uint w, ref uint h, ref uint bpp, ref uint channels, [Out] ushort[] rawArray);

        public static string GetSDKFormattedVersion() {
            uint year = 0, month = 0, day = 0, subday = 0;
            CheckReturn(GetQHYCCDSDKVersion(ref year, ref month, ref day, ref subday), MethodBase.GetCurrentMethod());

            return "20" + year.ToString("D2") + month.ToString("D2") + day.ToString("D2") + "_" + subday.ToString();
        }

        [DllImport(DLLNAME, EntryPoint = "InitQHYCCD", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint InitQHYCCD(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "InitQHYCCDResource", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint InitQHYCCDResource();

        [DllImport(DLLNAME, EntryPoint = "IsQHYCCDControlAvailable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint IsQHYCCDControlAvailable(IntPtr handle, CONTROL_ID controlid);

        public static void N_CloseQHYCCD(IntPtr handle) {
            CheckReturn(CloseQHYCCD(handle), MethodBase.GetCurrentMethod(), handle);
        }

        public static void N_GetQHYCCDId(uint index, StringBuilder id) {
            CheckReturn(GetQHYCCDId(index, id), MethodBase.GetCurrentMethod(), index, new object[] { id });
        }

        public static void N_GetQHYCCDModel(StringBuilder id, StringBuilder model) {
            CheckReturn(GetQHYCCDModel(id, model), MethodBase.GetCurrentMethod(), new object[] { model });
        }

        public static void N_InitQHYCCD(IntPtr handle) {
            CheckReturn(InitQHYCCD(handle), MethodBase.GetCurrentMethod(), handle);
        }

        public static void N_InitQHYCCDResource() {
            CheckReturn(InitQHYCCDResource(), MethodBase.GetCurrentMethod());
        }

        public static IntPtr N_OpenQHYCCD(StringBuilder id) {
            IntPtr cameraP = OpenQHYCCD(id);

            if (cameraP == IntPtr.Zero) {
                throw new QHYCameraException("Unable to open camera", MethodBase.GetCurrentMethod(), new object[] { id });
            }
            return cameraP;
        }

        public static void N_ReleaseQHYCCDResource() {
            CheckReturn(ReleaseQHYCCDResource(), MethodBase.GetCurrentMethod());
        }

        [DllImport(DLLNAME, EntryPoint = "OpenQHYCCD", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern IntPtr OpenQHYCCD(StringBuilder id);

        [DllImport(DLLNAME, EntryPoint = "ReleaseQHYCCDResource", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint ReleaseQHYCCDResource();

        [DllImport(DLLNAME, EntryPoint = "StopQHYCCDLive", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint StopQHYCCDLive(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "ScanQHYCCD", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint ScanQHYCCD();

        [DllImport(DLLNAME, EntryPoint = "SendOrder2QHYCCDCFW", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SendOrder2QHYCCDCFW(IntPtr handle, String order, int length);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDBinMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SetQHYCCDBinMode(IntPtr handle, uint wbin, uint hbin);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDBitsMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SetQHYCCDBitsMode(IntPtr handle, uint bits);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDDebayerOnOff", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SetQHYCCDDebayerOnOff(IntPtr handle, bool onoff);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDLogLevel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SetQHYCCDLogLevel(byte level);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDParam", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SetQHYCCDParam(IntPtr handle, CONTROL_ID controlid, double value);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDReadMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SetQHYCCDReadMode(IntPtr handle, uint mode);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDResolution", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SetQHYCCDResolution(IntPtr handle, uint startx, uint starty, uint sizex, uint sizey);

        [DllImport(DLLNAME, EntryPoint = "SetQHYCCDStreamMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public unsafe static extern uint SetQHYCCDStreamMode(IntPtr handle, byte mode);

        /// <summary>
        /// Structure for keeping camera instance information in NINA.
        /// </summary>
        public struct QHYCCD_CAMERA_INFO {

            /// <summary>
            /// Sensor bayer pattern
            /// <seealso cref="LibQHYCCD.BAYER_ID"/>
            /// </summary>
            public BAYER_ID BayerPattern;

            /// <summary>
            /// Pixel bit depth
            /// </summary>
            public uint Bpp;

            /// <summary>
            /// The camera's current state (managed by NINA, not by SDK)
            /// <seealso cref="LibQHYCCD.QHYCCD_CAMERA_STATE"/>
            /// </summary>
            public string CamState;

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
            /// The sensor's claimed Effective Area
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
            /// Sensor has an overscan area?
            /// </summary>
            public bool HasOverscan;

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
            /// Maximum image width (pixels)
            /// </summary>
            public uint ImageX;

            /// <summary>
            /// Maximum image height (pixels)
            /// </summary>
            public uint ImageY;

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
            public List<string> ReadoutModes;

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
        };

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
    }
}
