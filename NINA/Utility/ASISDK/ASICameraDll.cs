using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace ZWOptical.ASISDK {
    public static class ASICameraDll {
        public enum ASI_CONTROL_TYPE {
            ASI_GAIN = 0,
            ASI_EXPOSURE,
            ASI_GAMMA,
            ASI_WB_R,
            ASI_WB_B,
            ASI_BRIGHTNESS,
            ASI_BANDWIDTHOVERLOAD,
            ASI_OVERCLOCK,
            ASI_TEMPERATURE,// return 10*temperature
            ASI_FLIP,
            ASI_AUTO_MAX_GAIN,
            ASI_AUTO_MAX_EXP,
            ASI_AUTO_MAX_BRIGHTNESS,
            ASI_HARDWARE_BIN,
            ASI_HIGH_SPEED_MODE,
            ASI_COOLER_POWER_PERC,
            ASI_TARGET_TEMP,// not need *10
            ASI_COOLER_ON,
            ASI_MONO_BIN,
            ASI_FAN_ON,
            ASI_PATTERN_ADJUST,
            ASI_ANTI_DEW_HEATER
        }


        public enum ASI_IMG_TYPE {
            //Supported image type
            ASI_IMG_RAW8 = 0,
            ASI_IMG_RGB24,
            ASI_IMG_RAW16,
            ASI_IMG_Y8,
            ASI_IMG_END = -1
        }


        public enum ASI_GUIDE_DIRECTION {
            ASI_GUIDE_NORTH = 0,
            ASI_GUIDE_SOUTH,
            ASI_GUIDE_EAST,
            ASI_GUIDE_WEST
        }

        public enum ASI_BAYER_PATTERN {
            ASI_BAYER_RG = 0,
            ASI_BAYER_BG,
            ASI_BAYER_GR,
            ASI_BAYER_GB
        };

        public enum ASI_EXPOSURE_STATUS {
            ASI_EXP_IDLE = 0,//: idle states, you can start exposure now
            ASI_EXP_WORKING,//: exposing
            ASI_EXP_SUCCESS,// exposure finished and waiting for download
            ASI_EXP_FAILED,//:exposure failed, you need to start exposure again
        };

        public enum ASI_ERROR_CODE { //ASI ERROR CODE
            ASI_SUCCESS = 0,
            ASI_ERROR_INVALID_INDEX, //no camera connected or index value out of boundary
            ASI_ERROR_INVALID_ID, //invalid ID
            ASI_ERROR_INVALID_CONTROL_TYPE, //invalid control type
            ASI_ERROR_CAMERA_CLOSED, //camera didn't open
            ASI_ERROR_CAMERA_REMOVED, //failed to find the camera, maybe the camera has been removed
            ASI_ERROR_INVALID_PATH, //cannot find the path of the file
            ASI_ERROR_INVALID_FILEFORMAT,
            ASI_ERROR_INVALID_SIZE, //wrong video format size
            ASI_ERROR_INVALID_IMGTYPE, //unsupported image formate
            ASI_ERROR_OUTOF_BOUNDARY, //the startpos is out of boundary
            ASI_ERROR_TIMEOUT, //timeout
            ASI_ERROR_INVALID_SEQUENCE,//stop capture first
            ASI_ERROR_BUFFER_TOO_SMALL, //buffer size is not big enough
            ASI_ERROR_VIDEO_MODE_ACTIVE,
            ASI_ERROR_EXPOSURE_IN_PROGRESS,
            ASI_ERROR_GENERAL_ERROR,//general error, eg: value is out of valid range
            ASI_ERROR_END
        };
        public enum ASI_BOOL {
            ASI_FALSE = 0,
            ASI_TRUE
        };
        public enum ASI_FLIP_STATUS {
            ASI_FLIP_NONE = 0,//: original
            ASI_FLIP_HORIZ,//: horizontal flip
            ASI_FLIP_VERT,// vertical flip
            ASI_FLIP_BOTH,//:both horizontal and vertical flip

        };
        public struct ASI_CAMERA_INFO {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            private byte[] name; //the name of the camera, you can display this to the UI
            public int CameraID; //this is used to control everything of the camera in other functions
            public int MaxHeight; //the max height of the camera
            public int MaxWidth; //the max width of the camera

            public ASI_BOOL IsColorCam;
            public ASI_BAYER_PATTERN BayerPattern;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public int[] SupportedBins; //1 means bin1 which is supported by every camera, 2 means bin 2 etc.. 0 is the end of supported binning method
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public ASI_IMG_TYPE[] SupportedVideoFormat;// ASI_IMG_TYPE[8]; //this array will content with the support output format type.IMG_END is the end of supported video format

            public double PixelSize; //the pixel size of the camera, unit is um. such like 5.6um
            public ASI_BOOL MechanicalShutter;
            public ASI_BOOL ST4Port;
            public ASI_BOOL IsCoolerCam;
            public ASI_BOOL IsUSB3Host;
            public ASI_BOOL IsUSB3Camera;
            public float ElecPerADU;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 24)]
            public byte[] Unused;

            public string Name {
                get { return Encoding.ASCII.GetString(name).TrimEnd((Char)0); }
            }
        };


        [StructLayout(LayoutKind.Sequential)]
        public struct ASI_CONTROL_CAPS {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            private byte[] name; //the name of the Control like Exposure, Gain etc..
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 128)]
            private byte[] description; //description of this control
            public int MaxValue;
            public int MinValue;
            public int DefaultValue;
            public ASI_BOOL IsAutoSupported; //support auto set 1, don't support 0
            public ASI_BOOL IsWritable; //some control like temperature can only be read by some cameras 
            public ASI_CONTROL_TYPE ControlType;//this is used to get value and set value of the control
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 32)]
            public byte[] Unused;

            public string Name {
                get { return Encoding.ASCII.GetString(name).TrimEnd((Char)0); }
            }

            public string Description {
                get { return Encoding.ASCII.GetString(description).TrimEnd((Char)0); }
            }
        }

        public enum ExposureStatus {
            ExpIdle = 0, //: idle states, you can start exposure now
            ExpWorking, //: exposing
            ExpSuccess, // exposure finished and waiting for download
            ExpFailed, //:exposure failed, you need to start exposure again

        }

        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ASIGetNumOfConnectedCameras")]
        public static extern int GetNumOfConnectedCameras();


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetCameraProperty(out ASI_CAMERA_INFO pASICameraInfo, int iCameraIndex);

        public static ASI_CAMERA_INFO GetCameraProperties(int cameraIndex) {
            ASI_CAMERA_INFO result;
            CheckReturn(ASIGetCameraProperty(out result, cameraIndex), MethodBase.GetCurrentMethod(), cameraIndex);
            return result;
        }

        private static void CheckReturn(ASI_ERROR_CODE errorCode, MethodBase callingMethod, params object[] parameters) {
            switch (errorCode) {
                case ASI_ERROR_CODE.ASI_SUCCESS:
                    break;
                case ASI_ERROR_CODE.ASI_ERROR_INVALID_INDEX:
                case ASI_ERROR_CODE.ASI_ERROR_INVALID_ID:
                case ASI_ERROR_CODE.ASI_ERROR_INVALID_CONTROL_TYPE:
                case ASI_ERROR_CODE.ASI_ERROR_CAMERA_CLOSED:
                case ASI_ERROR_CODE.ASI_ERROR_CAMERA_REMOVED:
                case ASI_ERROR_CODE.ASI_ERROR_INVALID_PATH:
                case ASI_ERROR_CODE.ASI_ERROR_INVALID_FILEFORMAT:
                case ASI_ERROR_CODE.ASI_ERROR_INVALID_SIZE:
                case ASI_ERROR_CODE.ASI_ERROR_INVALID_IMGTYPE:
                case ASI_ERROR_CODE.ASI_ERROR_OUTOF_BOUNDARY:
                case ASI_ERROR_CODE.ASI_ERROR_TIMEOUT:
                case ASI_ERROR_CODE.ASI_ERROR_INVALID_SEQUENCE:
                case ASI_ERROR_CODE.ASI_ERROR_BUFFER_TOO_SMALL:
                case ASI_ERROR_CODE.ASI_ERROR_VIDEO_MODE_ACTIVE:
                case ASI_ERROR_CODE.ASI_ERROR_EXPOSURE_IN_PROGRESS:
                case ASI_ERROR_CODE.ASI_ERROR_GENERAL_ERROR:
                case ASI_ERROR_CODE.ASI_ERROR_END:
                    throw new ASICameraException(errorCode, callingMethod, parameters);
                default:
                    throw new ArgumentOutOfRangeException("errorCode");
            }
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIOpenCamera(int iCameraID);

        public static void OpenCamera(int cameraId) {
            CheckReturn(ASIOpenCamera(cameraId), MethodBase.GetCurrentMethod(), cameraId);
        }

        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIInitCamera(int iCameraID);

        public static void InitCamera(int cameraId) {
            CheckReturn(ASIInitCamera(cameraId), MethodBase.GetCurrentMethod(), cameraId);
        }



        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASICloseCamera(int iCameraID);

        public static void CloseCamera(int cameraId) {
            CheckReturn(ASICloseCamera(cameraId), MethodBase.GetCurrentMethod(), cameraId);
        }



        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetNumOfControls(int iCameraID, out int piNumberOfControls);

        public static int GetNumOfControls(int cameraId) {
            int result;
            CheckReturn(ASIGetNumOfControls(cameraId, out result), MethodBase.GetCurrentMethod(), cameraId);
            return result;
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetControlCaps(int iCameraID, int iControlIndex, out ASI_CONTROL_CAPS pControlCaps);

        public static ASI_CONTROL_CAPS GetControlCaps(int cameraIndex, int controlIndex) {
            ASI_CONTROL_CAPS result;
            CheckReturn(ASIGetControlCaps(cameraIndex, controlIndex, out result), MethodBase.GetCurrentMethod(), cameraIndex, controlIndex);
            return result;
        }

        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetControlValue(int iCameraID, ASI_CONTROL_TYPE controlType, out int plValue, out ASI_BOOL pbAuto);

        public static int GetControlValue(int cameraId, ASI_CONTROL_TYPE controlType, out bool isAuto) {
            ASI_BOOL auto;
            int result;
            CheckReturn(ASIGetControlValue(cameraId, controlType, out result, out auto), MethodBase.GetCurrentMethod(), cameraId, controlType);
            isAuto = auto != ASI_BOOL.ASI_FALSE;
            return result;
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASISetControlValue(int iCameraID, ASI_CONTROL_TYPE controlType, int lValue, ASI_BOOL bAuto);

        public static void SetControlValue(int cameraId, ASI_CONTROL_TYPE controlType, int value, bool auto) {
            CheckReturn(ASISetControlValue(cameraId, controlType, value, auto ? ASI_BOOL.ASI_TRUE : ASI_BOOL.ASI_FALSE), MethodBase.GetCurrentMethod(), cameraId, controlType, value, auto);
        }



        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASISetROIFormat(int iCameraID, int iWidth, int iHeight, int iBin, ASI_IMG_TYPE Img_type);

        public static void SetROIFormat(int cameraId, Size size, int bin, ASI_IMG_TYPE imageType) {
            CheckReturn(ASISetROIFormat(cameraId, size.Width, size.Height, bin, imageType), MethodBase.GetCurrentMethod(), cameraId, size, bin, imageType);
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetROIFormat(int iCameraID, out int piWidth, out int piHeight, out int piBin, out ASI_IMG_TYPE pImg_type);

        public static Size GetROIFormat(int cameraId, out int bin, out ASI_IMG_TYPE imageType) {
            int width, height;
            CheckReturn(ASIGetROIFormat(cameraId, out width, out height, out bin, out imageType), MethodBase.GetCurrentMethod(), cameraId, bin);
            return new Size(width, height);
        }

        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASISetStartPos(int iCameraID, int iStartX, int iStartY);

        public static void SetStartPos(int cameraId, Point startPos) {
            CheckReturn(ASISetStartPos(cameraId, startPos.X, startPos.Y), MethodBase.GetCurrentMethod(), cameraId, startPos);
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetStartPos(int iCameraID, out int piStartX, out int piStartY);

        public static Point GetStartPos(int cameraId) {
            int x, y;
            CheckReturn(ASIGetStartPos(cameraId, out x, out y), MethodBase.GetCurrentMethod(), cameraId);
            return new Point(x, y);
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetDroppedFrames(int iCameraID, out int piDropFrames);

        public static int GetDroppedFrames(int cameraId) {
            int result;
            CheckReturn(ASIGetDroppedFrames(cameraId, out result), MethodBase.GetCurrentMethod(), cameraId);
            return result;
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIEnableDarkSubtract(int iCameraID, [MarshalAs(UnmanagedType.LPStr)] string pcBMPPath, out ASI_BOOL bIsSubDarkWorking);

        public static bool EnableDarkSubtract(int cameraId, string darkFilePath) {
            ASI_BOOL result;
            CheckReturn(ASIEnableDarkSubtract(cameraId, darkFilePath, out result), MethodBase.GetCurrentMethod(), cameraId, darkFilePath);
            return result != ASI_BOOL.ASI_FALSE;
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIDisableDarkSubtract(int iCameraID);

        public static void DisableDarkSubtract(int cameraId) {
            CheckReturn(ASIDisableDarkSubtract(cameraId), MethodBase.GetCurrentMethod(), cameraId);
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIStartVideoCapture(int iCameraID);

        public static void StartVideoCapture(int cameraId) {
            CheckReturn(ASIStartVideoCapture(cameraId), MethodBase.GetCurrentMethod(), cameraId);
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIStopVideoCapture(int iCameraID);

        public static void StopVideoCapture(int cameraId) {
            CheckReturn(ASIStopVideoCapture(cameraId), MethodBase.GetCurrentMethod(), cameraId);
        }

        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetVideoData(int iCameraID, IntPtr pBuffer, int lBuffSize, int iWaitms);

        public static bool GetVideoData(int cameraId, IntPtr buffer, int bufferSize, int waitMs) {
            var result = ASIGetVideoData(cameraId, buffer, bufferSize, waitMs);

            if (result == ASI_ERROR_CODE.ASI_ERROR_TIMEOUT)
                return false;

            CheckReturn(result, MethodBase.GetCurrentMethod(), cameraId, buffer, bufferSize, waitMs);
            return true;
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIPulseGuideOn(int iCameraID, ASI_GUIDE_DIRECTION direction);

        public static void PulseGuideOn(int cameraId, ASI_GUIDE_DIRECTION direction) {
            CheckReturn(ASIPulseGuideOn(cameraId, direction), MethodBase.GetCurrentMethod(), cameraId, direction);
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIPulseGuideOff(int iCameraID, ASI_GUIDE_DIRECTION direction);

        public static void PulseGuideOff(int cameraId, ASI_GUIDE_DIRECTION direction) {
            CheckReturn(ASIPulseGuideOff(cameraId, direction), MethodBase.GetCurrentMethod(), cameraId, direction);
        }



        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIStartExposure(int iCameraID, ASI_BOOL bIsDark);

        public static void StartExposure(int cameraId, bool isDark) {
            CheckReturn(ASIStartExposure(cameraId, isDark ? ASI_BOOL.ASI_TRUE : ASI_BOOL.ASI_FALSE), MethodBase.GetCurrentMethod(), cameraId, isDark);
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIStopExposure(int iCameraID);

        public static void StopExposure(int cameraId) {
            CheckReturn(ASIStopExposure(cameraId), MethodBase.GetCurrentMethod(), cameraId);
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetExpStatus(int iCameraID, out ExposureStatus pExpStatus);

        public static ExposureStatus GetExposureStatus(int cameraId) {
            ExposureStatus result;
            CheckReturn(ASIGetExpStatus(cameraId, out result), MethodBase.GetCurrentMethod(), cameraId);
            return result;
        }


        [DllImport("External/ASI/ASICamera2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetDataAfterExp(int iCameraID, IntPtr pBuffer, int lBuffSize);

        public static bool GetDataAfterExp(int cameraId, IntPtr buffer, int bufferSize) {
            var result = ASIGetDataAfterExp(cameraId, buffer, bufferSize);
            if (result == ASI_ERROR_CODE.ASI_ERROR_TIMEOUT)
                return false;

            CheckReturn(result, MethodBase.GetCurrentMethod(), cameraId, buffer, bufferSize);
            return true;
        }
    }

    [Serializable]
    public class ASICameraException : Exception {
        public ASICameraException(SerializationInfo info, StreamingContext context) : base(info, context) {

        }

        public ASICameraException(ASICameraDll.ASI_ERROR_CODE errorCode) : base(errorCode.ToString()) {
        }

        public ASICameraException(ASICameraDll.ASI_ERROR_CODE errorCode, MethodBase callingMethod, object[] parameters) : base(CreateMessage(errorCode, callingMethod, parameters)) {
        }

        private static string CreateMessage(ASICameraDll.ASI_ERROR_CODE errorCode, MethodBase callingMethod, object[] parameters) {
            StringBuilder bld = new StringBuilder();
            bld.AppendLine("Error '" + errorCode + "' from call to ");
            bld.Append("ASI" + callingMethod.Name + "(");
            var paramNames = callingMethod.GetParameters().Select(x => x.Name);
            foreach (var line in paramNames.Zip(parameters, (s, o) => string.Format("{0}={1}, ", s, o))) {
                bld.Append(line);
            }
            bld.Remove(bld.Length - 2, 2);
            bld.Append(")");
            return bld.ToString();
        }
    }
}
