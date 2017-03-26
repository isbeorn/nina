using System;
using System.Runtime.InteropServices;

namespace ZWOptical.ASISDK
{
    public partial class ASICameraDll
    {
        public enum ControlId
        {
            CONTROL_GAIN = 0,
            CONTROL_EXPOSURE,
            CONTROL_GAMMA,
            CONTROL_WB_R,
            CONTROL_WB_B,
            CONTROL_BRIGHTNESS,
            CONTROL_BANDWIDTHOVERLOAD,
            CONTROL_OVERCLOCK,
            CONTROL_TEMPERATURE,//return a temperature value multiplied 10
            CONTROL_HARDWAREBIN,
            CONTROL_HIGHSPEED,
            CONTROL_COOLERPOWERPERC,
            CONTROL_TARGETTEMP,
            CONTROL_COOLER_ON,
	    CONTROL_MONO_BIN,
	    CONTROL_FAN_ON
        }


        public enum IMG_TYPE
        {
            //Supported image type
            IMG_RAW8 = 0,
            IMG_RGB24,
            IMG_RAW16,
            IMG_Y8,
        }


        public enum GuideDirections
        {
            guideNorth = 0,
            guideSouth,
            guideEast,
            guideWest
        }

        public enum BayerPattern
        {
            BayerRG = 0,
            BayerBG,
            BayerGR,
            BayerGB
        };

        public enum EXPOSURE_STATUS
        {
	        EXP_IDLE = 0,
	        EXP_WORKING,
	        EXP_SUCCESS,
	        EXP_FAILED
        };

        // is control supported for the current camera
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return : MarshalAs(UnmanagedType.I1)]
        public static extern bool isAvailable(ControlId control);

        // get control current value
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getValue(ControlId control, [MarshalAs(UnmanagedType.I1)] out bool pbAuto);


        // get smallest possible value of control
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getMin(ControlId control);

        // get highest possible value of control
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getMax(ControlId control);

        // set current value of control
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void setValue(ControlId control, int value, [MarshalAs(UnmanagedType.I1)] bool auto);

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getNumberOfConnectedCameras();
        
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool openCamera(int camIndex);

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool initCamera();

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void closeCamera();

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool isColorCam();

        // get highest possible value of control
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern double getPixelSize();

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern BayerPattern getColorBayer();

        [DllImport("ASICamera.dll", CharSet = CharSet.Ansi, EntryPoint = "getCameraModel", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getCameraModel_impl(int camIndex);
        public static string getCameraModel(int camIndex)
        {
            return Marshal.PtrToStringAnsi(getCameraModel_impl(camIndex));
        }

        // max image width
        [DllImport("ASICamera.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getMaxWidth();

        // max image height
        [DllImport("ASICamera.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getMaxHeight();

        // get current width
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getWidth();

        // get current heigth
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getHeight();

        // get ROI start X
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getStartX();

        // get ROI start Y
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getStartY();

        //get the temp of sensor ,only ASI120 support
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern float getSensorTemp();

        //get Dropped frames 
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint getDroppedFrames();

        //Flip x and y
        [return: MarshalAs(UnmanagedType.I1)]
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SetMisc([MarshalAs(UnmanagedType.I1)] bool bFlipRow, [MarshalAs(UnmanagedType.I1)] bool bFlipColumn);

        //Get Flip setting
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetMisc([MarshalAs(UnmanagedType.I1)] out bool pbFlipRow, [MarshalAs(UnmanagedType.I1)] out bool pbFlipColumn);


        //whether the camera support bin2 or bin3
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool isBinSupported(int binning);

        //whether the camera support this img_type
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool isImgTypeSupported(IMG_TYPE img_type);

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool setStartPos(int startx, int starty);

        // setting new image format - depends on if you suport Format7
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool setImageFormat(int width, int height, int binning, IMG_TYPE img_type);

        //get the image type current set
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IMG_TYPE getImgType();

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void startCapture();

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void stopCapture();


        // wait waitms capture a single frame -1 means wait forever
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool getImageData(IntPtr buffer, int bufSize, int waitms);

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void pulseGuide(GuideDirections direction, int timems);

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool isAutoSupported(ControlId control);

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int getBin();

        //Starts an exposure, 0 means dark frame if there is shutter 
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void  startExposure();

        //EXPOSURE_STATUS GetExpStates();
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern EXPOSURE_STATUS getExpStatus();

        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void getImageAfterExp(IntPtr buffer, int bufSize);

	    //Stops the current exposure, if any. you can still get the image with the GetSingleImage API 
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void  stopExposure();

	    //check if the camera works at usb3 status
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool isUSB3Host();
 
        //check if this is a camera with cooler;
        [DllImport("ASICamera.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool isCoolerCam();

    }
}
