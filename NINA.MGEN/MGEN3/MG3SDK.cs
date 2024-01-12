#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.MGEN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.MGEN3 {

    internal class MG3SDK : IMG3SDK {
        private const string DLLNAME = "MG3lib.dll";

        private object lockobj = new object();
        private IntPtr handle = IntPtr.Zero;

        public MG3SDK(string ftdiDllPath, string mg3DllPath, ILogger logger) {
            DllLoader.LoadDll(ftdiDllPath, logger);
            DllLoader.LoadDll(mg3DllPath, logger);
        }

        public int PollDevice() {
            lock (lockobj) {
                if (handle != IntPtr.Zero) {
                    Release_MG3_handle(handle);
                }
                handle = Create_MG3_Handle();
                var code = MG3lib_PollDevice(handle);
                Release_MG3_handle(handle);
                return code;
            }
        }

        public int Open() {
            lock (lockobj) {
                if (handle != IntPtr.Zero) {
                    Release_MG3_handle(handle);
                }
                handle = Create_MG3_Handle();
                return MG3lib_Open(handle);
            }
        }

        public int PulseESC(int duration) {
            lock (lockobj) {
                return MG3lib_PulseESC(handle, duration);
            }
        }

        public int ReadDisplay(ushort[] buffer, byte[] leds, bool fullRead) {
            lock (lockobj) {
                return MG3lib_UpdateDisplay(handle, buffer, leds, fullRead);
            }
        }

        public int PushButton(int bcode, bool keyup) {
            lock (lockobj) {
                return MG3lib_InsertButtonEvent(handle, bcode, keyup);
            }
        }

        public int Dither() {
            lock (lockobj) {
                return MG3lib_DitheringControl(handle, true);
            }
        }

        public int GetDitherState(out int state) {
            state = 0;
            lock (lockobj) {
                return MG3lib_ReadDitheringState(handle, out state);
            }
        }

        public void Close() {
            lock (lockobj) {
                MG3lib_Close(handle);
                Release_MG3_handle(handle);
                handle = IntPtr.Zero;
            }
        }

        public int ReadDitheringParameters(out MGEN3_DitherParameters pd) {
            lock (lockobj) {
                pd = new MGEN3_DitherParameters();
                pd.strSize = (uint)(System.Runtime.InteropServices.Marshal.SizeOf(pd));
                return MG3lib_ReadDitheringPrms(handle, out pd);
            }
        }

        public int ReadImagingParameters(out int pgain, out int pexpo_ms) {
            lock (lockobj) {
                return MG3lib_ReadImagingPrms(handle, out pgain, out pexpo_ms);
            }
        }

        public int SetImagingParameters(int pgain, int pexpo_ms) {
            lock (lockobj) {
                return MG3lib_SetImagingPrms(handle, pgain, pexpo_ms);
            }
        }

        public int StartAutoGuiding(int newrefpt) {
            lock (lockobj) {
                return MG3lib_Function_AG_Start(handle, newrefpt);
            }
        }

        public int StopAutoGuiding() {
            lock (lockobj) {
                return MG3lib_Function_AG_Stop(handle);
            }
        }

        public int CancelFunction() {
            lock (lockobj) {
                return MG3lib_CancelFunction(handle);
            }
        }

        public int StarSearch(int ming = -1, int maxg = -1, int minexpo = -1, int maxexpo = -1) {
            lock (lockobj) {
                var pss = new MGEN3_StarSearch() {
                    min_gain = ming,
                    max_gain = maxg,
                    min_expo = minexpo,
                    max_expo = maxexpo,
                    prefer_long_expo = 0
                };
                pss.strSize = (uint)(System.Runtime.InteropServices.Marshal.SizeOf(pss));
                return MG3lib_Function_StarSearch(handle, ref pss);
            }
        }

        public int StartCalibration() {
            lock (lockobj) {
                var cal = new MGEN3_Func_Cal() {
                    tries_max = 20,
                    retry_rate = 2
                };
                cal.strSize = (uint)(System.Runtime.InteropServices.Marshal.SizeOf(cal));
                return MG3lib_Function_Calibration(handle, ref cal);
            }
        }

        public int ClearCalibration() {
            lock (lockobj) {
                //"To clear the calibration data set RA vector's length lower than 1e-6 (practically zero)"
                var zero = new MGEN3_Calibration() { rax = 0.00000000001F, ray = 0.00000000001F };
                zero.strSize = (uint)(System.Runtime.InteropServices.Marshal.SizeOf(zero));
                return MG3lib_SetCalibration(handle, ref zero);
            }
        }

        public int GetFunctionState(out int pires) {
            lock (lockobj) {
                return MG3lib_GetFunctionState(handle, out pires);
            }
        }

        public int ReadLastFrameData(ref MGEN3_FrameData data) {
            lock (lockobj) {
                data.strSize = (uint)(System.Runtime.InteropServices.Marshal.SizeOf(data));
                data.frame_idx = 0xffffffff;
                data.pimage = null;
                data.size = new byte[] { 0, 0 };
                return MG3lib_ReadLastFrameData(handle, ref data);
            }
        }

        public int ReadCalibration(out MGEN3_Calibration calibration) {
            lock (lockobj) {
                calibration = new MGEN3_Calibration();
                calibration.strSize = (uint)(System.Runtime.InteropServices.Marshal.SizeOf(calibration));

                return MG3lib_ReadCalibration(handle, out calibration);
            }
        }

        #region "MGEN3_External_Calls"

        [DllImport(DLLNAME, EntryPoint = "Create_MG3_Handle", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Create_MG3_Handle();

        [DllImport(DLLNAME, EntryPoint = "Release_MG3_Handle", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Release_MG3_handle(IntPtr handle);

        /// <summary>
        /// Opens and initializes the communication with a MGen-3 HandController device.
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3_PollDevice", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_PollDevice(IntPtr handle);

        /// <summary>
        /// opens the first FT device that has the attributes of an MGen-3
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3_Open", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_Open(IntPtr handle);

        /// <summary>
        /// closes the connection if any
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3_Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern void MG3lib_Close(IntPtr handle);

        /// <summary>
        /// pulses the ESC control pin for at least the given amount of time (turning on MGen-3)
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3_ESC_control", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_PulseESC(IntPtr handle, int length, string addr = null, int srvport = 0);

        /// <summary>
        /// pulses the RESET line of the MCU. Use it ONLY when nothing else is possible, this is not a normal way to restart MGen-3!
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3_RESET_control", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_PulseRESET(IntPtr handle, int length = 20);

        /// <summary>
        /// resets the communication protocol (MGen-3 will reset its comm.interface)
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3_ResetCommunication", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ResetCommunication(IntPtr handle, bool reopen = true);

        /// <summary>
        /// get device mode and protocol version
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3_GetCurrentMode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_GetDeviceMode(IntPtr handle, out int pboot, out ushort pver, out ushort pextra);

        /// <summary>
        /// get current device's info
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadDeviceInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ReadDeviceInfo(IntPtr handle, out MGEN3_DeviceInfo pdi);

        /// <summary>
        /// Changes mode of running. Stops, starts fw/boot etc. Default (false,false) is turn-off. (Not available from BOOT at ver. <1.2)
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ChangeRunningMode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ChangeRunningMode(IntPtr handle, bool goto_boot = false, bool start_fw = false);

        /// <summary>
        /// enters a new button event into MGen-3's input queue
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_InsertButtonEvent", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_InsertButtonEvent(IntPtr handle, int bcode, bool keyup = false);

        /// <summary>
        /// reads the display content of MGen-3 and updates the frame buffer in 'pBuf'
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_UpdateDisplay", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_UpdateDisplay(IntPtr handle, ushort[] pBuf, byte[] pLeds, bool fullfrm = false);

        /// <summary>
        /// read guider setup variables
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadGuiderSetup", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ReadGuiderSetup(IntPtr handle, out int pfoclen_mm, out float pagspeed);

        /// <summary>
        /// set guider setup variables
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_SetGuiderSetup", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_SetGuiderSetup(IntPtr handle, int foclen_mm = -1, float agspeed = -1.0f);

        /// <summary>
        /// read imaging parameters
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadImagingPrms", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ReadImagingPrms(IntPtr handle, out int pgain, out int pexpo_ms);

        /// <summary>
        /// set imaging parameters
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_SetImagingPrms", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_SetImagingPrms(IntPtr handle, int gain = -1, int expo_ms = -1);

        /// <summary>
        /// read AG parameters
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadAGPrms", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ReadAGPrms(IntPtr handle, out MGEN3_AutoGuidingParameters pag, int ax);

        /// <summary>
        /// set AG parameters
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_SetAGPrms", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_SetAGPrms(IntPtr handle, ref MGEN3_AutoGuidingParameters pag, int ax);

        /// <summary>
        /// read dithering parameters
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadDitheringPrms", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ReadDitheringPrms(IntPtr handle, out MGEN3_DitherParameters pd);

        /// <summary>
        /// set dithering parameters
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_SetDitheringPrms", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_SetDitheringPrms(IntPtr handle, ref MGEN3_DitherParameters pd);

        /// <summary>
        /// read last frame's data
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadLastFrameData", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ReadLastFrameData(IntPtr handle, ref MGEN3_FrameData pfd);

        /// <summary>
        /// read current calibration data
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadCalibration", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ReadCalibration(IntPtr handle, out MGEN3_Calibration pc);

        /// <summary>
        /// set current calibr. data
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_SetCalibration", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_SetCalibration(IntPtr handle, ref MGEN3_Calibration pc);

        /// <summary>
        /// read dithering state
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadDitheringState", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_ReadDitheringState(IntPtr handle, out int pst);

        /// <summary>
        /// trigger or stop dithering
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_DitheringControl", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_DitheringControl(IntPtr handle, bool trigger);

        /// <summary>
        /// reads the state of the currently running function in MGen-3
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_ReadFunctionState", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_GetFunctionState(IntPtr handle, out int pires);

        /// <summary>
        /// signals the cancellation of the running function
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_CancelFunction", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_CancelFunction(IntPtr handle);

        /// <summary>
        /// starts a StarSearch procedure
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_Function_StarSearch", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_Function_StarSearch(IntPtr handle, ref MGEN3_StarSearch pss);

        /// <summary>
        /// starts a Calibration procedure
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_Function_Calibrate", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_Function_Calibration(IntPtr handle, ref MGEN3_Func_Cal calibration);

        /// <summary>
        /// starts AutoGuiding if possible
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_Function_AGStart", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_Function_AG_Start(IntPtr handle, int newrefpt = 0);

        /// <summary>
        /// stops AutoGuiding
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_Function_AGStop", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_Function_AG_Stop(IntPtr handle);

        /// <summary>
        /// starts One-Push guiding procedure
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3cmd_Function_OnePushStart", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MG3lib_Function_OnePush_Start(IntPtr handle);

        /// <summary>
        /// formats the return value (error code) into a simple string
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "MG3_FormatResult", CallingConvention = CallingConvention.Cdecl)]
        private static extern StringBuilder MG3lib_FormatResult(IntPtr handle, int code);

        #endregion "MGEN3_External_Calls"

        #region "MGEN3_Structs"

        [StructLayout(LayoutKind.Sequential)]
        public struct MGEN3_DeviceInfo {
            private StringBuilder ft_serial;         //FTDI IC's data
            private StringBuilder ft_descr;

            private int is_boot;                //MGen-3 HC's data

            private ushort prot_ver;
            private ushort extra_ver;
            private uint ChipID;
            private StringBuilder UID;
            private byte PCB_pins;

            private StringBuilder distr_name;       //Distributor's name (if other than LACERTA directly)
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MGEN3_AutoGuidingParameters {
            public uint strSize;
            private int mode;           //AutoGuiding mode (0=manual, 1=auto trim)
            private float Prop;
            private float Integr;
            private int I_enable;
            private float Tol;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MGEN3_DitherParameters {
            public uint strSize;
            public int enable;
            public float diameter;
            public int period;
        }

        [Flags]
        public enum LFDFlags : uint {
            LFD_BASIC_DATA = 0x01,
            LFD_VEC_AGCENTER = 0x02,
            LFD_VEC_AGREFPT = 0x04,
            LFD_VEC_XFORM = 0x08,
            LFD_IMAGE = 0x10,
            LFD_IGNORE_KNOWN = 0x20,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MGEN3_FrameData {
            public uint strSize;
            public uint query;
            public int star_present;
            public int ag_enabled;
            public uint frame_idx;
            public float pos_x;
            public float pos_y;
            public float fwhm;
            public float brightness;
            public float ag_center_x;
            public float ag_center_y;
            public float ag_refpt_x;
            public float ag_refpt_y;
            public float cal_ra_x;
            public float cal_ra_y;
            public float cal_dec_x;
            public float cal_dec_y;

            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I2)]
            public short[] pimage;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U1)]
            public byte[] size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MGEN3_StarSearch {
            public uint strSize;
            public float min_gain;
            public float max_gain;
            public float min_expo;
            public float max_expo;
            public float prefer_long_expo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MGEN3_Calibration {
            public uint strSize;
            public float rax;
            public float ray;
            public float decx;
            public float decy;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MGEN3_Func_Cal {
            public uint strSize;
            public int tries_max;
            public float retry_rate;
        }

        #endregion "MGEN3_Structs"

        #region "MGEN3_ReturnValues

        // possible return values and error codes of the library functions
        public const int MG3_INVALID_HANDLE = -1; //used for DLL API: if there was no object created yet (invalid), functions return this value

        public const int MG3_OK = 0;

        public const int MG3_FAILED_TO_OPEN_DEVICE = 1;  //comm. error
        public const int MG3_DEVICE_NOT_OPENED = 2;  //comm. error
        public const int MG3_FT230_ERROR = 9;  //comm. error (FT230's driver or no answer (cancelled command))
        public const int MG3_INVALID_COMMAND = 10;  //comm. error
        public const int MG3_COMMAND_TOO_LONG = 11;  // was too long before sending
        public const int MG3_SYS_MODE_CHANGED = 12;  //this is actually not 'error' but indicates that the comm.protocol will be reset (The current command may be lost/invalid but the next ones should work.)
        public const int MG3_SYS_POWEROFF = 13;  //| these indicate that the MGen-3 will be powered down now, communication is closed. (MG3_Close() is called automatically)
        public const int MG3_SYS_ERROR = 14;  //|   (on an unknown error the same must be done)
        public const int MG3_INVALID_ANSWER = 30;
        public const int MG3_WRONG_ANSWER_ID_RECVD = 31;
        public const int MG3_WRONG_COUNTER_RECVD = 32;
        public const int MG3_UNKNOWN_ANSWER_TYPE = 33;
        public const int MG3_ANSWER_TOO_SHORT = 34;
        public const int MG3_CANT_READ_PROTOCOL = 35;

        public const int MG3_INVALID_COUNTER_VALUE = 50;  //communication is OK, higher level errors are here
        public const int MG3_UNKNOWN_PARAMETER = 51;
        public const int MG3_TOO_MANY_PARAMETERS = 52;
        public const int MG3_TOO_MANY_BYTES_SENT = 53;  // if we sent too many bytes and they couldn't be stored (MG3 stores not all parameters, we can send more than it may store)
        public const int MG3_TOO_FEW_BYTES_RECVD = 54;  //some of the required length prm. couldnt be read
        public const int MG3_TOO_MANY_BYTES_RECVD = 55;
        public const int MG3_UNKNOWN_ANS_PARAMETER = 56;  //answer contains unknown prm.
        public const int MG3_NO_ANSWER = 57;
        public const int MG3_BAD_ANSWER_PRM_LENGTH = 58;
        public const int MG3_COMMAND_NOT_EXISTING = 60;  //the device does not know the command (by its cmd.set descriptor)
        public const int MG3_COMMAND_PRM_UNSUPPORTED = 61;  //a parameter is unsupported by the command
        public const int MG3_COMMAND_PRM_WRONG_SIZE = 62;  //a parameter size is wrongly given
        public const int MG3_COMMAND_SIZE_OVERRUN = 63;  //a command is too long now (ofc. not sent to device)

        public const int MG3_CMD_EXEC_ERROR = 66;  //this happens if the execution fails on MG3. Normal use error. Unique error codes are reported for each cmd.
        public const int MG3_FUNC_BUSY = 70;  //the HC is busy and so can't do/start the required function
        public const int MG3_VARIABLE_SIZE_MISMATCH = 75;  //the variable's size (expected and read/required) differs

        public const int MG3_BOOT_MODE_REQUIRED = 90; //for commands that need BOOT mode to be run

        public const int MG3_UPDATE_DONE_BUT_SIZE_MISMATCH = 100;     //update has been done but some size mismatch is detected (we should treat this as an error)
        public const int MG3_UPDATE_INTERNAL_FAULT = 101;     //should not happen... but it may if MG3 is confused/faulty
        public const int MG3_UPDATE_PROHIBITED = 102;     //MG3 must be in such mode that Fw.update is prohibited/unavailable
        public const int MG3_UPDATE_FAILED_WRONG_HS_KEY = 103;     //wrong handshaking key was sent back
        public const int MG3_UPDATE_FAILED = 104;     //failed for some reason
        public const int MG3_UPDATE_FAILED_WRONG_USER_KEY = 105;     //wrong user key is sent
        public const int MG3_UPDATE_FAILED_TO_START_CAMERA = 106;
        public const int MG3_UPDATE_FAILED_NO_SPACE = 107;     //no space for HC firmware

        public const int MG3_WRONG_ARGUMENT_GIVEN = 200;     //a function has got a wrong argument value

        public const int MG3_WIN_CLIPBOARD_ACCESS_ERROR = 901;     //can't access Windows clipboard
        public const int MG3_WIN_BITMAP_ERROR = 902;     //some error with the bitmap

        #endregion "MGEN3_ReturnValues

        #region "MGEN3_FunctionReturnValues"

        public const int ERR_OK = 0;  //no error
        public const int ERR_CANCELED = 1;  //User has canceled the function (ESC has been pressed or by an API call)

        public const int ERR_VARIABLE_CHANGE_PERMITTED = 50;  //change of an internal variable is permitted, function can't be finished
        public const int ERR_FUNCTION_IS_DISABLED = 51;  //running the function is permitted now, e.g. AutoGuiding is in progress and it disables the function
        public const int ERR_FUNCTION_TIMED_OUT = 52;  //when an internal communication times out (deadlock or extreme situation)

        public const int ERR_CAMERA_NOT_ACTIVE = 100;  //Camera is not active but would be required
        public const int ERR_GUIDESTAR_NOT_AVAILABLE = 110;  //no guiding star is available currently
        public const int ERR_MOVEMENT_TOO_SMALL = 111;  //the movement of the star is too small (for a function that needs it)
        public const int ERR_AG_ACTIVE = 120;  //the movement of the star is too small (for a function that needs it)

        public const int ERR_FILESYS_NOT_AVAILABLE = 200;  //FileSystem is not available (no SD card, mounting error etc.)
        public const int ERR_FILE_CANT_FIND = 201;  //a file can't be opened (found)
        public const int ERR_FILESYS_CANT_SAVE = 202;  //a file can't be saved
        public const int ERR_FILE_READ_ERROR = 210;  //file read error, common (format is bad etc.)
        public const int ERR_DIR_ERRORS = 220;  //directory operation error (create, change etc.)
        public const int ERR_FILESYS_CANT_FORMAT = 250;  //can't format SD card (for any reason, there is open file, invalid card, no card etc.)

        public const int ERR_OUT_OF_MEMORY = 300;  //out of dynamic memory / can't allocate. Should not happen.

        public const int ERR_CAMERA_BUSY = 400;  //The AG/Camera object is busy that must be used while a(n interface's) function call
                                                 // ie. if Star Search is running and using the Camera, while another interface wants to allocate the Camera control for another functions...

        public const int ERR_CALIBRATION_MISSING = 500;  //a valid calibration data is required

        public const int ERR_WRONG_IMAGE_DETECTED = 900;  //an image taken that must have wrong data (wrong black level, too noisy etc.)

        public const int ERR_INTERNAL = 1000;  //any internal error that should not happen. Must be some bug in the Firmware or hw. fault.

        #endregion "MGEN3_FunctionReturnValues"
    }
}