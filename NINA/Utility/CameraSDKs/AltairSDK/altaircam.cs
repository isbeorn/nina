using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NINA.Utility;
using System.IO;

#if !(NETFX_CORE || WINDOWS_UWP)

using System.Security.Permissions;
using System.Runtime.ConstrainedExecution;

#endif

/*
    Versin: 30.13270.2018.1102

    For Microsoft .NET Framework.

    We use P/Invoke to call into the altaircam.dll API, the c# class AltairCam is a thin wrapper class to the native api of altaircam.dll.
    So the manual en.html(English) and hans.html(Simplified Chinese) are also applicable for programming with altaircam.cs.
    See them in the 'doc' directory.
*/

namespace Altair {
#if !(NETFX_CORE || WINDOWS_UWP)

    public class SafeCamHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private const string DLLNAME = "altaircam.dll";

        static SafeCamHandle() {
            DllLoader.LoadDll(Path.Combine("Altair", DLLNAME));
        }

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern void Altaircam_Close(IntPtr h);

        public SafeCamHandle()
            : base(true) {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle() {
            // Here, we must obey all rules for constrained execution regions.
            Altaircam_Close(handle);
            return true;
        }
    };

#else
    public class SafeCamHandle : SafeHandle
    {
        private const string DLLNAME = "altaircam.dll";

        static SafeCamHandle() {
            DllLoader.LoadDll(Path.Combine("Altair", DLLNAME));
        }

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern void Altaircam_Close(IntPtr h);

        public SafeCamHandle()
            : base(IntPtr.Zero, true)
        {
        }

        override protected bool ReleaseHandle()
        {
            Altaircam_Close(handle);
            return true;
        }

        public override bool IsInvalid
        {
            get { return base.handle == IntPtr.Zero || base.handle == (IntPtr)(-1); }
        }
    };
#endif

    public class AltairCam : IDisposable {
        private const string DLLNAME = "altaircam.dll";

        static AltairCam() {
            DllLoader.LoadDll(Path.Combine("Altair", DLLNAME));
        }

        [Flags]
        public enum eFLAG : ulong {
            FLAG_CMOS = 0x00000001,   /* cmos sensor */
            FLAG_CCD_PROGRESSIVE = 0x00000002,   /* progressive ccd sensor */
            FLAG_CCD_INTERLACED = 0x00000004,   /* interlaced ccd sensor */
            FLAG_ROI_HARDWARE = 0x00000008,   /* support hardware ROI */
            FLAG_MONO = 0x00000010,   /* monochromatic */
            FLAG_BINSKIP_SUPPORTED = 0x00000020,   /* support bin/skip mode */
            FLAG_USB30 = 0x00000040,   /* usb3.0 */
            FLAG_TEC = 0x00000080,   /* Thermoelectric Cooler */
            FLAG_USB30_OVER_USB20 = 0x00000100,   /* usb3.0 camera connected to usb2.0 port */
            FLAG_ST4 = 0x00000200,   /* ST4 */
            FLAG_GETTEMPERATURE = 0x00000400,   /* support to get the temperature of the sensor */
            FLAG_PUTTEMPERATURE = 0x00000800,   /* support to put the target temperature of the sensor */
            FLAG_RAW10 = 0x00001000,   /* pixel format, RAW 10bits */
            FLAG_RAW12 = 0x00002000,   /* pixel format, RAW 12bits */
            FLAG_RAW14 = 0x00004000,   /* pixel format, RAW 14bits*/
            FLAG_RAW16 = 0x00008000,   /* pixel format, RAW 16bits */
            FLAG_FAN = 0x00010000,   /* cooling fan */
            FLAG_TEC_ONOFF = 0x00020000,   /* Thermoelectric Cooler can be turn on or off, support to set the target temperature of TEC */
            FLAG_ISP = 0x00040000,   /* ISP (Image Signal Processing) chip */
            FLAG_TRIGGER_SOFTWARE = 0x00080000,   /* support software trigger */
            FLAG_TRIGGER_EXTERNAL = 0x00100000,   /* support external trigger */
            FLAG_TRIGGER_SINGLE = 0x00200000,   /* only support trigger single: one trigger, one image */
            FLAG_BLACKLEVEL = 0x00400000,   /* support set and get the black level */
            FLAG_AUTO_FOCUS = 0x00800000,   /* support auto focus */
            FLAG_BUFFER = 0x01000000,   /* frame buffer */
            FLAG_DDR = 0x02000000,   /* use very large capacity DDR (Double Data Rate SDRAM) for frame buffer */
            FLAG_CG = 0x04000000,   /* support Conversion Gain mode: HCG, LCG */
            FLAG_YUV411 = 0x08000000,   /* pixel format, yuv411 */
            FLAG_VUYY = 0x10000000,   /* pixel format, yuv422, VUYY */
            FLAG_YUV444 = 0x20000000,   /* pixel format, yuv444 */
            FLAG_RGB888 = 0x40000000,   /* pixel format, RGB888 */

            [Obsolete("Use FLAG_RAW10")]
            FLAG_BITDEPTH10 = FLAG_RAW10,   /* obsolete, same as FLAG_RAW10 */

            [Obsolete("Use FLAG_RAW12")]
            FLAG_BITDEPTH12 = FLAG_RAW12,   /* obsolete, same as FLAG_RAW12 */

            [Obsolete("Use FLAG_RAW14")]
            FLAG_BITDEPTH14 = FLAG_RAW14,   /* obsolete, same as FLAG_RAW14 */

            [Obsolete("Use FLAG_RAW16")]
            FLAG_BITDEPTH16 = FLAG_RAW16,   /* obsolete, same as FLAG_RAW16 */

            FLAG_RAW8 = 0x80000000,   /* pixel format, RAW 8 bits */
            FLAG_GMCY8 = 0x0000000100000000,  /* pixel format, GMCY, 8 bits */
            FLAG_GMCY12 = 0x0000000200000000,  /* pixel format, GMCY, 12 bits */
            FLAG_UYVY = 0x0000000400000000,  /* pixel format, yuv422, UYVY */
            FLAG_CGHDR = 0x0000000800000000   /* Conversion Gain: HCG, LCG, HDR */
        };

        public enum eEVENT : uint {
            EVENT_EXPOSURE = 0x0001, /* exposure time changed */
            EVENT_TEMPTINT = 0x0002, /* white balance changed, Temp/Tint mode */
            EVENT_CHROME = 0x0003, /* reversed, do not use it */
            EVENT_IMAGE = 0x0004, /* live image arrived, use Altaircam_PullImage to get this image */
            EVENT_STILLIMAGE = 0x0005, /* snap (still) frame arrived, use Altaircam_PullStillImage to get this frame */
            EVENT_WBGAIN = 0x0006, /* white balance changed, RGB Gain mode */
            EVENT_TRIGGERFAIL = 0x0007, /* trigger failed */
            EVENT_BLACK = 0x0008, /* black balance changed */
            EVENT_FFC = 0x0009, /* flat field correction status changed */
            EVENT_DFC = 0x000a, /* dark field correction status changed */
            EVENT_ERROR = 0x0080, /* generic error */
            EVENT_DISCONNECTED = 0x0081, /* camera disconnected */
            EVENT_TIMEOUT = 0x0082, /* timeout error */
            EVENT_FACTORY = 0x8001  /* restore factory settings */
        };

        public enum ePROCESSMODE : uint {
            PROCESSMODE_FULL = 0x00, /* better image quality, more cpu usage. this is the default value */
            PROCESSMODE_FAST = 0x01 /* lower image quality, less cpu usage */
        };

        public enum eOPTION : uint {
            OPTION_NOFRAME_TIMEOUT = 0x01, /* 1 = enable; 0 = disable. default: disable */
            OPTION_THREAD_PRIORITY = 0x02, /* set the priority of the internal thread which grab data from the usb device. iValue: 0 = THREAD_PRIORITY_NORMAL; 1 = THREAD_PRIORITY_ABOVE_NORMAL; 2 = THREAD_PRIORITY_HIGHEST; default: 0; see: msdn SetThreadPriority */
            OPTION_PROCESSMODE = 0x03, /* 0 = better image quality, more cpu usage. this is the default value
                                               1 = lower image quality, less cpu usage */
            OPTION_RAW = 0x04, /* raw data mode, read the sensor "raw" data. This can be set only BEFORE Altaircam_StartXXX(). 0 = rgb, 1 = raw, default value: 0 */
            OPTION_HISTOGRAM = 0x05, /* 0 = only one, 1 = continue mode */
            OPTION_BITDEPTH = 0x06, /* 0 = 8 bits mode, 1 = 16 bits mode */
            OPTION_FAN = 0x07, /* 0 = turn off the cooling fan, [1, max] = fan speed */
            OPTION_TEC = 0x08, /* 0 = turn off the thermoelectric cooler, 1 = turn on the thermoelectric cooler */
            OPTION_LINEAR = 0x09, /* 0 = turn off the builtin linear tone mapping, 1 = turn on the builtin linear tone mapping, default value: 1 */
            OPTION_CURVE = 0x0a, /* 0 = turn off the builtin curve tone mapping, 1 = turn on the builtin polynomial curve tone mapping, 2 = logarithmic curve tone mapping, default value: 2 */
            OPTION_TRIGGER = 0x0b, /* 0 = video mode, 1 = software or simulated trigger mode, 2 = external trigger mode, default value = 0 */
            OPTION_RGB = 0x0c, /* 0 => RGB24; 1 => enable RGB48 format when bitdepth > 8; 2 => RGB32; 3 => 8 Bits Gray (only for mono camera); 4 => 16 Bits Gray (only for mono camera when bitdepth > 8) */
            OPTION_COLORMATIX = 0x0d, /* enable or disable the builtin color matrix, default value: 1 */
            OPTION_WBGAIN = 0x0e, /* enable or disable the builtin white balance gain, default value: 1 */
            OPTION_TECTARGET = 0x0f, /* get or set the target temperature of the thermoelectric cooler, in 0.1 degree Celsius. For example, 125 means 12.5 degree Celsius, -35 means -3.5 degree Celsius */
            OPTION_AGAIN = 0x10, /* enable or disable adjusting the analog gain when auto exposure is enabled. default value: enable */
            OPTION_FRAMERATE = 0x11, /* limit the frame rate, range=[0, 63], the default value 0 means no limit */
            OPTION_DEMOSAIC = 0x12, /* demosaic method for both video and still image: BILINEAR = 0, VNG(Variable Number of Gradients interpolation) = 1, PPG(Patterned Pixel Grouping interpolation) = 2, AHD(Adaptive Homogeneity-Directed interpolation) = 3, see https://en.wikipedia.org/wiki/Demosaicing, default value: 0 */
            OPTION_DEMOSAIC_VIDEO = 0x13, /* demosaic method for video */
            OPTION_DEMOSAIC_STILL = 0x14, /* demosaic method for still image */
            OPTION_BLACKLEVEL = 0x15, /* black level */
            OPTION_MULTITHREAD = 0x16, /* multithread image processing */
            OPTION_BINNING = 0x17, /* binning, 0x01 (no binning), 0x02 (add, 2*2), 0x03 (add, 3*3), 0x04 (add, 4*4), 0x82 (average, 2*2), 0x83 (average, 3*3), 0x84 (average, 4*4) */
            OPTION_ROTATE = 0x18, /* rotate clockwise: 0, 90, 180, 270 */
            OPTION_CG = 0x19, /* Conversion Gain mode: 0 = LCG, 1 = HCG, 2 = HDR */
            OPTION_PIXEL_FORMAT = 0x1a, /* pixel format */
            OPTION_FFC = 0x1b, /* flat field correction
                                                set:
                                                    0: disable
                                                    1: enable
                                                    -1: reset
                                                    (0xff000000 | n): set the average number to n, [1~255]
                                                get:
                                                    (val & 0xff): 0 -> disable, 1 -> enable, 2 -> inited
                                                    ((val & 0xff00) >> 8): sequence
                                                    ((val & 0xff0000) >> 8): average number
                                            */
            OPTION_DDR_DEPTH = 0x1c, /* the number of the frames that DDR can cache
                                                1: DDR cache only one frame
                                                0: Auto:
                                                    ->one for video mode when auto exposure is enabled
                                                    ->full capacity for others
                                                1: DDR can cache frames to full capacity
                                            */
            OPTION_DFC = 0x1d, /* dark field correction
                                                set:
                                                    0: disable
                                                    1: enable
                                                    -1: reset
                                                    (0xff000000 | n): set the average number to n, [1~255]
                                                get:
                                                    (val & 0xff): 0 -> disable, 1 -> enable, 2 -> inited
                                                    ((val & 0xff00) >> 8): sequence
                                                    ((val & 0xff0000) >> 8): average number
                                            */
            OPTION_SHARPENING = 0x1e, /* Sharpening: (threshold << 24) | (radius << 16) | strength)
                                                strength: [0, 500], default: 0 (disable)
                                                radius: [1, 10]
                                                threshold: [0, 255]
                                            */
            OPTION_FACTORY = 0x1f, /* restore the factory settings */
            OPTION_TEC_VOLTAGE = 0x20, /* get the current TEC voltage in 0.1V, 59 mean 5.9V; readonly */
            OPTION_TEC_VOLTAGE_MAX = 0x21, /* get the TEC maximum voltage in 0.1V; readonly */
            OPTION_DEVICE_RESET = 0x22, /* reset usb device, simulate a replug */
            OPTION_UPSIDE_DOWN = 0x23, /* upsize down:
                                                1: yes
                                                0: no
                                                default: 1 (win), 0 (linux/macos)
                                            */
        };

        public enum ePIXELFORMAT : uint {
            PIXELFORMAT_RAW8 = 0x00,
            PIXELFORMAT_RAW10 = 0x01,
            PIXELFORMAT_RAW12 = 0x02,
            PIXELFORMAT_RAW14 = 0x03,
            PIXELFORMAT_RAW16 = 0x04,
            PIXELFORMAT_YUV411 = 0x05,
            PIXELFORMAT_VUYY = 0x06,
            PIXELFORMAT_YUV444 = 0x07,
            PIXELFORMAT_RGB888 = 0x08,
            PIXELFORMAT_GMCY8 = 0x09,
            PIXELFORMAT_GMCY12 = 0x0a,
            PIXELFORMAT_UYVY = 0x0b
        };

        public enum eFRAMEINFO_FLAG : uint {
            FRAMEINFO_FLAG_SEQ = 0x01, /* sequence number */
            FRAMEINFO_FLAG_TIMESTAMP = 0x02
        };

        public enum eIoControType : uint {
            IOCONTROTYPE_GET_SUPPORTEDMODE = 0x01, /* 0x01->Input, 0x02->Output, (0x01 | 0x02)->support both Input and Output */
            IOCONTROTYPE_GET_ALLSTATUS = 0x02, /* A single bit field indicating the current logical state of all available line signals at time of polling */
            IOCONTROTYPE_GET_MODE = 0x03, /* 0x01->Input, 0x02->Output */
            IOCONTROTYPE_SET_MODE = 0x04,
            IOCONTROTYPE_GET_FORMAT = 0x05, /*
                                                                0x00-> not connected
                                                                0x01-> Tri-state: Tri-state mode (Not driven)
                                                                0x02-> TTL: TTL level signals
                                                                0x03-> LVDS: LVDS level signals
                                                                0x04-> RS-422: RS-422 level signals
                                                                0x05-> Opto-coupled
                                                            */
            IOCONTROTYPE_SET_FORMAT = 0x06,
            IOCONTROTYPE_GET_INVERTER = 0x07, /* boolean */
            IOCONTROTYPE_SET_INVERTER = 0x08,
            IOCONTROTYPE_GET_LOGIC = 0x09, /* 0x01->Positive, 0x02->Negative */
            IOCONTROTYPE_SET_LOGIC = 0x0a,
            IOCONTROTYPE_GET_MINIMUMOUTPUTPULSEWIDTH = 0x0b, /* minimum signal width of an output signal (in microseconds) */
            IOCONTROTYPE_SET_MINIMUMOUTPUTPULSEWIDTH = 0x0c,
            IOCONTROTYPE_GET_OVERLOADSTATUS = 0x0d, /* boolean */
            IOCONTROTYPE_SET_OVERLOADSTATUS = 0x0e,
            IOCONTROTYPE_GET_PITCH = 0x0f, /* Number of bytes separating the starting pixels of two consecutive lines */
            IOCONTROTYPE_SET_PITCH = 0x10,
            IOCONTROTYPE_GET_PITCHENABLE = 0x11, /* boolean */
            IOCONTROTYPE_SET_PITCHENABLE = 0x12,
            IOCONTROTYPE_GET_SOURCE = 0x13, /*
                                                                0->ExposureActive
                                                                1->TimerActive
                                                                2->UserOutput
                                                                3->TriggerReady
                                                                4->SerialTx
                                                                5->AcquisitionTriggerReady
                                                            */
            IOCONTROTYPE_SET_SOURCE = 0x14,
            IOCONTROTYPE_GET_STATUS = 0x15, /* boolean */
            IOCONTROTYPE_SET_STATUS = 0x16,
            IOCONTROTYPE_GET_DEBOUNCERTIME = 0x17, /* debouncer time in microseconds */
            IOCONTROTYPE_SET_DEBOUNCERTIME = 0x18
        };

        public const int TEC_TARGET_MIN = -300;
        public const int TEC_TARGET_DEF = -100;
        public const int TEC_TARGET_MAX = 300;

        public const uint MAX_AE_EXPTIME = 350000;  /* default: 350 ms */
        public const uint MAX_AE_AGAIN = 500;

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;

            public void Init() {
                biSize = (uint)Marshal.SizeOf(this);
            }
        }

        public struct Resolution {
            public uint width;
            public uint height;
        };

        public struct ModelV2 {
            public string name;
            public ulong flag;
            public uint maxspeed;
            public uint preview;
            public uint still;
            public uint maxfanspeed;
            public uint ioctrol;
            public float xpixsz;
            public float ypixsz;
            public Resolution[] res;
        };

        public struct InstanceV2 {
            public string displayname; /* display name */
            public string id; /* unique and opaque id of a connected camera */
            public ModelV2 model;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct FrameInfoV2 {
            public uint width;
            public uint height;
            public uint flag;      /* FRAMEINFO_FLAG_xxxx */
            public uint seq;       /* sequence number */
            public ulong timestamp; /* microsecond */
        };

        [Obsolete("Use ModelV2")]
        public struct Model {
            public string name;
            public uint flag;
            public uint maxspeed;
            public uint preview;
            public uint still;
            public Resolution[] res;
        };

        [Obsolete("Use InstanceV2")]
        public struct Instance {
            public string displayname; /* display name */
            public string id; /* unique and opaque id of a connected camera */
            public Model model;
        };

#if !(NETFX_CORE || WINDOWS_UWP)

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory")]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

#endif

        public delegate void DelegateEventCallback(eEVENT nEvent);

        public delegate void DelegateDataCallback(IntPtr pData, ref BITMAPINFOHEADER header, bool bSnap);

        public delegate void DelegateDataCallbackV2(IntPtr pData, ref FrameInfoV2 info, bool bSnap);

        public delegate void DelegateExposureCallback();

        public delegate void DelegateTempTintCallback(int nTemp, int nTint);

        public delegate void DelegateWhitebalanceCallback(int[] aGain);

        public delegate void DelegateHistogramCallback(float[] aHistY, float[] aHistR, float[] aHistG, float[] aHistB);

        public delegate void DelegateChromeCallback();

        public delegate void DelegateBlackbalanceCallback(ushort[] aSub);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PALTAIRCAM_DATA_CALLBACK(IntPtr pData, IntPtr pHeader, bool bSnap, IntPtr pCallbackCtx);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PALTAIRCAM_DATA_CALLBACK_V2(IntPtr pData, IntPtr pInfo, bool bSnap, IntPtr pCallbackCtx);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PIALTAIRCAM_EXPOSURE_CALLBACK(IntPtr pCtx);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PIALTAIRCAM_TEMPTINT_CALLBACK(int nTemp, int nTint, IntPtr pCtx);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PIALTAIRCAM_WHITEBALANCE_CALLBACK(IntPtr aGain, IntPtr pCtx);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PIALTAIRCAM_HISTOGRAM_CALLBACK(IntPtr aHistY, IntPtr aHistR, IntPtr aHistG, IntPtr aHistB, IntPtr pCtx);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PIALTAIRCAM_CHROME_CALLBACK(IntPtr pCtx);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PALTAIRCAM_EVENT_CALLBACK(eEVENT nEvent, IntPtr pCtx);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        internal delegate void PIALTAIRCAM_BLACKBALANCE_CALLBACK(IntPtr aSub, IntPtr pCtx);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int left, top, right, bottom;
        };

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr Altaircam_Version();

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall), Obsolete("Use Altaircam_EnumV2")]
        private static extern uint Altaircam_Enum(IntPtr ti);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_EnumV2(IntPtr ti);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern SafeCamHandle Altaircam_Open([MarshalAs(UnmanagedType.LPWStr)] string id);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_StartPullModeWithWndMsg(SafeCamHandle h, IntPtr hWnd, uint nMsg);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_StartPullModeWithCallback(SafeCamHandle h, PALTAIRCAM_EVENT_CALLBACK pEventCallback, IntPtr pCallbackCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_PullImage(SafeCamHandle h, IntPtr pImageData, int bits, out uint pnWidth, out uint pnHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_PullStillImage(SafeCamHandle h, IntPtr pImageData, int bits, out uint pnWidth, out uint pnHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_PullImageWithRowPitch(SafeCamHandle h, IntPtr pImageData, int bits, int rowPitch, out uint pnWidth, out uint pnHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_PullStillImageWithRowPitch(SafeCamHandle h, IntPtr pImageData, int bits, int rowPitch, out uint pnWidth, out uint pnHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_StartPushMode(SafeCamHandle h, PALTAIRCAM_DATA_CALLBACK pDataCallback, IntPtr pCallbackCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_PullImageV2(SafeCamHandle h, IntPtr pImageData, int bits, out FrameInfoV2 pInfo);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_PullStillImageV2(SafeCamHandle h, IntPtr pImageData, int bits, out FrameInfoV2 pInfo);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_PullImageWithRowPitchV2(SafeCamHandle h, IntPtr pImageData, int bits, int rowPitch, out FrameInfoV2 pInfo);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_PullStillImageWithRowPitchV2(SafeCamHandle h, IntPtr pImageData, int bits, int rowPitch, out FrameInfoV2 pInfo);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_StartPushModeV2(SafeCamHandle h, PALTAIRCAM_DATA_CALLBACK_V2 pDataCallback, IntPtr pCallbackCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_Stop(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_Pause(SafeCamHandle h, int bPause);

        /* for still image snap */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_Snap(SafeCamHandle h, uint nResolutionIndex);

        /*
            soft trigger:
            nNumber:    0xffff:     trigger continuously
                        0:          cancel trigger
                        others:     number of images to be triggered
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_Trigger(SafeCamHandle h, ushort nNumber);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Size(SafeCamHandle h, int nWidth, int nHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Size(SafeCamHandle h, out int nWidth, out int nHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_eSize(SafeCamHandle h, uint nResolutionIndex);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_eSize(SafeCamHandle h, out uint nResolutionIndex);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_ResolutionNumber(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_Resolution(SafeCamHandle h, uint nResolutionIndex, out int pWidth, out int pHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_ResolutionRatio(SafeCamHandle h, uint nResolutionIndex, out int pNumerator, out int pDenominator);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_Field(SafeCamHandle h);

        /*
            FourCC:
                MAKEFOURCC('G', 'B', 'R', 'G')
                MAKEFOURCC('R', 'G', 'G', 'B')
                MAKEFOURCC('B', 'G', 'G', 'R')
                MAKEFOURCC('G', 'R', 'B', 'G')
                MAKEFOURCC('Y', 'U', 'Y', 'V')
                MAKEFOURCC('Y', 'Y', 'Y', 'Y')
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_RawFormat(SafeCamHandle h, out uint nFourCC, out uint bitdepth);

        /*
            set or get the process mode: ALTAIRCAM_PROCESSMODE_FULL or ALTAIRCAM_PROCESSMODE_FAST
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_ProcessMode(SafeCamHandle h, ePROCESSMODE nProcessMode);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_ProcessMode(SafeCamHandle h, out ePROCESSMODE pnProcessMode);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_RealTime(SafeCamHandle h, int bEnable);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_RealTime(SafeCamHandle h, out int bEnable);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_Flush(SafeCamHandle h);

        /* sensor Temperature */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Temperature(SafeCamHandle h, out short pTemperature);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Temperature(SafeCamHandle h, short nTemperature);

        /* ROI */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Roi(SafeCamHandle h, out uint pxOffset, out uint pyOffset, out uint pxWidth, out uint pyHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Roi(SafeCamHandle h, uint xOffset, uint yOffset, uint xWidth, uint yHeight);

        /*
            ------------------------------------------------------------------|
            | Parameter               |   Range       |   Default             |
            |-----------------------------------------------------------------|
            | Auto Exposure Target    |   16~235      |   120                 |
            | Temp                    |   2000~15000  |   6503                |
            | Tint                    |   200~2500    |   1000                |
            | LevelRange              |   0~255       |   Low = 0, High = 255 |
            | Contrast                |   -100~100    |   0                   |
            | Hue                     |   -180~180    |   0                   |
            | Saturation              |   0~255       |   128                 |
            | Brightness              |   -64~64      |   0                   |
            | Gamma                   |   20~180      |   100                 |
            | WBGain                  |   -127~127    |   0                   |
            ------------------------------------------------------------------|
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_AutoExpoEnable(SafeCamHandle h, out int bAutoExposure);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_AutoExpoEnable(SafeCamHandle h, int bAutoExposure);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_AutoExpoTarget(SafeCamHandle h, out ushort Target);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_AutoExpoTarget(SafeCamHandle h, ushort Target);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_MaxAutoExpoTimeAGain(SafeCamHandle h, uint maxTime, ushort maxAGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_ExpoTime(SafeCamHandle h, out uint Time)/* in microseconds */;

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_ExpoTime(SafeCamHandle h, uint Time)/* inmicroseconds */;

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_ExpTimeRange(SafeCamHandle h, out uint nMin, out uint nMax, out uint nDef);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_ExpoAGain(SafeCamHandle h, out ushort AGain);/* percent, such as 300 */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_ExpoAGain(SafeCamHandle h, ushort AGain);/* percent */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_ExpoAGainRange(SafeCamHandle h, out ushort nMin, out ushort nMax, out ushort nDef);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_LevelRange(SafeCamHandle h, [In] ushort[] aLow, [In] ushort[] aHigh);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_LevelRange(SafeCamHandle h, [Out] ushort[] aLow, [Out] ushort[] aHigh);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Hue(SafeCamHandle h, int Hue);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Hue(SafeCamHandle h, out int Hue);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Saturation(SafeCamHandle h, int Saturation);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Saturation(SafeCamHandle h, out int Saturation);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Brightness(SafeCamHandle h, int Brightness);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Brightness(SafeCamHandle h, out int Brightness);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Contrast(SafeCamHandle h, out int Contrast);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Contrast(SafeCamHandle h, int Contrast);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Gamma(SafeCamHandle h, out int Gamma);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Gamma(SafeCamHandle h, int Gamma);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Chrome(SafeCamHandle h, out int bChrome);    /* monochromatic mode */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Chrome(SafeCamHandle h, int bChrome);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_VFlip(SafeCamHandle h, out int bVFlip);  /* vertical flip */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_VFlip(SafeCamHandle h, int bVFlip);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_HFlip(SafeCamHandle h, out int bHFlip);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_HFlip(SafeCamHandle h, int bHFlip);  /* horizontal flip */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Negative(SafeCamHandle h, out int bNegative);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Negative(SafeCamHandle h, int bNegative);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Speed(SafeCamHandle h, ushort nSpeed);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Speed(SafeCamHandle h, out ushort pSpeed);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_MaxSpeed(SafeCamHandle h);/* get the maximum speed, "Frame Speed Level", speed range = [0, max] */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_MaxBitDepth(SafeCamHandle h);/* get the max bit depth of this camera, such as 8, 10, 12, 14, 16 */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_FanMaxSpeed(SafeCamHandle h);/* get the maximum fan speed, the fan speed range = [0, max], closed interval */

        /* power supply:
                0 -> 60HZ AC
                1 -> 50Hz AC
                2 -> DC
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_HZ(SafeCamHandle h, int nHZ);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_HZ(SafeCamHandle h, out int nHZ);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Mode(SafeCamHandle h, int bSkip); /* skip or bin */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Mode(SafeCamHandle h, out int bSkip);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_TempTint(SafeCamHandle h, int nTemp, int nTint);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_TempTint(SafeCamHandle h, out int nTemp, out int nTint);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_WhiteBalanceGain(SafeCamHandle h, [In] int[] aGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_WhiteBalanceGain(SafeCamHandle h, [Out] int[] aGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_BlackBalance(SafeCamHandle h, [In] ushort[] aSub);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_BlackBalance(SafeCamHandle h, [Out] ushort[] aSub);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_AWBAuxRect(SafeCamHandle h, ref RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_AWBAuxRect(SafeCamHandle h, out RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_AEAuxRect(SafeCamHandle h, ref RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_AEAuxRect(SafeCamHandle h, out RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_ABBAuxRect(SafeCamHandle h, ref RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_ABBAuxRect(SafeCamHandle h, out RECT pAuxRect);

        /*
            S_FALSE:    color mode
            S_OK:       mono mode, such as EXCCD00300KMA and UHCCD01400KMA
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_MonoMode(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint Altaircam_get_StillResolutionNumber(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_StillResolution(SafeCamHandle h, uint nResolutionIndex, out int pWidth, out int pHeight);

        /*
            get the revision
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Revision(SafeCamHandle h, out ushort pRevision);

        /*
            get the serial number which is always 32 chars which is zero-terminated such as "TP110826145730ABCD1234FEDC56787"
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_SerialNumber(SafeCamHandle h, IntPtr sn);

        /*
            get the camera firmware version, such as: 3.2.1.20140922
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_FwVersion(SafeCamHandle h, IntPtr fwver);

        /*
            get the camera hardware version, such as: 3.2.1.20140922
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_HwVersion(SafeCamHandle h, IntPtr hwver);

        /*
            get the FPGA version, such as: 1.3
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_FpgaVersion(SafeCamHandle h, IntPtr fpgaver);

        /*
            get the production date, such as: 20150327
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_ProductionDate(SafeCamHandle h, IntPtr pdate);

        /*
            get the sensor pixel size, such as: 2.4um
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_PixelSize(SafeCamHandle h, uint nResolutionIndex, out float x, out float y);

        /*
                    ------------------------------------------------------------|
                    | Parameter         |   Range       |   Default             |
                    |-----------------------------------------------------------|
                    | VidgetAmount      |   -100~100    |   0                   |
                    | VignetMidPoint    |   0~100       |   50                  |
                    -------------------------------------------------------------
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_VignetEnable(SafeCamHandle h, int bEnable);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_VignetEnable(SafeCamHandle h, out int bEnable);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_VignetAmountInt(SafeCamHandle h, int nAmount);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_VignetAmountInt(SafeCamHandle h, out int nAmount);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_VignetMidPointInt(SafeCamHandle h, int nMidPoint);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_VignetMidPointInt(SafeCamHandle h, out int nMidPoint);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_ExpoCallback(SafeCamHandle h, PIALTAIRCAM_EXPOSURE_CALLBACK fnExpoProc, IntPtr pExpoCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_ChromeCallback(SafeCamHandle h, PIALTAIRCAM_CHROME_CALLBACK fnChromeProc, IntPtr pChromeCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_AwbOnePush(SafeCamHandle h, PIALTAIRCAM_TEMPTINT_CALLBACK fnTTProc, IntPtr pTTCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_AwbInit(SafeCamHandle h, PIALTAIRCAM_WHITEBALANCE_CALLBACK fnWBProc, IntPtr pWBCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_LevelRangeAuto(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_GetHistogram(SafeCamHandle h, PIALTAIRCAM_HISTOGRAM_CALLBACK fnHistogramProc, IntPtr pHistogramCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_AbbOnePush(SafeCamHandle h, PIALTAIRCAM_BLACKBALANCE_CALLBACK fnBBProc, IntPtr pBBCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_LEDState(SafeCamHandle h, ushort iLed, ushort iState, ushort iPeriod);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_write_EEPROM(SafeCamHandle h, uint addr, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_read_EEPROM(SafeCamHandle h, uint addr, IntPtr pBuffer, uint nBufferLen);

        [DllImport("libaltaircam.so", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Altaircam_write_Pipe(SafeCamHandle h, uint pipeNum, IntPtr pBuffer, uint nBufferLen);

        [DllImport("libaltaircam.so", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Altaircam_read_Pipe(SafeCamHandle h, uint pipeNum, IntPtr pBuffer, uint nBufferLen);

        [DllImport("libaltaircam.so", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Altaircam_feed_Pipe(SafeCamHandle h, uint pipeNum);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_write_UART(SafeCamHandle h, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_read_UART(SafeCamHandle h, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Option(SafeCamHandle h, eOPTION iOption, int iValue);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_Option(SafeCamHandle h, eOPTION iOption, out int iValue);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Linear(SafeCamHandle h, byte[] v8, ushort[] v16);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_Curve(SafeCamHandle h, byte[] v8, ushort[] v16);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_ColorMatrix(SafeCamHandle h, double[] v);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_put_InitWBGain(SafeCamHandle h, ushort[] v);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_get_FrameRate(SafeCamHandle h, out uint nFrame, out uint nTime, out uint nTotalFrame);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_FfcOnePush(SafeCamHandle h);

        [DllImport("libaltaircam.so", ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_DfcOnePush(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int Altaircam_IoControl(SafeCamHandle h, uint index, eIoControType eType, int outVal, out int inVal);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern double Altaircam_calc_ClarityFactor(IntPtr pImageData, int bits, uint nImgWidth, uint nImgHeight);

        static public uint MAKEFOURCC(uint a, uint b, uint c, uint d) {
            return ((uint)(byte)(a) | ((uint)(byte)(b) << 8) | ((uint)(byte)(c) << 16) | ((uint)(byte)(d) << 24));
        }

        private SafeCamHandle _handle;
        private GCHandle _gchandle;
        private DelegateDataCallback _dDataCallback;
        private DelegateDataCallbackV2 _dDataCallbackV2;
        private DelegateEventCallback _dEventCallback;
        private DelegateExposureCallback _dExposureCallback;
        private DelegateTempTintCallback _dTempTintCallback;
        private DelegateWhitebalanceCallback _dWhitebalanceCallback;
        private DelegateBlackbalanceCallback _dBlackbalanceCallback;
        private DelegateHistogramCallback _dHistogramCallback;
        private DelegateChromeCallback _dChromeCallback;
        private PALTAIRCAM_DATA_CALLBACK _pDataCallback;
        private PALTAIRCAM_DATA_CALLBACK_V2 _pDataCallbackV2;
        private PALTAIRCAM_EVENT_CALLBACK _pEventCallback;
        private PIALTAIRCAM_EXPOSURE_CALLBACK _pExposureCallback;
        private PIALTAIRCAM_TEMPTINT_CALLBACK _pTempTintCallback;
        private PIALTAIRCAM_WHITEBALANCE_CALLBACK _pWhitebalanceCallback;
        private PIALTAIRCAM_BLACKBALANCE_CALLBACK _pBlackbalanceCallback;
        private PIALTAIRCAM_HISTOGRAM_CALLBACK _pHistogramCallback;
        private PIALTAIRCAM_CHROME_CALLBACK _pChromeCallback;

        private void EventCallback(eEVENT nEvent) {
            if (_dEventCallback != null)
                _dEventCallback(nEvent);
        }

        private void DataCallback(IntPtr pData, IntPtr pHeader, bool bSnap) {
            if (pData == IntPtr.Zero || pHeader == IntPtr.Zero) /* pData == 0 means that something error, we callback to tell the application */
            {
                if (_dDataCallback != null) {
                    BITMAPINFOHEADER h = new BITMAPINFOHEADER();
                    _dDataCallback(IntPtr.Zero, ref h, bSnap);
                }
            } else {
#if !(NETFX_CORE || WINDOWS_UWP)
                BITMAPINFOHEADER h = (BITMAPINFOHEADER)Marshal.PtrToStructure(pHeader, typeof(BITMAPINFOHEADER));
#else
                BITMAPINFOHEADER h = Marshal.PtrToStructure<BITMAPINFOHEADER>(pHeader);
#endif
                if (_dDataCallback != null)
                    _dDataCallback(pData, ref h, bSnap);
            }
        }

        private void DataCallbackV2(IntPtr pData, IntPtr pInfo, bool bSnap) {
            if (pData == IntPtr.Zero || pInfo == IntPtr.Zero) /* pData == 0 means that something error, we callback to tell the application */
            {
                if (_dDataCallbackV2 != null) {
                    FrameInfoV2 info = new FrameInfoV2();
                    _dDataCallbackV2(IntPtr.Zero, ref info, bSnap);
                }
            } else {
#if !(NETFX_CORE || WINDOWS_UWP)
                FrameInfoV2 info = (FrameInfoV2)Marshal.PtrToStructure(pInfo, typeof(FrameInfoV2));
#else
                FrameInfoV2 info = Marshal.PtrToStructure<FrameInfoV2>(pInfo);
#endif
                if (_dDataCallbackV2 != null)
                    _dDataCallbackV2(pData, ref info, bSnap);
            }
        }

        private void ExposureCallback() {
            if (_dExposureCallback != null)
                _dExposureCallback();
        }

        private void TempTintCallback(int nTemp, int nTint) {
            if (_dTempTintCallback != null) {
                _dTempTintCallback(nTemp, nTint);
                _dTempTintCallback = null;
            }
            _pTempTintCallback = null;
        }

        private void WhitebalanceCallback(int[] aGain) {
            if (_dWhitebalanceCallback != null) {
                _dWhitebalanceCallback(aGain);
                _dWhitebalanceCallback = null;
            }
            _pWhitebalanceCallback = null;
        }

        private void BlackbalanceCallback(ushort[] aSub) {
            if (_dBlackbalanceCallback != null) {
                _dBlackbalanceCallback(aSub);
                _dBlackbalanceCallback = null;
            }
            _pBlackbalanceCallback = null;
        }

        private void ChromeCallback() {
            if (_dChromeCallback != null)
                _dChromeCallback();
        }

        private void HistogramCallback(float[] aHistY, float[] aHistR, float[] aHistG, float[] aHistB) {
            if (_dHistogramCallback != null) {
                _dHistogramCallback(aHistY, aHistR, aHistG, aHistB);
                _dHistogramCallback = null;
            }
            _pHistogramCallback = null;
        }

        private static void DataCallback(IntPtr pData, IntPtr pHeader, bool bSnap, IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null)
                    pthis.DataCallback(pData, pHeader, bSnap);
            }
        }

        private static void DataCallbackV2(IntPtr pData, IntPtr pInfo, bool bSnap, IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null)
                    pthis.DataCallbackV2(pData, pInfo, bSnap);
            }
        }

        private static void EventCallback(eEVENT nEvent, IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null)
                    pthis.EventCallback(nEvent);
            }
        }

        private static void ExposureCallback(IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null)
                    pthis.ExposureCallback();
            }
        }

        private static void TempTintCallback(int nTemp, int nTint, IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null)
                    pthis.TempTintCallback(nTemp, nTint);
            }
        }

        private static void WhitebalanceCallback(IntPtr aGain, IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null) {
                    int[] newGain = new int[3];
                    Marshal.Copy(aGain, newGain, 0, 3);
                    pthis.WhitebalanceCallback(newGain);
                }
            }
        }

        private static void BlackbalanceCallback(IntPtr aSub, IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null) {
                    short[] newSub = new short[3];
                    ushort[] newuSub = new ushort[3];
                    Marshal.Copy(aSub, newSub, 0, 3);
                    newuSub[0] = (ushort)newSub[0];
                    newuSub[1] = (ushort)newSub[1];
                    newuSub[2] = (ushort)newSub[2];
                    pthis.BlackbalanceCallback(newuSub);
                }
            }
        }

        private static void ChromeCallback(IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null)
                    pthis.ChromeCallback();
            }
        }

        private static void HistogramCallback(IntPtr aHistY, IntPtr aHistR, IntPtr aHistG, IntPtr aHistB, IntPtr pCallbackCtx) {
            GCHandle gch = GCHandle.FromIntPtr(pCallbackCtx);
            if (gch != null) {
                AltairCam pthis = gch.Target as AltairCam;
                if (pthis != null) {
                    float[] arrHistY = new float[256];
                    float[] arrHistR = new float[256];
                    float[] arrHistG = new float[256];
                    float[] arrHistB = new float[256];
                    Marshal.Copy(aHistY, arrHistY, 0, 256);
                    Marshal.Copy(aHistR, arrHistR, 0, 256);
                    Marshal.Copy(aHistG, arrHistG, 0, 256);
                    Marshal.Copy(aHistB, arrHistB, 0, 256);
                    pthis.HistogramCallback(arrHistY, arrHistR, arrHistG, arrHistB);
                }
            }
        }

        ~AltairCam() {
            Dispose(false);
        }

#if !(NETFX_CORE || WINDOWS_UWP)

        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
#endif
        protected virtual void Dispose(bool disposing) {
            // Note there are three interesting states here:
            // 1) CreateFile failed, _handle contains an invalid handle
            // 2) We called Dispose already, _handle is closed.
            // 3) _handle is null, due to an async exception before
            //    calling CreateFile. Note that the finalizer runs
            //    if the constructor fails.
            if (_handle != null && !_handle.IsInvalid) {
                // Free the handle
                _handle.Dispose();
            }
            // SafeHandle records the fact that we've called Dispose.
        }

        public void Dispose()  // Follow the Dispose pattern - public nonvirtual.
        {
            Dispose(true);
            if (_gchandle.IsAllocated)
                _gchandle.Free();
            GC.SuppressFinalize(this);
        }

        public void Close() {
            Dispose();
        }

        /* get the version of this dll, which is: 30.13270.2018.1102 */

        public static string Version() {
            return Marshal.PtrToStringUni(Altaircam_Version());
        }

        /* enumerate AltairCam cameras that are currently connected to computer */

        public static InstanceV2[] EnumV2() {
            IntPtr ti = Marshal.AllocHGlobal(512 * 16);
            uint cnt = Altaircam_EnumV2(ti);
            InstanceV2[] arr = new InstanceV2[cnt];
            if (cnt != 0) {
                float[] tmp = new float[1];
                Int64 p = ti.ToInt64();
                for (uint i = 0; i < cnt; ++i) {
                    arr[i].displayname = Marshal.PtrToStringUni((IntPtr)p);
                    p += sizeof(char) * 64;
                    arr[i].id = Marshal.PtrToStringUni((IntPtr)p);
                    p += sizeof(char) * 64;

                    IntPtr pm = Marshal.ReadIntPtr((IntPtr)p);
                    p += IntPtr.Size;

                    {
                        Int64 q = pm.ToInt64();
                        IntPtr pmn = Marshal.ReadIntPtr((IntPtr)q);
                        arr[i].model.name = Marshal.PtrToStringUni(pmn);
                        q += IntPtr.Size;
                        if (4 == IntPtr.Size)   /* 32bits windows */
                            q += 4; //skip 4 bytes, different from the linux version
                        arr[i].model.flag = (ulong)Marshal.ReadInt64((IntPtr)q);
                        q += sizeof(long);
                        arr[i].model.maxspeed = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);
                        arr[i].model.preview = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);
                        arr[i].model.still = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);
                        arr[i].model.maxfanspeed = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);
                        arr[i].model.ioctrol = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);
                        Marshal.Copy((IntPtr)q, tmp, 0, 1);
                        arr[i].model.xpixsz = tmp[0];
                        q += sizeof(float);
                        Marshal.Copy((IntPtr)q, tmp, 0, 1);
                        arr[i].model.ypixsz = tmp[0];
                        q += sizeof(float);
                        uint resn = Math.Max(arr[i].model.preview, arr[i].model.still);
                        arr[i].model.res = new Resolution[resn];
                        for (uint j = 0; j < resn; ++j) {
                            arr[i].model.res[j].width = (uint)Marshal.ReadInt32((IntPtr)q);
                            q += sizeof(int);
                            arr[i].model.res[j].height = (uint)Marshal.ReadInt32((IntPtr)q);
                            q += sizeof(int);
                        }
                    }
                }
            }
            Marshal.FreeHGlobal(ti);
            return arr;
        }

        [Obsolete("Use EnumV2")]
        public static Instance[] Enum() {
            IntPtr ti = Marshal.AllocHGlobal(512 * 16);
            uint cnt = Altaircam_Enum(ti);
            Instance[] arr = new Instance[cnt];
            if (cnt != 0) {
                Int64 p = ti.ToInt64();
                for (uint i = 0; i < cnt; ++i) {
                    arr[i].displayname = Marshal.PtrToStringUni((IntPtr)p);
                    p += sizeof(char) * 64;
                    arr[i].id = Marshal.PtrToStringUni((IntPtr)p);
                    p += sizeof(char) * 64;

                    IntPtr pm = Marshal.ReadIntPtr((IntPtr)p);
                    p += IntPtr.Size;

                    {
                        Int64 q = pm.ToInt64();
                        IntPtr pmn = Marshal.ReadIntPtr((IntPtr)q);
                        arr[i].model.name = Marshal.PtrToStringUni(pmn);
                        q += IntPtr.Size;
                        arr[i].model.flag = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);
                        arr[i].model.maxspeed = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);
                        arr[i].model.preview = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);
                        arr[i].model.still = (uint)Marshal.ReadInt32((IntPtr)q);
                        q += sizeof(int);

                        uint resn = Math.Max(arr[i].model.preview, arr[i].model.still);
                        arr[i].model.res = new Resolution[resn];
                        for (uint j = 0; j < resn; ++j) {
                            arr[i].model.res[j].width = (uint)Marshal.ReadInt32((IntPtr)q);
                            q += sizeof(int);
                            arr[i].model.res[j].height = (uint)Marshal.ReadInt32((IntPtr)q);
                            q += sizeof(int);
                        }
                    }
                }
            }
            Marshal.FreeHGlobal(ti);
            return arr;
        }

        // id: enumerated by EnumV2
        public bool Open(string id) {
            SafeCamHandle tmphandle = Altaircam_Open(id);
            if (tmphandle == null || tmphandle.IsInvalid || tmphandle.IsClosed)
                return false;
            _handle = tmphandle;
            _gchandle = GCHandle.Alloc(this);
            return true;
        }

        public SafeCamHandle Handle {
            get {
                return _handle;
            }
        }

        public uint ResolutionNumber {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_ResolutionNumber(_handle);
            }
        }

        public uint StillResolutionNumber {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_StillResolutionNumber(_handle);
            }
        }

        public bool MonoMode {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return false;
                return (0 == Altaircam_get_MonoMode(_handle));
            }
        }

        /* get the maximum speed, "Frame Speed Level" */

        public uint MaxSpeed {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_MaxSpeed(_handle);
            }
        }

        /* get the max bit depth of this camera, such as 8, 10, 12, 14, 16 */

        public uint MaxBitDepth {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_MaxBitDepth(_handle);
            }
        }

        /* get the maximum fan speed, the fan speed range = [0, max], closed interval */

        public uint FanMaxSpeed {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_FanMaxSpeed(_handle);
            }
        }

        /* get the revision */

        public ushort Revision {
            get {
                ushort rev = 0;
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return rev;

                Altaircam_get_Revision(_handle, out rev);
                return rev;
            }
        }

        /* get the serial number which is always 32 chars which is zero-terminated such as "TP110826145730ABCD1234FEDC56787" */

        public string SerialNumber {
            get {
                string sn = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return sn;
                IntPtr ptr = Marshal.AllocHGlobal(64);
                if (Altaircam_get_SerialNumber(_handle, ptr) < 0)
                    sn = "";
                else
                    sn = Marshal.PtrToStringAnsi(ptr);

                Marshal.FreeHGlobal(ptr);
                return sn;
            }
        }

        /* get the camera firmware version, such as: 3.2.1.20140922 */

        public string FwVersion {
            get {
                string fwver = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return fwver;
                IntPtr ptr = Marshal.AllocHGlobal(32);
                if (Altaircam_get_FwVersion(_handle, ptr) < 0)
                    fwver = "";
                else
                    fwver = Marshal.PtrToStringAnsi(ptr);

                Marshal.FreeHGlobal(ptr);
                return fwver;
            }
        }

        /* get the camera hardware version, such as: 3.2.1.20140922 */

        public string HwVersion {
            get {
                string hwver = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return hwver;
                IntPtr ptr = Marshal.AllocHGlobal(32);
                if (Altaircam_get_HwVersion(_handle, ptr) < 0)
                    hwver = "";
                else
                    hwver = Marshal.PtrToStringAnsi(ptr);

                Marshal.FreeHGlobal(ptr);
                return hwver;
            }
        }

        /* such as: 20150327 */

        public string ProductionDate {
            get {
                string pdate = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return pdate;
                IntPtr ptr = Marshal.AllocHGlobal(32);
                if (Altaircam_get_ProductionDate(_handle, ptr) < 0)
                    pdate = "";
                else
                    pdate = Marshal.PtrToStringAnsi(ptr);

                Marshal.FreeHGlobal(ptr);
                return pdate;
            }
        }

        /* such as: 1.3 */

        public string FpgaVersion {
            get {
                string fpgaver = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return fpgaver;
                IntPtr ptr = Marshal.AllocHGlobal(32);
                if (Altaircam_get_FpgaVersion(_handle, ptr) < 0)
                    fpgaver = "";
                else
                    fpgaver = Marshal.PtrToStringAnsi(ptr);

                Marshal.FreeHGlobal(ptr);
                return fpgaver;
            }
        }

        public uint Field {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return (uint)Altaircam_get_Field(_handle);
            }
        }

#if !(NETFX_CORE || WINDOWS_UWP)

        public bool StartPullModeWithWndMsg(IntPtr hWnd, uint nMsg) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            return (Altaircam_StartPullModeWithWndMsg(_handle, hWnd, nMsg) >= 0);
        }

#endif

        public bool StartPullModeWithCallback(DelegateEventCallback edelegate) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dEventCallback = edelegate;
            if (edelegate != null) {
                _pEventCallback = new PALTAIRCAM_EVENT_CALLBACK(EventCallback);
                return (Altaircam_StartPullModeWithCallback(_handle, _pEventCallback, GCHandle.ToIntPtr(_gchandle)) >= 0);
            } else {
                return (Altaircam_StartPullModeWithCallback(_handle, null, IntPtr.Zero) >= 0);
            }
        }

        /*  bits: 24 (RGB24), 32 (RGB32), 8 (Gray) or 16 (Gray) */

        public bool PullImage(IntPtr pImageData, int bits, out uint pnWidth, out uint pnHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pnWidth = pnHeight = 0;
                return false;
            }

            return (Altaircam_PullImage(_handle, pImageData, bits, out pnWidth, out pnHeight) >= 0);
        }

        public bool PullImageV2(IntPtr pImageData, int bits, out FrameInfoV2 pInfo) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pInfo.width = pInfo.height = pInfo.flag = pInfo.seq = 0;
                pInfo.timestamp = 0;
                return false;
            }

            return (Altaircam_PullImageV2(_handle, pImageData, bits, out pInfo) >= 0);
        }

        /*  bits: 24 (RGB24), 32 (RGB32), 8 (Gray) or 16 (Gray) */

        public bool PullStillImage(IntPtr pImageData, int bits, out uint pnWidth, out uint pnHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pnWidth = pnHeight = 0;
                return false;
            }

            return (Altaircam_PullStillImage(_handle, pImageData, bits, out pnWidth, out pnHeight) >= 0);
        }

        public bool PullStillImageV2(IntPtr pImageData, int bits, out FrameInfoV2 pInfo) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pInfo.width = pInfo.height = pInfo.flag = pInfo.seq = 0;
                pInfo.timestamp = 0;
                return false;
            }

            return (Altaircam_PullStillImageV2(_handle, pImageData, bits, out pInfo) >= 0);
        }

        /*  bits: 24 (RGB24), 32 (RGB32), 8 (Gray) or 16 (Gray)
            rowPitch: The distance from one row of to the next row. rowPitch = 0 means using the default row pitch
        */

        public bool PullImageWithRowPitch(IntPtr pImageData, int bits, int rowPitch, out uint pnWidth, out uint pnHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pnWidth = pnHeight = 0;
                return false;
            }

            return (Altaircam_PullImageWithRowPitch(_handle, pImageData, bits, rowPitch, out pnWidth, out pnHeight) >= 0);
        }

        public bool PullImageWithRowPitchV2(IntPtr pImageData, int bits, int rowPitch, out FrameInfoV2 pInfo) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pInfo.width = pInfo.height = pInfo.flag = pInfo.seq = 0;
                pInfo.timestamp = 0;
                return false;
            }

            return (Altaircam_PullImageWithRowPitchV2(_handle, pImageData, bits, rowPitch, out pInfo) >= 0);
        }

        /*  bits: 24 (RGB24), 32 (RGB32), 8 (Gray) or 16 (Gray)
            rowPitch: The distance from one row of to the next row. rowPitch = 0 means using the default row pitch
        */

        public bool PullStillImageWithRowPitch(IntPtr pImageData, int bits, int rowPitch, out uint pnWidth, out uint pnHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pnWidth = pnHeight = 0;
                return false;
            }

            return (Altaircam_PullStillImageWithRowPitch(_handle, pImageData, bits, rowPitch, out pnWidth, out pnHeight) >= 0);
        }

        public bool PullStillImageWithRowPitchV2(IntPtr pImageData, int bits, int rowPitch, out FrameInfoV2 pInfo) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pInfo.width = pInfo.height = pInfo.flag = pInfo.seq = 0;
                pInfo.timestamp = 0;
                return false;
            }

            return (Altaircam_PullStillImageWithRowPitchV2(_handle, pImageData, bits, rowPitch, out pInfo) >= 0);
        }

        public bool StartPushMode(DelegateDataCallback ddelegate) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dDataCallback = ddelegate;
            _pDataCallback = new PALTAIRCAM_DATA_CALLBACK(DataCallback);
            return (Altaircam_StartPushMode(_handle, _pDataCallback, GCHandle.ToIntPtr(_gchandle)) >= 0);
        }

        public bool StartPushModeV2(DelegateDataCallbackV2 ddelegate) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dDataCallbackV2 = ddelegate;
            _pDataCallbackV2 = new PALTAIRCAM_DATA_CALLBACK_V2(DataCallbackV2);
            return (Altaircam_StartPushModeV2(_handle, _pDataCallbackV2, GCHandle.ToIntPtr(_gchandle)) >= 0);
        }

        public bool Stop() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Stop(_handle) >= 0);
        }

        public bool Pause(bool bPause) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Pause(_handle, bPause ? 1 : 0) >= 0);
        }

        public bool Snap(uint nResolutionIndex) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Snap(_handle, nResolutionIndex) >= 0);
        }

        /*
            soft trigger:
            nNumber:    0xffff:     trigger continuously
                        0:          cancel trigger
                        others:     number of images to be triggered
        */

        public bool Trigger(ushort nNumber) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Trigger(_handle, nNumber) >= 0);
        }

        public bool put_Size(int nWidth, int nHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Size(_handle, nWidth, nHeight) >= 0);
        }

        public bool get_Size(out int nWidth, out int nHeight) {
            nWidth = nHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Size(_handle, out nWidth, out nHeight) >= 0);
        }

        /*
            put_Size, put_eSize, can be used to set the video output resolution BEFORE Start.
            put_Size use width and height parameters, put_eSize use the index parameter.
            for example, UCMOS03100KPA support the following resolutions:
                index 0:    2048,   1536
                index 1:    1024,   768
                index 2:    680,    510
            so, we can use put_Size(h, 1024, 768) or put_eSize(h, 1). Both have the same effect.
        */

        public bool put_eSize(uint nResolutionIndex) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_eSize(_handle, nResolutionIndex) >= 0);
        }

        public bool get_eSize(out uint nResolutionIndex) {
            nResolutionIndex = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_eSize(_handle, out nResolutionIndex) >= 0);
        }

        public bool get_Resolution(uint nResolutionIndex, out int pWidth, out int pHeight) {
            pWidth = pHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Resolution(_handle, nResolutionIndex, out pWidth, out pHeight) >= 0);
        }

        /*
            get the sensor pixel size, such as: 2.4um
        */

        public bool get_PixelSize(uint nResolutionIndex, out float x, out float y) {
            x = y = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_PixelSize(_handle, nResolutionIndex, out x, out y) >= 0);
        }

        public bool get_ResolutionRatio(uint nResolutionIndex, out int pNumerator, out int pDenominator) {
            pNumerator = pDenominator = 1;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ResolutionRatio(_handle, nResolutionIndex, out pNumerator, out pDenominator) >= 0);
        }

        public bool get_RawFormat(out uint nFourCC, out uint bitdepth) {
            nFourCC = bitdepth = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_RawFormat(_handle, out nFourCC, out bitdepth) >= 0);
        }

        public bool put_ProcessMode(ePROCESSMODE nProcessMode) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_ProcessMode(_handle, nProcessMode) >= 0);
        }

        public bool get_ProcessMode(out ePROCESSMODE pnProcessMode) {
            pnProcessMode = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ProcessMode(_handle, out pnProcessMode) >= 0);
        }

        public bool put_RealTime(bool bEnable) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_RealTime(_handle, bEnable ? 1 : 0) >= 0);
        }

        public bool get_RealTime(out bool bEnable) {
            bEnable = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iEnable = 0;
            if (Altaircam_get_RealTime(_handle, out iEnable) < 0)
                return false;

            bEnable = (iEnable != 0);
            return true;
        }

        public bool Flush() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Flush(_handle) >= 0);
        }

        public bool get_AutoExpoEnable(out bool bAutoExposure) {
            bAutoExposure = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iEnable = 0;
            if (Altaircam_get_AutoExpoEnable(_handle, out iEnable) < 0)
                return false;

            bAutoExposure = (iEnable != 0);
            return true;
        }

        public bool put_AutoExpoEnable(bool bAutoExposure) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_AutoExpoEnable(_handle, bAutoExposure ? 1 : 0) >= 0);
        }

        public bool get_AutoExpoTarget(out ushort Target) {
            Target = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_AutoExpoTarget(_handle, out Target) >= 0);
        }

        public bool put_AutoExpoTarget(ushort Target) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_AutoExpoTarget(_handle, Target) >= 0);
        }

        public bool put_MaxAutoExpoTimeAGain(uint maxTime, ushort maxAGain) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_MaxAutoExpoTimeAGain(_handle, maxTime, maxAGain) >= 0);
        }

        public bool get_ExpoTime(out uint Time)/* in microseconds */
        {
            Time = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ExpoTime(_handle, out Time) >= 0);
        }

        public bool put_ExpoTime(uint Time)/* in microseconds */
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_ExpoTime(_handle, Time) >= 0);
        }

        public bool get_ExpTimeRange(out uint nMin, out uint nMax, out uint nDef) {
            nMin = nMax = nDef = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ExpTimeRange(_handle, out nMin, out nMax, out nDef) >= 0);
        }

        public bool get_ExpoAGain(out ushort AGain)/* percent, such as 300 */
        {
            AGain = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ExpoAGain(_handle, out AGain) >= 0);
        }

        public bool put_ExpoAGain(ushort AGain)/* percent */
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_ExpoAGain(_handle, AGain) >= 0);
        }

        public bool get_ExpoAGainRange(out ushort nMin, out ushort nMax, out ushort nDef) {
            nMin = nMax = nDef = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ExpoAGainRange(_handle, out nMin, out nMax, out nDef) >= 0);
        }

        public bool put_LevelRange(ushort[] aLow, ushort[] aHigh) {
            if (aLow.Length != 4 || aHigh.Length != 4)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_LevelRange(_handle, aLow, aHigh) >= 0);
        }

        public bool get_LevelRange(ushort[] aLow, ushort[] aHigh) {
            if (aLow.Length != 4 || aHigh.Length != 4)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_LevelRange(_handle, aLow, aHigh) >= 0);
        }

        public bool put_Hue(int Hue) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Hue(_handle, Hue) >= 0);
        }

        public bool get_Hue(out int Hue) {
            Hue = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Hue(_handle, out Hue) >= 0);
        }

        public bool put_Saturation(int Saturation) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Saturation(_handle, Saturation) >= 0);
        }

        public bool get_Saturation(out int Saturation) {
            Saturation = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Saturation(_handle, out Saturation) >= 0);
        }

        public bool put_Brightness(int Brightness) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Brightness(_handle, Brightness) >= 0);
        }

        public bool get_Brightness(out int Brightness) {
            Brightness = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Brightness(_handle, out Brightness) >= 0);
        }

        public bool get_Contrast(out int Contrast) {
            Contrast = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Contrast(_handle, out Contrast) >= 0);
        }

        public bool put_Contrast(int Contrast) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Contrast(_handle, Contrast) >= 0);
        }

        public bool get_Gamma(out int Gamma) {
            Gamma = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Gamma(_handle, out Gamma) >= 0);
        }

        public bool put_Gamma(int Gamma) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Gamma(_handle, Gamma) >= 0);
        }

        public bool get_Chrome(out bool bChrome)    /* monochromatic mode */
        {
            bChrome = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iEnable = 0;
            if (Altaircam_get_Chrome(_handle, out iEnable) < 0)
                return false;

            bChrome = (iEnable != 0);
            return true;
        }

        public bool put_Chrome(bool bChrome) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Chrome(_handle, bChrome ? 1 : 0) >= 0);
        }

        public bool get_VFlip(out bool bVFlip) /* vertical flip */
        {
            bVFlip = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iVFlip = 0;
            if (Altaircam_get_VFlip(_handle, out iVFlip) < 0)
                return false;

            bVFlip = (iVFlip != 0);
            return true;
        }

        public bool put_VFlip(bool bVFlip) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_VFlip(_handle, bVFlip ? 1 : 0) >= 0);
        }

        public bool get_HFlip(out bool bHFlip) {
            bHFlip = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iHFlip = 0;
            if (Altaircam_get_HFlip(_handle, out iHFlip) < 0)
                return false;

            bHFlip = (iHFlip != 0);
            return true;
        }

        public bool put_HFlip(bool bHFlip)  /* horizontal flip */
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_HFlip(_handle, bHFlip ? 1 : 0) >= 0);
        }

        /* negative film */

        public bool get_Negative(out bool bNegative) {
            bNegative = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iNegative = 0;
            if (Altaircam_get_Negative(_handle, out iNegative) < 0)
                return false;

            bNegative = (iNegative != 0);
            return true;
        }

        /* negative film */

        public bool put_Negative(bool bNegative) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Negative(_handle, bNegative ? 1 : 0) >= 0);
        }

        public bool put_Speed(ushort nSpeed) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Speed(_handle, nSpeed) >= 0);
        }

        public bool get_Speed(out ushort pSpeed) {
            pSpeed = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Speed(_handle, out pSpeed) >= 0);
        }

        /* power supply:
                0 -> 60HZ AC
                1 -> 50Hz AC
                2 -> DC
        */

        public bool put_HZ(int nHZ) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_HZ(_handle, nHZ) >= 0);
        }

        public bool get_HZ(out int nHZ) {
            nHZ = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_HZ(_handle, out nHZ) >= 0);
        }

        public bool put_Mode(bool bSkip) /* skip or bin */
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Mode(_handle, bSkip ? 1 : 0) >= 0);
        }

        public bool get_Mode(out bool bSkip) {
            bSkip = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iSkip = 0;
            if (Altaircam_get_Mode(_handle, out iSkip) < 0)
                return false;

            bSkip = (iSkip != 0);
            return true;
        }

        /* White Balance, Temp/Tint mode */

        public bool put_TempTint(int nTemp, int nTint) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_TempTint(_handle, nTemp, nTint) >= 0);
        }

        /* White Balance, Temp/Tint mode */

        public bool get_TempTint(out int nTemp, out int nTint) {
            nTemp = nTint = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_TempTint(_handle, out nTemp, out nTint) >= 0);
        }

        /* White Balance, RGB Gain Mode */

        public bool put_WhiteBalanceGain(int[] aGain) {
            if (aGain.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_WhiteBalanceGain(_handle, aGain) >= 0);
        }

        /* White Balance, RGB Gain Mode */

        public bool get_WhiteBalanceGain(int[] aGain) {
            if (aGain.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_WhiteBalanceGain(_handle, aGain) >= 0);
        }

        public bool put_AWBAuxRect(int X, int Y, int Width, int Height) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            rc.left = X;
            rc.right = X + Width;
            rc.top = Y;
            rc.bottom = Y + Height;
            return (Altaircam_put_AWBAuxRect(_handle, ref rc) >= 0);
        }

        public bool get_AWBAuxRect(out int X, out int Y, out int Width, out int Height) {
            X = Y = Width = Height = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            if (Altaircam_get_AWBAuxRect(_handle, out rc) < 0)
                return false;

            X = rc.left;
            Y = rc.top;
            Width = rc.right - rc.left;
            Height = rc.bottom - rc.top;
            return true;
        }

        public bool put_AEAuxRect(int X, int Y, int Width, int Height) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            rc.left = X;
            rc.right = X + Width;
            rc.top = Y;
            rc.bottom = Y + Height;
            return (Altaircam_put_AEAuxRect(_handle, ref rc) >= 0);
        }

        public bool get_AEAuxRect(out int X, out int Y, out int Width, out int Height) {
            X = Y = Width = Height = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            if (Altaircam_get_AEAuxRect(_handle, out rc) < 0)
                return false;

            X = rc.left;
            Y = rc.top;
            Width = rc.right - rc.left;
            Height = rc.bottom - rc.top;
            return true;
        }

        public bool put_BlackBalance(ushort[] aSub) {
            if (aSub.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_BlackBalance(_handle, aSub) >= 0);
        }

        public bool get_BlackBalance(ushort[] aSub) {
            if (aSub.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_BlackBalance(_handle, aSub) >= 0);
        }

        public bool put_ABBAuxRect(int X, int Y, int Width, int Height) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            rc.left = X;
            rc.right = X + Width;
            rc.top = Y;
            rc.bottom = Y + Height;
            return (Altaircam_put_ABBAuxRect(_handle, ref rc) >= 0);
        }

        public bool get_ABBAuxRect(out int X, out int Y, out int Width, out int Height) {
            X = Y = Width = Height = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            if (Altaircam_get_ABBAuxRect(_handle, out rc) < 0)
                return false;

            X = rc.left;
            Y = rc.top;
            Width = rc.right - rc.left;
            Height = rc.bottom - rc.top;
            return true;
        }

        public bool get_StillResolution(uint nResolutionIndex, out int pWidth, out int pHeight) {
            pWidth = pHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_StillResolution(_handle, nResolutionIndex, out pWidth, out pHeight) >= 0);
        }

        public bool put_VignetEnable(bool bEnable) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_VignetEnable(_handle, bEnable ? 1 : 0) >= 0);
        }

        public bool get_VignetEnable(out bool bEnable) {
            bEnable = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iEanble = 0;
            if (Altaircam_get_VignetEnable(_handle, out iEanble) < 0)
                return false;

            bEnable = (iEanble != 0);
            return true;
        }

        public bool put_VignetAmountInt(int nAmount) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_VignetAmountInt(_handle, nAmount) >= 0);
        }

        public bool get_VignetAmountInt(out int nAmount) {
            nAmount = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_VignetAmountInt(_handle, out nAmount) >= 0);
        }

        public bool put_VignetMidPointInt(int nMidPoint) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_VignetMidPointInt(_handle, nMidPoint) >= 0);
        }

        public bool get_VignetMidPointInt(out int nMidPoint) {
            nMidPoint = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_VignetMidPointInt(_handle, out nMidPoint) >= 0);
        }

        /* led state:
            iLed: Led index, (0, 1, 2, ...)
            iState: 1 -> Ever bright; 2 -> Flashing; other -> Off
            iPeriod: Flashing Period (>= 500ms)
        */

        public bool put_LEDState(ushort iLed, ushort iState, ushort iPeriod) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_LEDState(_handle, iLed, iState, iPeriod) >= 0);
        }

        public int write_EEPROM(SafeCamHandle h, uint addr, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_write_EEPROM(_handle, addr, pBuffer, nBufferLen);
        }

        public int read_EEPROM(SafeCamHandle h, uint addr, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_read_EEPROM(_handle, addr, pBuffer, nBufferLen);
        }

        public int write_Pipe(SafeCamHandle h, uint pipeNum, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_write_Pipe(_handle, pipeNum, pBuffer, nBufferLen);
        }

        public int read_Pipe(SafeCamHandle h, uint pipeNum, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_read_Pipe(_handle, pipeNum, pBuffer, nBufferLen);
        }

        public int feed_Pipe(SafeCamHandle h, uint pipeNum) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_feed_Pipe(_handle, pipeNum);
        }

        public int write_UART(SafeCamHandle h, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_write_UART(_handle, pBuffer, nBufferLen);
        }

        public int read_UART(SafeCamHandle h, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_read_UART(_handle, pBuffer, nBufferLen);
        }

        public bool put_Option(eOPTION iOption, int iValue) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Option(_handle, iOption, iValue) >= 0);
        }

        public bool get_Option(eOPTION iOption, out int iValue) {
            iValue = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Option(_handle, iOption, out iValue) >= 0);
        }

        public bool put_Linear(byte[] v8, ushort[] v16) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Linear(_handle, v8, v16) >= 0);
        }

        public bool put_Curve(byte[] v8, ushort[] v16) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Curve(_handle, v8, v16) >= 0);
        }

        public bool put_ColorMatrix(double[] v) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_ColorMatrix(_handle, v) >= 0);
        }

        public bool put_InitWBGain(ushort[] v) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_InitWBGain(_handle, v) >= 0);
        }

        /* get the temperature of the sensor, in 0.1 degrees Celsius (32 means 3.2 degrees Celsius, -35 means -3.5 degree Celsius) */

        public bool get_Temperature(out short pTemperature) {
            pTemperature = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Temperature(_handle, out pTemperature) == 0);
        }

        /* set the target temperature of the sensor or TEC, in 0.1 degrees Celsius (32 means 3.2 degrees Celsius, -35 means -3.5 degree Celsius) */

        public bool put_Temperature(short nTemperature) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Temperature(_handle, nTemperature) == 0);
        }

        public bool put_Roi(uint xOffset, uint yOffset, uint xWidth, uint yHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Roi(_handle, xOffset, yOffset, xWidth, yHeight) >= 0);
        }

        public bool get_Roi(out uint pxOffset, out uint pyOffset, out uint pxWidth, out uint pyHeight) {
            pxOffset = pyOffset = pxWidth = pyHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Roi(_handle, out pxOffset, out pyOffset, out pxWidth, out pyHeight) >= 0);
        }

        /*
            get the frame rate: framerate (fps) = Frame * 1000.0 / nTime
        */

        public bool get_FrameRate(out uint nFrame, out uint nTime, out uint nTotalFrame) {
            nFrame = nTime = nTotalFrame = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_FrameRate(_handle, out nFrame, out nTime, out nTotalFrame) >= 0);
        }

        public bool LevelRangeAuto() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_LevelRangeAuto(_handle) >= 0);
        }

        public bool put_ExpoCallback(DelegateExposureCallback fnExpoProc) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dExposureCallback = fnExpoProc;
            if (fnExpoProc == null)
                return (Altaircam_put_ExpoCallback(_handle, null, IntPtr.Zero) >= 0);
            else {
                _pExposureCallback = new PIALTAIRCAM_EXPOSURE_CALLBACK(ExposureCallback);
                return (Altaircam_put_ExpoCallback(_handle, _pExposureCallback, GCHandle.ToIntPtr(_gchandle)) >= 0);
            }
        }

        public bool put_ChromeCallback(DelegateChromeCallback fnChromeProc) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dChromeCallback = fnChromeProc;
            if (fnChromeProc == null)
                return (Altaircam_put_ChromeCallback(_handle, null, IntPtr.Zero) >= 0);
            else {
                _pChromeCallback = new PIALTAIRCAM_CHROME_CALLBACK(ChromeCallback);
                return (Altaircam_put_ChromeCallback(_handle, _pChromeCallback, GCHandle.ToIntPtr(_gchandle)) >= 0);
            }
        }

        /* Auto White Balance, Temp/Tint Mode */

        public bool AwbOnePush(DelegateTempTintCallback fnTTProc) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dTempTintCallback = fnTTProc;
            if (fnTTProc == null)
                return (Altaircam_AwbOnePush(_handle, null, IntPtr.Zero) >= 0);
            else {
                _pTempTintCallback = new PIALTAIRCAM_TEMPTINT_CALLBACK(TempTintCallback);
                return (Altaircam_AwbOnePush(_handle, _pTempTintCallback, GCHandle.ToIntPtr(_gchandle)) >= 0);
            }
        }

        /* put_TempTintInit is obsolete, it's a synonyms for AwbOnePush. */

        public bool put_TempTintInit(DelegateTempTintCallback fnTTProc) {
            return AwbOnePush(fnTTProc);
        }

        /* Auto White Balance, RGB Gain Mode */

        public bool AwbInit(DelegateWhitebalanceCallback fnWBProc) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dWhitebalanceCallback = fnWBProc;
            if (fnWBProc == null)
                return (Altaircam_AwbOnePush(_handle, null, IntPtr.Zero) >= 0);
            else {
                _pWhitebalanceCallback = new PIALTAIRCAM_WHITEBALANCE_CALLBACK(WhitebalanceCallback);
                return (Altaircam_AwbInit(_handle, _pWhitebalanceCallback, GCHandle.ToIntPtr(_gchandle)) >= 0);
            }
        }

        public bool AbbOnePush(DelegateBlackbalanceCallback fnBBProc) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dBlackbalanceCallback = fnBBProc;
            if (fnBBProc == null)
                return (Altaircam_AbbOnePush(_handle, null, IntPtr.Zero) >= 0);
            else {
                _pBlackbalanceCallback = new PIALTAIRCAM_BLACKBALANCE_CALLBACK(BlackbalanceCallback);
                return (Altaircam_AbbOnePush(_handle, _pBlackbalanceCallback, GCHandle.ToIntPtr(_gchandle)) >= 0);
            }
        }

        public bool FfcOnePush() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            return (Altaircam_FfcOnePush(_handle) >= 0);
        }

        public bool DfcOnePush() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            return (Altaircam_DfcOnePush(_handle) >= 0);
        }

        public bool IoControl(uint index, eIoControType eType, int outVal, out int inVal) {
            inVal = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            return (Altaircam_IoControl(_handle, index, eType, outVal, out inVal) >= 0);
        }

        public bool GetHistogram(DelegateHistogramCallback fnHistogramProc) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dHistogramCallback = fnHistogramProc;
            _pHistogramCallback = new PIALTAIRCAM_HISTOGRAM_CALLBACK(HistogramCallback);
            return (Altaircam_GetHistogram(_handle, _pHistogramCallback, GCHandle.ToIntPtr(_gchandle)) >= 0);
        }

        /*
            calculate the clarity factor:
            pImageData: pointer to the image data
            bits: 8(Grey), 24 (RGB24), 32(RGB32)
            nImgWidth, nImgHeight: the image width and height
        */

        public static double calcClarityFactor(IntPtr pImageData, int bits, uint nImgWidth, uint nImgHeight) {
            return Altaircam_calc_ClarityFactor(pImageData, bits, nImgWidth, nImgHeight);
        }
    }
}