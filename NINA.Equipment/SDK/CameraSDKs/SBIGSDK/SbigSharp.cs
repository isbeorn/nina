#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp {

    public static class SBIG {

        static SBIG() {
            Logger.Trace($"SBIGSDK: Loading SBIG SDK dll");
            DllLoader.LoadDll(Path.Combine("SBIG", DLLNAME));
        }

        //

        #region Constants

        //
        private const string DLLNAME = "SBIGUDrv.dll";

        private static GCHandle NullGch = new GCHandle();

        #endregion

        //

        #region Enums

        //

        public enum Cmd : ushort {
            /*
                General Use Commands
            */
            CC_NULL = 0,

            /* 1 - 10 */
            CC_START_EXPOSURE = 1,
            CC_END_EXPOSURE,
            CC_READOUT_LINE,
            CC_DUMP_LINES,
            CC_SET_TEMPERATURE_REGULATION,
            CC_QUERY_TEMPERATURE_STATUS,
            CC_ACTIVATE_RELAY,
            CC_PULSE_OUT,
            CC_ESTABLISH_LINK,
            CC_GET_DRIVER_INFO,

            /* 11 - 20 */
            CC_GET_CCD_INFO,
            CC_QUERY_COMMAND_STATUS,
            CC_MISCELLANEOUS_CONTROL,
            CC_READ_SUBTRACT_LINE,
            CC_UPDATE_CLOCK,
            CC_READ_OFFSET,
            CC_OPEN_DRIVER,
            CC_CLOSE_DRIVER,
            CC_TX_SERIAL_BYTES,
            CC_GET_SERIAL_STATUS,

            /* 21 - 30 */
            CC_AO_TIP_TILT,
            CC_AO_SET_FOCUS,
            CC_AO_DELAY,
            CC_GET_TURBO_STATUS,
            CC_END_READOUT,
            CC_GET_US_TIMER,
            CC_OPEN_DEVICE,
            CC_CLOSE_DEVICE,
            CC_SET_IRQL,
            CC_GET_IRQL,

            /* 31 - 40 */
            CC_GET_LINE,
            CC_GET_LINK_STATUS,
            CC_GET_DRIVER_HANDLE,
            CC_SET_DRIVER_HANDLE,
            CC_START_READOUT,
            CC_GET_ERROR_STRING,
            CC_SET_DRIVER_CONTROL,
            CC_GET_DRIVER_CONTROL,
            CC_USB_AD_CONTROL,
            CC_QUERY_USB,

            /* 41 - 50 */
            CC_GET_PENTIUM_CYCLE_COUNT,
            CC_RW_USB_I2C,
            CC_CFW,
            CC_BIT_IO,
            CC_USER_EEPROM,
            CC_AO_CENTER,
            CC_BTDI_SETUP,
            CC_MOTOR_FOCUS,
            CC_QUERY_ETHERNET,
            CC_START_EXPOSURE2,

            /* 51 - 60 */
            CC_SET_TEMPERATURE_REGULATION2,
            CC_READ_OFFSET2,
            CC_DIFF_GUIDER,
            CC_COLUMN_EEPROM,
            CC_CUSTOMER_OPTIONS,
            CC_DEBUG_LOG,
            CC_QUERY_USB2,
            CC_QUERY_ETHERNET2,
            CC_GET_AO_MODEL,

            /*
                SBIG Use Only Commands
            */

            /* 90 - 99 */
            CC_SEND_BLOCK = 90,
            CC_SEND_BYTE,
            CC_GET_BYTE,
            CC_SEND_AD,
            CC_GET_AD,
            CC_CLOCK_AD,
            CC_SYSTEM_TEST,
            CC_GET_DRIVER_OPTIONS,
            CC_SET_DRIVER_OPTIONS,
            CC_FIRMWARE,

            /* 100 -109 */
            CC_BULK_IO,
            CC_RIPPLE_CORRECTION,
            CC_EZUSB_RESET,
            CC_BREAKPOINT,
            CC_QUERY_EXPOSURE_TICKS,
            CC_SET_ACTIVE_CCD_AREA,

            CC_LAST_COMMAND
        } // enum Cmd

        public enum Error : ushort {
            /* 0 - 10 */
            CE_NO_ERROR = 0,
            CE_CAMERA_NOT_FOUND = 1,
            CE_EXPOSURE_IN_PROGRESS,
            CE_NO_EXPOSURE_IN_PROGRESS,
            CE_UNKNOWN_COMMAND,
            CE_BAD_CAMERA_COMMAND,
            CE_BAD_PARAMETER,
            CE_TX_TIMEOUT,
            CE_RX_TIMEOUT,
            CE_NAK_RECEIVED,
            CE_CAN_RECEIVED,

            /* 11 - 20 */
            CE_UNKNOWN_RESPONSE,
            CE_BAD_LENGTH,
            CE_AD_TIMEOUT,
            CE_KBD_ESC,
            CE_CHECKSUM_ERROR,
            CE_EEPROM_ERROR,
            CE_SHUTTER_ERROR,
            CE_UNKNOWN_CAMERA,
            CE_DRIVER_NOT_FOUND,
            CE_DRIVER_NOT_OPEN,

            /* 21 - 30 */
            CE_DRIVER_NOT_CLOSED,
            CE_SHARE_ERROR,
            CE_TCE_NOT_FOUND,
            CE_AO_ERROR,
            CE_ECP_ERROR,
            CE_MEMORY_ERROR,
            CE_DEVICE_NOT_FOUND,
            CE_DEVICE_NOT_OPEN,
            CE_DEVICE_NOT_CLOSED,
            CE_DEVICE_NOT_IMPLEMENTED,

            /* 31 - 40 */
            CE_DEVICE_DISABLED,
            CE_OS_ERROR,
            CE_SOCK_ERROR,
            CE_SERVER_NOT_FOUND,
            CE_CFW_ERROR,
            CE_MF_ERROR,
            CE_FIRMWARE_ERROR,
            CE_DIFF_GUIDER_ERROR,
            CE_RIPPLE_CORRECTION_ERROR,
            CE_EZUSB_RESET,

            /* 41 - 50*/
            CE_NEXT_ERROR
        } // enum Error

        public enum DeviceType : ushort {
            LPT1 = 1,
            LPT2 = 2,
            LPT3 = 3,
            Ethernet = 0x7F01,
            USB1 = 0x7F02,
            USB2 = 0x7F03,
            USB3 = 0x7F04,
            USB4 = 0x7F05,
            USB5 = 0x7F06,
            USB6 = 0x7F07,
            USB7 = 0x7F08,
            USB8 = 0x7F09
        }

        public enum CCD : ushort {
            Imaging = 0,
            Tracking = 1,
            ExternalTrackingInStxOrStl = 2
        }

        public enum CcdInfoRequest : ushort {
            ImagingCcdStandard = 0,
            TrackingCcdStandard,
            CameraInfoExtended,
            PixCel255_237Extended,
            ImagingCcdSecondaryExtended,
            TrackingCcdSecondaryExtended,
            CcdCameraExtended
        }

        public enum AbgState : ushort {
            Off = 0,
            Low,
            Med,
            High
        }

        public enum ShutterState : ushort {
            Unchanged = 0,
            Open,
            Close,
            Reinitialize,
            OpenStlExternal,
            CloseStlExternal
        }

        public enum CameraType : ushort {
            ST_7 = 4,
            ST_8,
            ST_5C,
            TCE,
            ST_237,
            ST_K,
            ST_9,
            STV,
            ST_10,
            ST_1K,
            ST_2K,
            STL,
            ST_402,
            STX,
            ST_4K,
            STT,
            ST_i,
            STF_8300,
            NoCamera = 0xFFFF
        }

        public enum LedState : ushort {
            Off = 0,
            On,
            BlinkLow,
            BlinkHigh
        }

        public enum DriverControlParam : ushort {
            DCP_USB_FIFO_ENABLE,
            DCP_CALL_JOURNAL_ENABLE,
            DCP_IVTOH_RATIO,
            DCP_USB_FIFO_SIZE,
            DCP_USB_DRIVER,
            DCP_KAI_RELGAIN,
            DCP_USB_PIXEL_DL_ENABLE,
            DCP_HIGH_THROUGHPUT,
            DCP_VDD_OPTIMIZED,
            DCP_AUTO_AD_GAIN,
            DCP_NO_HCLKS_FOR_INTEGRATION,
            DCP_TDI_MODE_ENABLE,
            DCP_VERT_FLUSH_CONTROL_ENABLE,
            DCP_ETHERNET_PIPELINE_ENABLE,
            DCP_FAST_LINK,
            DCP_OVERSCAN_ROWSCOLS,
            DCP_PIXEL_PIPELINE_ENABLE,
            DCP_COLUMN_REPAIR_ENABLE,
            DCP_WARM_PIXEL_REPAIR_ENABLE,
            DCP_WARM_PIXEL_REPAIR_COUNT,
            DCP_LAST
        }

        public enum TempStatusRequest : ushort {
            TEMP_STATUS_STANDARD,
            TEMP_STATUS_ADVANCED,
            TEMP_STATUS_ADVANCED2
        }

        public enum A2dSize : ushort {
            Unknown = 0,
            TwelveBits = 1,
            SixteenBits = 2
        }

        public enum FilterType : ushort {
            Unknown = 0,
            External = 1,
            TwoPosition = 2,
            FivePosition = 3
        }

        public enum ReadoutMode : ushort {
            NoBinning = 0,
            Bin2x2 = 1,
            Bin3x3 = 2,
            BinNx1 = 3, // N pixels of vertical binning, specified in high byte
            BinNx2 = 4, // N pixels of vertical binning, specified in high byte
            BinNx3 = 5, // N pixels of vertical binning, specified in high byte
            NoBinning2 = 6,
            Bin2x2VertOffChip = 7,
            Bin3x3VertOffChip = 8,
            Bin9x9 = 9,
            BinNxN = 10, // N specified in high byte, STF-8300 only
        }

        public enum AntiBloomingGatePresence : ushort {
            NoABG = 0,
            HasABG = 1
        }

        public enum TemperatureRegulation : ushort {
            Off = 0,
            On,
            Override,
            Freeze,
            Unfreeze,
            AutoFreeze,
            DisableAutoFreeze
        }

        public enum CfwModelSelect : ushort {
            CFWSEL_UNKNOWN = 0,         /*!< Unknown Model	*/
            CFWSEL_CFW2,            /*!< CFW2			*/
            CFWSEL_CFW5,            /*!< CFW5			*/
            CFWSEL_CFW8,            /*!< CFW8			*/
            CFWSEL_CFWL,            /*!< CFWL			*/
            CFWSEL_CFW402,          /*!< CFW-402		*/
            CFWSEL_AUTO,            /*!< Auto			*/
            CFWSEL_CFW6A,           /*!< CFW-6A			*/
            CFWSEL_CFW10,           /*!< CFW10			*/
            CFWSEL_CFW10_SERIAL,    /*!< CFW10-Serial	*/
            CFWSEL_CFW9,            /*!< CFW9			*/
            CFWSEL_CFWL8,           /*!< CFWL8			*/
            CFWSEL_CFWL8G,          /*!< CFWL8-G		*/
            CFWSEL_CFW1603,         /*!< CFW1603		*/
            CFWSEL_FW5_STX,         /*!< FW5-STX		*/
            CFWSEL_FW5_8300,        /*!< FW5-8300		*/
            CFWSEL_FW8_8300,        /*!< FW8-8300		*/
            CFWSEL_FW7_STX,         /*!< FW7-STX		*/
            CFWSEL_FW8_STT,         /*!< FW8-STT		*/
            CFWSEL_FW5_STF_DETENT	/*!< FW5-STF Detent */
        }

        public enum CfwCommand : ushort {
            CFWC_QUERY = 0,         /*!< Query			*/
            CFWC_GOTO,          /*!< Go-to slot		*/
            CFWC_INIT,          /*!< Initialize		*/
            CFWC_GET_INFO,      /*!< Get Info		*/
            CFWC_OPEN_DEVICE,   /*!< Open device	*/
            CFWC_CLOSE_DEVICE	/*!< Close device	*/
        }

        public enum CfwStatus : ushort {
            CFWS_UNKNOWN = 0,   /*!< Unknown state	*/
            CFWS_IDLE,      /*!< Idle state		*/
            CFWS_BUSY		/*!< Busy state		*/
        }

        public enum CfwError : ushort {
            CFWE_NONE = 0,              /*!< No error					*/
            CFWE_BUSY,              /*!< Busy error					*/
            CFWE_BAD_COMMAND,       /*!< Bad command error			*/
            CFWE_CAL_ERROR,         /*!< Calibration error			*/
            CFWE_MOTOR_TIMEOUT,     /*!< Motor timeout error		*/
            CFWE_BAD_MODEL,         /*!< Bad model error			*/
            CFWE_DEVICE_NOT_CLOSED, /*!< Device not closed	error	*/
            CFWE_DEVICE_NOT_OPEN,   /*!< Device not open error		*/
            CFWE_I2C_ERROR			/*!< I2C communication error	*/
        }

        public enum CfwPosition : ushort {
            CFWP_UNKNOWN = 0,   /*!< Unknown	*/
            CFWP_1,         /*!< Slot 1		*/
            CFWP_2,         /*!< Slot 2		*/
            CFWP_3,         /*!< Slot 3		*/
            CFWP_4,         /*!< Slot 4		*/
            CFWP_5,         /*!< Slot 5		*/
            CFWP_6,         /*!< Slot 6		*/
            CFWP_7,         /*!< Slot 7		*/
            CFWP_8,         /*!< Slot 8		*/
            CFWP_9,         /*!< Slot 9		*/
            CFWP_10			/*!< Slot 10	*/
        }

        public enum CfwComPort : ushort {
            CFWPORT_COM1 = 1,   /*!< COM1	*/
            CFWPORT_COM2,   /*!< COM2	*/
            CFWPORT_COM3,   /*!< COM3	*/
            CFWPORT_COM4 	/*!< COM4	*/
        }

        public enum CfwGetInfoSelect : ushort {
            CFWG_FIRMWARE_VERSION = 0,  /*!< Firmware version	*/
            CFWG_CAL_DATA,          /*!< Calibration data	*/
            CFWG_DATA_REGISTERS		/*!< Data registers		*/
        }

        #endregion Enums

        //

        #region Types

        //

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class EstablishLinkParams {
            public ushort sbigUseOnly;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class EstablishLinkResults {
            public CameraType cameraType;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class StartExposureParams2 {
            public CCD ccd;
            public uint exposureTime;  //  integration time in hundredths of a second in the least significant 24 bits
            public AbgState abgState;
            public ShutterState openShutter;
            public ushort readoutMode;
            public ushort top;
            public ushort left;
            public ushort height;
            public ushort width;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class EndExposureParams {
            public CCD ccd;

            public EndExposureParams() : this(CCD.Imaging) {
            }

            public EndExposureParams(CCD ccd) {
                this.ccd = ccd;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class ReadoutLineParams {
            public CCD ccd;
            public ushort readoutMode;
            public ushort pixelStart;
            public ushort pixelLength;

            public static ushort MakeNBinMode(ReadoutMode rlp, ushort n) {
                // put the high byte in place, but only if it's one of those binning modes
                if (ReadoutMode.BinNx1 == rlp || ReadoutMode.BinNx2 == rlp || ReadoutMode.BinNx3 == rlp || ReadoutMode.BinNxN == rlp)
                    return (ushort)(((ushort)rlp) | (n << 8));
                else
                    return (ushort)rlp;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class QueryTemperatureStatusParams {
            public TempStatusRequest request;

            public QueryTemperatureStatusParams() : this(TempStatusRequest.TEMP_STATUS_STANDARD) {
            }

            public QueryTemperatureStatusParams(TempStatusRequest tsr) {
                request = tsr;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class QueryTemperatureStatusResults {
            public ushort enabled;
            public ushort ccdSetpoint;
            public ushort power;
            public ushort ccdThermistor;
            public ushort ambientThermistor;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class QueryTemperatureStatusResults2 {
            public ushort coolingEnabled;
            public ushort fanEnabled;
            public double ccdSetpoint;
            public double imagingCCDTemperature;
            public double trackingCCDTemperature;
            public double externalTrackingCCDTemperature;
            public double ambientTemperature;
            public double imagingCCDPower;
            public double trackingCCDPower;
            public double externalTrackingCCDPower;
            public double heatsinkTemperature;
            public double fanPower;
            public double fanSpeed;
            public double trackingCCDSetpoint;
        }

        /// <summary>
        /// controls the thermoelectric cooler, using old school A2D units (see docs)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class SetTemperatureRegulationParams {
            public TemperatureRegulation state;
            public ushort ccdSetpointA2dUnits;
        }

        /// <summary>
        /// controls the thermoelectric cooler in nice, simple degrees C
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class SetTemperatureRegulationParams2 {
            public TemperatureRegulation state;
            public double ccdSetpointCelcius;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class QueryCommandStatusParams {
            public Cmd command;

            public QueryCommandStatusParams() : this(Cmd.CC_NULL) {
            }

            public QueryCommandStatusParams(Cmd cmd) {
                command = cmd;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class QueryCommandStatusResults {
            public Error status;

            public QueryCommandStatusResults() : this(Error.CE_NO_ERROR) {
            }

            public QueryCommandStatusResults(Error e) {
                status = e;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class MiscellaneousControlParams {
            public ushort fanEnable;
            public ShutterState shutterCommand;
            public LedState ledState;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class OpenDeviceParams {
            public DeviceType deviceType;       /* LPT, Ethernet, etc */
            public ushort lptBaseAddress;       /* DEV_LPTN: Windows 9x Only, Win NT uses deviceSelect */
            public uint ipAddress;			    /* DEV_ETH:  Ethernet address, the most significant byte specifying the first part of the address */

            public OpenDeviceParams() {
                deviceType = (DeviceType)0;    // illegal value to make things fail fast if unintialized
                ipAddress = 0;
                lptBaseAddress = 0;             // either it's irrelvant or the OS will handle it
            }

            public OpenDeviceParams(string s) : this() {
                try {
                    // first, try to parse as an IP address
                    IPAddress ip = IPAddress.Parse(s);
                    byte[] b = ip.GetAddressBytes();
                    this.ipAddress = (((uint)b[0]) << 24) | (((uint)b[1]) << 16) | (((uint)b[2]) << 8) | ((uint)b[3]);
                    deviceType = DeviceType.Ethernet;
                } catch (FormatException) {
                    // if it's not an IP, it should be a string value of the enum
                    if (!Enum.TryParse<DeviceType>(s, true, out deviceType))
                        throw new ArgumentException("must pass either an IP address or valid DeviceType enum string");
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class SetDriverControlParams {
            public DriverControlParam controlParameter;
            public int controlValue;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct StartReadoutParams {
            public CCD ccd;
            public ushort readoutMode;
            public ushort top;
            public ushort left;
            public ushort height;
            public ushort width;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct UsbInfo {
            public ushort cameraFound;
            public bool CameraFound { get { return 0 != cameraFound; } }
            public CameraType cameraType;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string name;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            public string serialNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class QueryUsbResults {
            public ushort camerasFound;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public UsbInfo[] dev;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class GetCcdInfoParams {
            public CcdInfoRequest req;

            public GetCcdInfoParams(CcdInfoRequest req) {
                this.req = req;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct ReadoutInfo {
            public ushort mode;
            public ushort width;
            public ushort height;
            public ushort gain;        // amplifier gain in e-/ADU, e.g. 0x1234 = 12.34 e- per ADU

            // CHANGE: These are ulong in SbigSharp, but ulong represents 64-bits in .NET compared to 32-bits for unsigned long in C
            public uint pixelWidth;   // pixel width in microns in the form XXXXXX.XX

            public uint pixelHeight;  // pixel height in microns in the form XXXXXX.XX
        }

        /// <summary>
        /// Result structure for CC_GET_CCD_INFO request types 0 and 1
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class GetCcdInfoResults01 {
            public ushort firmwareVersion; // 0x1234 = v12.34
            public CameraType cameraType;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string name;

            public ushort readoutModeCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public ReadoutInfo[] readoutInfo;
        }

        /// <summary>
        /// Result structure for CC_GET_CCD_INFO request type 2
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class GetCcdInfoResults2 {
            public ushort badColumns;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] columns;

            public AntiBloomingGatePresence imagingABG; // 0 = no ABG, 1 = Anti-Blooming Gate protection

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            public string serialNumber;
        }

        /// <summary>
        /// Result structure for CC_GET_CCD_INFO request type 3
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class GetCcdInfoResults3 {
            public A2dSize a2dSize;
            public FilterType filterType;
        }

        /// <summary>
        /// Result structure for CC_GET_CCD_INFO request type 4 and 5
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class GetCcdInfoResults45 {
            public ushort capabilitiesBits;
            public ushort dumpExtra;
        }

        /// <summary>
        /// Result structure for CC_GET_CCD_INFO request type 6
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class GetCcdInfoResults6 {
            public uint cameraBits;
            public uint ccdBits;
            public uint extraBits;
        }

        /// <summary>
        /// Input structure for CC_ACTIVATE_RELAY command to control telescope mount
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class ActivateRelayParams {
            public ushort tXPlus;
            public ushort tXMinus;
            public ushort tYPlus;
            public ushort tYMinus;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class SetDriverHandleParams {
            public short handle;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class GetDriverHandleResults {
            public short handle;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class CfwParams {
            public CfwModelSelect cfwModel;
            public CfwCommand cfwCommand;
            public uint cfwParam1;
            public uint cfwParam2;

            public ushort outLength = 0;
            public UIntPtr outPtr = UIntPtr.Zero;
            public ushort inLength = 0;
            public UIntPtr inPtr = UIntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public class CfwResult {
            public CfwModelSelect cfwModel;
            public CfwPosition cfwPosition;
            public CfwStatus cfwStatus;
            public CfwError cfwError;
            public uint cfwResult1;
            public uint cfwResult2;
        }

        /// <summary>
        /// gets thrown whenever an SBIG operation doesn't return success (CE_NO_ERROR)
        /// </summary>
        public class FailedOperation : Exception {
            public Error errorcode;

            public FailedOperation(Error errorcode) {
                this.errorcode = errorcode;
            }

            public override string Message {
                get {
                    return errorcode.ToString();
                }
            }
        } // class FailedOperation

        #endregion Types

        //

        #region Methods

        //

        /// <summary>
        /// Direct pass-through to SBIG Universal Driver
        /// </summary>
        /// <param name="Command">the command to be executed</param>
        /// <param name="Parameters">inputs to the operation, if any</param>
        /// <param name="Results">outputs from the operation, if any</param>
        /// <returns>error code from Error enum (e.g. CE_CAMERA_NOT_FOUND)</returns>
        [DllImport(DLLNAME)]
        private static extern Error SBIGUnivDrvCommand(Cmd Command, IntPtr Parameters, IntPtr Results);

        /// <summary>
        /// Gets the file version of the SBIG SDK dll
        /// </summary>
        /// <returns>The file version information</returns>
        public static FileVersionInfo GetVersion() {
            return DllLoader.DllVersion(Path.Combine("SBIG", DLLNAME));
        }

        /// <summary>
        /// Exposes the direct pass-through to SBIG Universal Driver
        /// </summary>
        /// <param name="Command">the command to be executed</param>
        /// <param name="Parameters">inputs to the operation, if any</param>
        /// <param name="Results">outputs from the operation, if any</param>
        /// <returns>error code from Error enum (e.g. CE_CAMERA_NOT_FOUND)</returns>
        public static void UnivDrvCommandDirect(Cmd Command, IntPtr Parameters, IntPtr Results) {
            Error err = SBIGUnivDrvCommand(Command, Parameters, Results);
            if (Error.CE_NO_ERROR != err)
                throw new FailedOperation(err);
        }

        /// <summary>
        /// Calls the SBIG Universal Driver with a (possibly null) input parameter struct
        /// </summary>
        /// <param name="Command">the command to be executed</param>
        /// <param name="Parameters">inputs to the operation, null if none</param>
        /// <param name="Results">Pointer to the output buffer.</param>
        /// <exception cref="FailedOperation">throws a FailedOperation exception if command doesn't return CE_NO_ERROR</exception>
        public static void UnivDrvCommand(Cmd Command, object Parameters) {
            // marshall the input structure, if it exists
            GCHandle ParamGch = NullGch;
            IntPtr ParamPtr = IntPtr.Zero;
            if (null != Parameters) {
                ParamGch = GCHandle.Alloc(Parameters, GCHandleType.Pinned);
                ParamPtr = ParamGch.AddrOfPinnedObject();
            }

            //
            // make the call
            //
            Error err = SBIGUnivDrvCommand(Command, ParamPtr, IntPtr.Zero);
            if (Error.CE_NO_ERROR != err)
                throw new FailedOperation(err);

            // clean up
            if (IntPtr.Zero != ParamPtr)
                ParamGch.Free();
        }

        /// <summary>
        /// Calls the SBIG Universal Driver with a (possibly null) input parameter struct DIRECTLY WRITING output into Results
        /// </summary>
        /// <param name="Command">the command to be executed</param>
        /// <param name="Parameters">inputs to the operation, null if none</param>
        /// <param name="Results">array or structure to write command output DIRECTLY into (no marshalling occurs)</param>
        /// <exception cref="FailedOperation">throws a FailedOperation exception if command doesn't return CE_NO_ERROR</exception>
        public static void UnivDrvCommand(Cmd Command, object Parameters, object Results) {
            // marshall the input structure, if it exists
            GCHandle ParamGch = NullGch;
            IntPtr ParamPtr = IntPtr.Zero;
            if (null != Parameters) {
                ParamGch = GCHandle.Alloc(Parameters, GCHandleType.Pinned);
                ParamPtr = ParamGch.AddrOfPinnedObject();
            }
            // pin the output bytes while we pass the buffer to the SBIG SDK
            GCHandle ResultsGch = GCHandle.Alloc(Results, GCHandleType.Pinned);

            //
            // make the call
            //
            Error err = SBIGUnivDrvCommand(Command, ParamPtr, ResultsGch.AddrOfPinnedObject());
            if (Error.CE_NO_ERROR != err)
                throw new FailedOperation(err);

            // clean up
            ResultsGch.Free();
            if (IntPtr.Zero != ParamPtr)
                ParamGch.Free();
        }

        /// <summary>
        /// Calls the SBIG Universal Driver, MARSHALLING the input parameter struct to do any necessary type translation
        /// Only use this version only when type translation is required (as it's slower)
        /// </summary>
        /// <param name="Command">the command to be executed</param>
        /// <param name="Parameters">inputs to the operation, null if none</param>
        /// <exception cref="FailedOperation">throws a FailedOperation exception if command doesn't return CE_NO_ERROR</exception>
        public static void UnivDrvCommandMarshal(Cmd Command, object Parameters) {
            // marshall the input structure, if it exists
            IntPtr ParamPtr = IntPtr.Zero;
            if (null != Parameters) {
                ParamPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Parameters));
                Marshal.StructureToPtr(Parameters, ParamPtr, false);
            }

            //
            // make the call
            //
            Error err = SBIGUnivDrvCommand(Command, ParamPtr, IntPtr.Zero);
            if (Error.CE_NO_ERROR != err)
                throw new FailedOperation(err);

            // clean up
            if (IntPtr.Zero != ParamPtr)
                Marshal.FreeHGlobal(ParamPtr);
        }

        /// <summary>
        /// Calls the SBIG Universal Driver with a (possibly null) input parameter struct DIRECTLY WRITING output into return value
        /// </summary>
        /// <param name="Command">the command to be executed</param>
        /// <param name="Parameters">inputs to the operation, null if none</param>
        /// <returns>object with command output written DIRECTLY into it (no marshalling occurs)</returns>
        /// <exception cref="FailedOperation">throws a FailedOperation exception if command doesn't return CE_NO_ERROR</exception>
        public static T UnivDrvCommand<T>(Cmd Command, object Parameters) where T : new() {
            // marshall the input structure, if it exists
            GCHandle ParamGch = NullGch;
            IntPtr ParamPtr = IntPtr.Zero;
            if (null != Parameters) {
                ParamGch = GCHandle.Alloc(Parameters, GCHandleType.Pinned);
                ParamPtr = ParamGch.AddrOfPinnedObject();
            }
            // prepare the output structure and pin it while we pass it to the SBIG SDK
            T Results = new T();
            GCHandle ResultsGch = GCHandle.Alloc(Results, GCHandleType.Pinned);
            IntPtr ResultsPtr = ResultsGch.AddrOfPinnedObject();

            //
            // make the call
            //
            Error err = SBIGUnivDrvCommand(Command, ParamPtr, ResultsPtr);
            if (Error.CE_NO_ERROR != err)
                throw new FailedOperation(err);

            // clean up
            ResultsGch.Free();
            if (IntPtr.Zero != ParamPtr)
                ParamGch.Free();

            return Results;
        }

        /// <summary>
        /// Calls the SBIG Universal Driver with a (possibly null) input parameter struct MARSHALLING the output into return value
        /// Only use this version only when type translation is required (as it's slower)
        /// </summary>
        /// <param name="Command">the command to be executed</param>
        /// <param name="Parameters">inputs to the operation, null if none</param>
        /// <returns>object with command output MARSHALLED into it (types are translated as necessary)</returns>
        /// <exception cref="FailedOperation">throws a FailedOperation exception if command doesn't return CE_NO_ERROR</exception>
        public static T UnivDrvCommandMarshal<T>(Cmd Command, object Parameters) {
            // marshall the input structure, if it exists
            IntPtr ParamPtr = IntPtr.Zero;
            if (null != Parameters) {
                ParamPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Parameters));
                Marshal.StructureToPtr(Parameters, ParamPtr, false);
            }
            // marshall the output structure
            IntPtr ResultsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));

            //
            // make the call
            //
            Error err = SBIGUnivDrvCommand(Command, ParamPtr, ResultsPtr);
            if (Error.CE_NO_ERROR != err)
                throw new FailedOperation(err);

            // un-marshal the output
            T Results = (T)Marshal.PtrToStructure(ResultsPtr, typeof(T));
            Marshal.FreeHGlobal(ResultsPtr);

            // clean up
            if (IntPtr.Zero != ParamPtr)
                Marshal.FreeHGlobal(ParamPtr);

            return Results;
        }

        /// <summary>
        /// calls UnivDrvCommand while (possibly taking in, but definitely) marshalling out a complex struct
        /// (defined as a non-primitive or non-blittable type, like an array)
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="Parameters"></param>
        /// <param name="Results"></param>
        public static void UnivDrvCommand_OutComplex(Cmd Command, object Parameters, object Results) {
            // marshall the input structure, if it exists
            GCHandle ParamGch = NullGch;
            IntPtr ParamPtr = IntPtr.Zero;
            if (null != Parameters) {
                ParamGch = GCHandle.Alloc(Parameters, GCHandleType.Pinned);
                ParamPtr = ParamGch.AddrOfPinnedObject();
            }
            // translate the struct into bytes, which are pinned
            IntPtr ResultsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Results));
            // pass true to free up any incoming memory, since we're going to overwrite it
            Marshal.StructureToPtr(Results, ResultsPtr, true);

            //
            // make the call
            //
            Error err = SBIGUnivDrvCommand(Command, ParamPtr, ResultsPtr);
            if (Error.CE_NO_ERROR != err)
                throw new FailedOperation(err);

            // Marshall back
            Marshal.PtrToStructure(ResultsPtr, Results);

            // clean up
            Marshal.FreeHGlobal(ResultsPtr);
            if (IntPtr.Zero != ParamPtr)
                ParamGch.Free();
        }

        //
        // Exposure helpers
        //

        /// <summary>
        /// Waits for any exposure in progress to complete, ends it, and reads it out into a 2D ushort array
        /// </summary>
        public static ushort[,] WaitEndAndReadoutExposure(StartExposureParams2 sep) {
            // wait for the exposure to be done
            QueryCommandStatusParams qcsp = new QueryCommandStatusParams(Cmd.CC_START_EXPOSURE);
            QueryCommandStatusResults qcsr = new QueryCommandStatusResults(Error.CE_NO_ERROR);
            while (Error.CE_NO_EXPOSURE_IN_PROGRESS != qcsr.status)
                qcsr = UnivDrvCommand<QueryCommandStatusResults>(Cmd.CC_QUERY_COMMAND_STATUS, qcsp);

            // prepare the CCD for readout
            UnivDrvCommand(Cmd.CC_END_EXPOSURE, new EndExposureParams(CCD.Imaging));
            // then telling it where and how we're going to read
            StartReadoutParams srp = new StartReadoutParams();
            srp.ccd = CCD.Imaging;
            srp.readoutMode = 0;
            srp.left = 0;
            srp.top = 0;
            srp.width = sep.width;
            srp.height = sep.height;
            UnivDrvCommand(Cmd.CC_START_READOUT, srp);

            // allocate the image buffer
            ushort[,] data = new ushort[sep.height, sep.width];
            GCHandle datagch = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr dataptr = datagch.AddrOfPinnedObject();

            // put the data into it
            ReadoutLineParams rlp = new ReadoutLineParams();
            rlp.ccd = CCD.Imaging;
            rlp.pixelStart = 0;
            rlp.pixelLength = sep.width;
            rlp.readoutMode = 0;
            GCHandle rlpgch = GCHandle.Alloc(rlp, GCHandleType.Pinned);
            // get the image from the camera, line by line
            for (int i = 0; i < sep.height; i++)
                SBIGUnivDrvCommand(Cmd.CC_READOUT_LINE, rlpgch.AddrOfPinnedObject(), dataptr + (i * sep.width * sizeof(ushort)));

            // cleanup our memory goo
            rlpgch.Free();
            datagch.Free();
            /*Bitmap b3 = new Bitmap(sep.width, sep.height, PixelFormat.Format16bppGrayScale);
            BitmapData bd = b3.LockBits(new Rectangle(0, 0, sep.width, sep.height), ImageLockMode.WriteOnly, PixelFormat.Format16bppGrayScale);
            bd.Scan0 = datagch.AddrOfPinnedObject();
            b3.UnlockBits(bd);
            Color c2 = b3.GetPixel(0, 0);
            Bitmap bmp = new Bitmap(sep.width, sep.height, sep.width * sizeof(ushort), PixelFormat.Format16bppGrayScale, datagch.AddrOfPinnedObject());
            bmp.Save("foo.bmp");
            bmp.Dispose();*/

            return data;
        }

        // COMMENTED OUT: because GDI.net doesn't support our pixel format
        /// <summary>
        /// In addition to reading out the image data, converts it into a Bitmap
        /// </summary>
        //public static void SaveImageToVernacularFormat(StartExposureParams2 sep, ushort[,] data, string filename, ImageFormat format)
        //{
        //    // find min and max
        //    int min = Int32.MaxValue;
        //    int max = Int32.MinValue;
        //    for (int j = 0; j < sep.height; j++)
        //        for (int i = 0; i < sep.width; i++)
        //        {
        //            if (data[j, i] < min)
        //                min = data[j, i];
        //            if (data[j, i] > max)
        //                max = data[j, i];
        //        }

        //    // construct a new array with scales
        //    byte[,] b = new byte[sep.height, sep.width];
        //    for (int j = 0; j < sep.height; j++)
        //        for (int i = 0; i < sep.width; i++)
        //            b[j, i] = (byte) ( (data[j, i] - min) * 256.0 / (max - min) );

        //    // construct the bitmap object with the bytes
        //    GCHandle bgch = GCHandle.Alloc(b);
        //    using (Bitmap bmp = new Bitmap(sep.width, sep.height, /*sep.width * sizeof(byte),*/ PixelFormat.Format8bppIndexed))//, GCHandle.ToIntPtr(bgch)))
        //    {
        //        /*
        //        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, sep.width, sep.height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
        //        Marshal.Copy(GCHandle.ToIntPtr(bgch), 0, bd.Scan0, sep.width * sep.height);
        //        bmp.UnlockBits(bd);
        //        */

        //        // create a pallette for the damn image
        //        for (int i = 0; i < 256; i++)
        //            bmp.Palette.Entries[i] = Color.FromArgb(i, i, i);

        //        for (int j = 0; j < sep.height; j++)
        //            for (int i = 0; i < sep.width; i++)
        //                bmp.SetPixel(i, j, Color.FromArgb(b[i, j]));

        //        // write it out
        //        bmp.Save(filename, format);
        //    }

        //    // clean up
        //    bgch.Free();
        //}

        /// <summary>
        /// Tries to autodetect ImageFormat based on filename
        /// </summary>
        //public static void WaitEndReadAndSaveExposure(StartExposureParams2 sep, string filename)
        //{
        //    // grab the extension
        //    string ext = Path.GetExtension(filename);
        //    // strip off the leading dot, if it's there
        //    if (ext.StartsWith("."))
        //        ext = ext.Substring(1);

        //    // pick the format based on it matching the strings used in the ImageFormat enum
        //    ImageFormat format = (ImageFormat) Enum.Parse(typeof(ImageFormat), ext);

        //    // do the heaving lifting
        //    throw new Exception("Not yet imnplemented, probably never will be, and probably shouldn't be");
        //    //WaitEndAndReadExposureAsBitmap(sep).Save(filename, format);
        //}

        //
        // Shortcut commands
        //

        public static CameraType EstablishLink() {
            EstablishLinkParams elp = new EstablishLinkParams();
            return UnivDrvCommand<EstablishLinkResults>(Cmd.CC_ESTABLISH_LINK, elp).cameraType;
        }

        #endregion Methods
    } // class
} // namespace