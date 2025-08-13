using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.ASTPANSDK {
    [ExcludeFromCodeCoverage]
    public class ASTPANPInvokeProxy : IASTPANPInvokeProxy {
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetNumOfCameras(out int number) {
            return ASTPANPInvoke.ASTPANGetNumOfCameras(out number);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetCameraInfo(out ASTPAN_CAMERA_INFO pCameraInfo, int iCameraIndex) {
            return ASTPANPInvoke.ASTPANGetCameraInfo(out pCameraInfo, iCameraIndex);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetCameraInfoByID(int id, out ASTPAN_CAMERA_INFO pCameraInfo) {
            return ASTPANPInvoke.ASTPANGetCameraInfoByID(id, out pCameraInfo);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANOpenCamera(int id) {
            return ASTPANPInvoke.ASTPANOpenCamera(id);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANInitCamera(int id) {
            return ASTPANPInvoke.ASTPANInitCamera(id);
        }

        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANCloseCamera(int id) {
            return ASTPANPInvoke.ASTPANCloseCamera(id);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANStartVideoCapture(int id) {
            return ASTPANPInvoke.ASTPANStartVideoCapture(id);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANStopVideoCapture(int id) {
            return ASTPANPInvoke.ASTPANStopVideoCapture(id);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANStartExposure(int id) {
            return ASTPANPInvoke.ASTPANStartExposure(id);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANStopExposure(int id) {
            return ASTPANPInvoke.ASTPANStopExposure(id);
        }

        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANSetConfigValue(int id, ref ASTPAN_CONFIG pConfig, ASTPAN_CFG_TYPE index) {
            return ASTPANPInvoke.ASTPANSetConfigValue(id, ref pConfig, index);
        }

        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetConfigValue(int id, ref ASTPAN_CONFIG pConfig, ASTPAN_CFG_TYPE index) {
            return ASTPANPInvoke.ASTPANGetConfigValue(id, ref pConfig, index);
        }

        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetAutoConfigInfo(int id, ref ASTPAN_AUTO_CONFIG_INFO pAutoConfigInfo) {
            return ASTPANPInvoke.ASTPANGetAutoConfigInfo(id, ref pAutoConfigInfo);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANSetAutoConfigValue(int id, ASTPAN_AUTO_TYPE index, int value, int bAuto) {
            return ASTPANPInvoke.ASTPANSetAutoConfigValue(id, index, value, bAuto);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetAutoConfigValue(int id, ASTPAN_AUTO_TYPE index, out int value, out int bAuto) {
            return ASTPANPInvoke.ASTPANGetAutoConfigValue(id, index, out value, out bAuto);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetVideoDataMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize, int iWaitms) {
            return ASTPANPInvoke.ASTPANGetVideoDataMono8(iCameraID, pBuffer, lBuffSize, iWaitms);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetVideoDataMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize, int iWaitms) {
            return ASTPANPInvoke.ASTPANGetVideoDataMono16(iCameraID, pBuffer, lBuffSize, iWaitms);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetDataAfterExpMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize) {
            return ASTPANPInvoke.ASTPANGetDataAfterExpMono8(iCameraID, pBuffer, lBuffSize);
        }
        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetDataAfterExpMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize) {
            return ASTPANPInvoke.ASTPANGetDataAfterExpMono16(iCameraID, pBuffer, lBuffSize);
        }

        [SecurityCritical]
        public ASTPAN_RET_TYPE ASTPANGetExpStatus(int id, out ASTPAN_EXP_TYPE pExpStatus) {
            return ASTPANPInvoke.ASTPANGetExpStatus(id, out pExpStatus);
        }

        public string GetSDKVersion() {
            return "Unknown";                        
        }
    }

    [ExcludeFromCodeCoverage]
    public static class ASTPANPInvoke {
        private const string DLLNAME = "ASTPANCamera.dll";

        static ASTPANPInvoke() {
            DllLoader.LoadDll(Path.Combine("ASTPAN", DLLNAME));
        }

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANGetNumOfCameras), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetNumOfCameras(out int number);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANGetCameraInfo), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetCameraInfo(out ASTPAN_CAMERA_INFO pCameraInfo, int iCameraIndex);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANGetCameraInfoByID), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetCameraInfoByID(int id, out ASTPAN_CAMERA_INFO pCameraInfo);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANOpenCamera), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANOpenCamera(int id);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANInitCamera), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANInitCamera(int id);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANCloseCamera), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANCloseCamera(int id);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANSetConfigValue), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANSetConfigValue(int id, ref ASTPAN_CONFIG pConfig, ASTPAN_CFG_TYPE index);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANGetConfigValue), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetConfigValue(int id, ref ASTPAN_CONFIG pConfig, ASTPAN_CFG_TYPE index);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANGetAutoConfigInfo), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetAutoConfigInfo(int id, ref ASTPAN_AUTO_CONFIG_INFO pAutoConfigInfo);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANSetAutoConfigValue), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANSetAutoConfigValue(int id, ASTPAN_AUTO_TYPE index,int value, int bAuto);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANGetAutoConfigValue), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetAutoConfigValue(int id, ASTPAN_AUTO_TYPE index, out int value, out int bAuto);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANStartVideoCapture), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANStartVideoCapture(int id);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANStopVideoCapture), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANStopVideoCapture(int id);

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetVideoData", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetVideoDataMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize, int iWaitms);

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetVideoData", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetVideoDataMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize, int iWaitms);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANStartExposure), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANStartExposure(int id);

        [DllImport(DLLNAME, EntryPoint = nameof(ASTPANStopExposure), CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANStopExposure(int id);

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetDataAfterExp", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetDataAfterExpMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize);

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetDataAfterExp", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetDataAfterExpMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize);



        [DllImport(DLLNAME, EntryPoint = "ASTPANGetExpStatus", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASTPAN_RET_TYPE ASTPANGetExpStatus(int id, out ASTPAN_EXP_TYPE pExpStatus);

        //[DllImport(DLLNAME, EntryPoint = nameof(ASTPANGetAPIVersion), CallingConvention = CallingConvention.StdCall)]
        //public static extern IntPtr ASTPANGetAPIVersion();

        //public static string GetSDKVersion() {
        //    IntPtr p = ASTPANGetAPIVersion();
        //    string version = Marshal.PtrToStringAnsi(p);

        //    return version;
        //}
    }

    public struct ASTPAN_CAMERA_INFO {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U1)]
        public byte[] name;
        public int CameraID;
        public int MaxHeight;
        public int MaxWidth;
        public BAYER_PATTERN BayerPattern;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] SupportedBins;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public IMG_TYPE[] SupportedVideoFormat;
        public double PixelSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public CAMERA_MODE[] SupportedCameraMode;
        public int MechanicalShutter;
        public int ST4GuidePort;
        public int IsColorCam;
        public int IsCoolerCam;
        public int IsUSB3Host;
        public int IsUSB3Camera;
        public int IsEdgeTrigger;
        public int IsLevelTrigger;
        public float ElecPerADU;
        public int BitDepth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U1)]
        public byte[] Unused;

        public string Name => this.name?.Length > 0 ? Encoding.ASCII.GetString(this.name)?.TrimEnd(new char[1]) : "";
    }

    public enum ASTPAN_EXP_TYPE {
        ASTPAN_EXP_IDLE,
        ASTPAN_EXP_WORKING,
        ASTPAN_EXP_SUCCESS,
        ASTPAN_EXP_FAILED,
    }

    public enum BAYER_PATTERN {
        BAYER_RG = 0,
        BAYER_BG,
        BAYER_GR,
        BAYER_GB
    }

    public enum IMG_TYPE {//Supported Video Format
        IMG_END = -1, // 0xFFFFFFFF
        IMG_RAW8 = 0,
        IMG_RGB24 = 1,
        IMG_RAW16 = 2,
        IMG_Y8 = 3,
    }

    public enum CAMERA_MODE {
        MODE_NORMAL = 0,
        MODE_TRIG_SOFT,
        MODE_TRIG_RISE_EDGE,
        MODE_TRIG_FALL_EDGE,
        MODE_TRIG_DOUBLE_EDGE,
        MODE_TRIG_HIGH_LEVEL,
        MODE_TRIG_LOW_LEVEL,
        MODE_END = -1
    }
    public enum ASTPAN_RET_TYPE {
        ASTPAN_RET_SUCCESS,
        ASTPAN_RET_ERROR_INDEX,
        ASTPAN_RET_ERROR_ID,
        ASTPAN_RET_ERROR_PARAMETER,
        ASTPAN_RET_ERROR_SEQUENCE,
        ASTPAN_RET_ERROR_PATH,
        ASTPAN_RET_ERROR_FILEFORMAT,
        ASTPAN_RET_ERROR_SIZE,
        ASTPAN_RET_ERROR_IMGTYPE,
        ASTPAN_RET_ERROR_MODE,
        ASTPAN_RET_CAMERA_CLOSED,
        ASTPAN_RET_CAMERA_REMOVED,
        ASTPAN_RET_OUTOF_BOUNDARY,
        ASTPAN_RET_TIMEOUT,
        ASTPAN_RET_BUFFER_TOO_SMALL,
        ASTPAN_RET_VIDEO_MODE_ACTIVE,
        ASTPAN_RET_EXPOSURE_IN_PROGRESS,
        ASTPAN_RET_GENERAL_ERROR,
        ASTPAN_RET_END,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ASTPAN_CONFIG {
        public int Wr_ROIWidth;
        public int Wr_ROIHeight;
        public int Wr_ROIBin;
        public IMG_TYPE Wr_ROImg_type;
        public int Wr_StartPosX;
        public int Wr_StartPosY;
        public int Wr_PulseGuideOn;
        public int Wr_PulseGuideDirection;
        public int Wr_CameraMode;
        public int Wr_TriggerOutPin;
        public int Wr_TriggerOutHigh;
        public int Wr_TriggerOutDelay;
        public int Wr_TriggerOutDuration;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
        public byte[] Wr_ID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
        public byte[] r_SN;
        public int r_DroppedFrames;
        public int r_OffsetHighestDR;
        public int r_OffsetUnityGain;
        public int r_OffsetLowestRN;
        public int r_GainLowestRN;

        public string wr_id => Encoding.ASCII.GetString(this.Wr_ID).TrimEnd(new char[1]);

        public string r_sn => Encoding.ASCII.GetString(this.r_SN).TrimEnd(new char[1]);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ASTPAN_AUTO_CONFIG_INFO {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22, ArraySubType = UnmanagedType.Struct)]
        public ASTPAN_MUL_CONFIG[] AutoConfigInfo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ASTPAN_MUL_CONFIG {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U1)]
        public byte[] r_Name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128, ArraySubType = UnmanagedType.U1)]
        public byte[] r_Description;
        public int r_MaxValue;
        public int r_MinValue;
        public int r_DefaultValue;
        public int r_IsAutoSupported;
        public int r_IsWritable;
        public int r_IsSupported;

        public string r_name => Encoding.ASCII.GetString(this.r_Name).TrimEnd(new char[1]);

        public string r_description => Encoding.ASCII.GetString(this.r_Description).TrimEnd(new char[1]);
    }
    public enum ASTPAN_AUTO_TYPE {
        ASTPAN_AUTO_CFG_Gain,
        ASTPAN_AUTO_CFG_Exposure,
        ASTPAN_AUTO_CFG_Gamma,
        ASTPAN_AUTO_CFG_Wb_r,
        ASTPAN_AUTO_CFG_Wb_b,
        ASTPAN_AUTO_CFG_Offset,
        ASTPAN_AUTO_CFG_BandwidthOverload,
        ASTPAN_AUTO_CFG_Flip,
        ASTPAN_AUTO_CFG_OverClock,
        ASTPAN_AUTO_CFG_AutoMaxGain,
        ASTPAN_AUTO_CFG_AutoMaxExp,
        ASTPAN_AUTO_CFG_AutoTargetBrightn,
        ASTPAN_AUTO_CFG_HardwareBin,
        ASTPAN_AUTO_CFG_HighSpeedMode,
        ASTPAN_AUTO_CFG_CoolerPowerPerc,
        ASTPAN_AUTO_CFG_TargetTemp,
        ASTPAN_AUTO_CFG_CoolerOn,
        ASTPAN_AUTO_CFG_MonoBin,
        ASTPAN_AUTO_CFG_FanOn,
        ASTPAN_AUTO_CFG_PatternAdjust,
        ASTPAN_AUTO_CFG_Temperature,
        ASTPAN_AUTO_CFG_AntiDewHedter,
    }

    public enum ASTPAN_CFG_TYPE {
        ASTPAN_CFG_Wr_ROI,
        ASTPAN_CFG_Wr_StartPos,
        ASTPAN_CFG_Wr_PulseGuide,
        ASTPAN_CFG_wr_CameraMode,
        ASTPAN_CFG_Wr_TriggerOut,
        ASTPAN_CFG_Wr_ID,
        ASTPAN_CFG_r_SN,
        ASTPAN_CFG_r_DroppedFrames,
        ASTPAN_CFG_r_SupportedCameraMode,
        ASTPAN_CFG_r_OffsetGain,
    }
}
