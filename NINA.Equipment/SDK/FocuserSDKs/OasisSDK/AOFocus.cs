using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Astroasis.AstroasisSDK {
    public class AOFocus {
        private const string DLLNAME = "OasisFocuser.dll";

        static AOFocus() {
            DllLoader.LoadDll(Path.Combine("Oasis", DLLNAME));
        }

        public const int AO_FOCUSER_MAX_NUM = 32;        /* Maximum number of focusers which can be connected to the system */
        public const int AO_FOCUSER_VERSION_LEN = 32;    /* Length of version string */
        public const int AO_FOCUSER_NAME_LEN = 32;       /* Length of name string */

        public const int AO_LOG_LEVEL_QUIET = 0;    /* No log */
        public const int AO_LOG_LEVEL_ERROR = 1;    /* Error log */
        public const int AO_LOG_LEVEL_INFO = 2;     /* Info and error log */
        public const int AO_LOG_LEVEL_DEBUG = 3;    /* Debug, info and error log */

        public const int TEMPERATURE_INVALID = unchecked((int)0x80000000);  /* Invalid temperature value */

        public enum AOReturn : int {
            AO_SUCCESS = 0,                     /* Success */
            AO_ERROR_INVALID_ID,                /* Device ID is invalid */
            AO_ERROR_INVALID_PARAMETER,         /* One or more parameters are invalid */
            AO_ERROR_INVALID_STATE,             /* Device is not in correct state for specific API call */
            AO_ERROR_BUFFER_TOO_SMALL,          /* Size of buffer is too small */
            AO_ERROR_COMMUNICATION,             /* Data communication error such as device has been removed from USB port */
            AO_ERROR_TIMEOUT,                   /* Timeout occured */
            AO_ERROR_BUSY,                      /* Device is being used by another application */
            AO_ERROR_NULL_POINTER,              /* Caller passes null-pointer parameter which is not expected */
            AO_ERROR_OUT_OF_RESOURCE,           /* Out of resouce such as lack of memory */
            AO_ERROR_NOT_IMPLEMENTED,           /* The interface is not currently supported */
            AO_ERROR_FAULT,                     /* Significant fault which means the device may not work correctly and hard to recovery it */
            AO_ERROR_INVALID_SIZE,              /* Size is invalid */
            AO_ERROR_INVALID_VERSION,           /* Version is invalid */
            AO_ERROR_UNKNOWN = 0x40,            /* Any other errors */
        }

        public enum AOConfig : uint {
            MASK_MAX_STEP = 0x00000001,             /* Used to set maxStep field */
            MASK_BACKLASH = 0x00000002,             /* Used to set backlash field */
            MASK_BACKLASH_DIRECTION = 0x00000004,   /* Used to set backlashDirection field */
            MASK_REVERSE_DIRECTION = 0x00000008,    /* Used to set reverseDirection field */
            MASK_SPEED = 0x00000010,                /* Used to set speed field */
            MASK_BEEP_ON_MOVE = 0x00000020,         /* Used to set beepOnMove field */
            MASK_BEEP_ON_STARTUP = 0x00000040,      /* Used to set beepOnStartup field */
            MASK_BLUETOOTH = 0x00000080,            /* Used to set bluetoothOn field */
            MASK_STALL_DETECTION = 0x00000100,      /* Used to set stallDetection field */
            MASK_HEATING_TEMPERATURE = 0x00000200,  /* Used to set heatingTemperature field */
            MASK_HEATING_ON = 0x00000400,           /* Used to set heatingOn field */
            MASK_USB_POWER_CAPACITY = 0x00000800,   /* Used to set usbPowerCapacity field */
            MASK_ALL = 0xFFFFFFFF                   /* Used to set all fields */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AOFocuserVersion {
            public uint protocal;          /* Version of protocal over USB and Bluetooth communication */
            public uint hardware;          /* Focuser hardware version */
            public uint firmware;          /* Focuser firmware version */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public string built;           /* Null-terminated string which indicates firmware building time */
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct AOFocuserConfig {
            public uint mask;                      /* Used by AOFocuserSetConfig() to indicates which field wants to be set */
            public int maxStep;                    /* Maximum step or position */
            public int backlash;                   /* Backlash value */
            public int backlashDirection;          /* Backlash direction. 0 - IN, others - OUT */
            public int reverseDirection;           /* 0 - Not reverse motor moving direction, others - Reverse motor moving direction */
            public int speed;                      /* Reserved for motor speed setting. Should always be zero for now */
            public int beepOnMove;                 /* 0 - Turn off beep for move, others - Turn on beep for move */
            public int beepOnStartup;              /* 0 - Turn off beep for device startup, others - Turn on beep for device startup */
            public int bluetoothOn;                /* 0 - Turn off Bluetooth, others - Turn on Bluetooth */
            public int stallDetection;             /* 0 - Turn off motor stall detection, others - Turn on motor stall detection */
            public int heatingTemperature;         /* Target heating temperature in 0.01 degree unit */
            public int heatingOn;                  /* 0 - Turn off heating, others - Turn on heating */
            public int usbPowerCapacity;           /* 0 - 500mA, 1 - 1000mA or higher */
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct AOFocuserStatus {
            public int temperatureInt;             /* Internal (on board) temperature in 0.01 degree unit */
            public int temperatureExt;             /* External (ambient) temperature in 0.01 degree unit */
            public int temperatureDetection;       /* 0 - ambient temperature probe is not inserted, others - ambient temperature probe is inserted */
            public int position;                   /* Current motor position */
            public int moving;                     /* 0 - motor is not moving, others - Motor is moving */
            public int stallDetection;             /* 0 - motor stall is not detected, others - motor stall detected */
            public int heatingOn;                  /* 0 - heating is disabled, others - heating is enabled */
            public int heatingPower;               /* Current heating power in 1% unit */
            public int dcPower;                    /* Current DC power in 0.1V unit */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public int[] reserved;
        };

        [SecurityCritical]
        public static AOReturn FocuserScan(out int number, [Out] int[] ids) {
            return AOFocuserScan(out number, ids);
        }

        [SecurityCritical]
        public static AOReturn FocuserOpen(int id) {
            return AOFocuserOpen(id);
        }

        [SecurityCritical]
        public static AOReturn FocuserClose(int id) {
            return AOFocuserClose(id);
        }

        [SecurityCritical]
        public static AOReturn FocuserGetProductModel(int id, out string model) {
            StringBuilder buf = new StringBuilder(AO_FOCUSER_NAME_LEN);
            var err = AOFocuserGetProductModel(id, buf);
            model = buf.ToString();
            return err;
        }

        [SecurityCritical]
        public static AOReturn FocuserGetVersion(int id, out AOFocuserVersion version) {
            return AOFocuserGetVersion(id, out version);
        }

        [SecurityCritical]
        public static AOReturn FocuserGetSerialNumber(int id, out string sn) {
            StringBuilder buf = new StringBuilder(AO_FOCUSER_VERSION_LEN);
            var err = AOFocuserGetSerialNumber(id, buf);
            sn = buf.ToString();
            return err;
        }

        [SecurityCritical]
        public static AOReturn FocuserGetFriendlyName(int id, out string name) {
            StringBuilder buf = new StringBuilder(AO_FOCUSER_NAME_LEN);
            var err = AOFocuserGetFriendlyName(id, buf);
            name = buf.ToString();
            return err;
        }

        [SecurityCritical]
        public static AOReturn FocuserSetFriendlyName(int id, string name) {
            return AOFocuserSetFriendlyName(id, name);
        }

        [SecurityCritical]
        public static AOReturn FocuserGetConfig(int id, out AOFocuserConfig config) {
            return AOFocuserGetConfig(id, out config);
        }

        [SecurityCritical]
        public static AOReturn FocuserSetConfig(int id, ref AOFocuserConfig config) {
            return AOFocuserSetConfig(id, ref config);
        }

        [SecurityCritical]
        public static AOReturn FocuserGetStatus(int id, out AOFocuserStatus status) {
            return AOFocuserGetStatus(id, out status);
        }

        [SecurityCritical]
        public static AOReturn FocuserFactoryReset(int id) {
            return AOFocuserFactoryReset(id);
        }

        [SecurityCritical]
        public static AOReturn FocuserSetZeroPosition(int id) {
            return AOFocuserSetZeroPosition(id);
        }

        [SecurityCritical]
        public static AOReturn FocuserSyncPosition(int id, int position) {
            return AOFocuserSyncPosition(id, position);
        }

        [SecurityCritical]
        public static AOReturn FocuserMove(int id, int step) {
            return AOFocuserMove(id, step);
        }

        [SecurityCritical]
        public static AOReturn FocuserMoveTo(int id, int position) {
            return AOFocuserMoveTo(id, position);
        }

        [SecurityCritical]
        public static AOReturn FocuserStopMove(int id) {
            return AOFocuserStopMove(id);
        }

        [SecurityCritical]
        public static AOReturn FocuserClearStall(int id) {
            return AOFocuserClearStall(id);
        }

        [SecurityCritical]
        public static AOReturn FocuserGetSDKVersion(out string version) {
            StringBuilder buf = new StringBuilder(AO_FOCUSER_VERSION_LEN);
            var err = AOFocuserGetSDKVersion(buf);
            version = buf.ToString();
            return err;
        }

        [SecurityCritical]
        public static AOReturn FocuserSetLogLevel(int level) {
            return AOFocuserSetLogLevel(level);
        }

        [DllImport(DLLNAME, EntryPoint = "AOFocuserScan", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserScan(out int number, [Out] int[] ids);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserOpen", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserOpen(int id);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserClose", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserClose(int id);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserGetProductModel", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserGetProductModel(int id, StringBuilder model);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserGetVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserGetVersion(int id, out AOFocuserVersion version);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserGetSerialNumber", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserGetSerialNumber(int id, StringBuilder sn);

        // AOReturn AOFocuserGetFriendlyName(int id, char* name);
        [DllImport(DLLNAME, EntryPoint = "AOFocuserGetFriendlyName", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserGetFriendlyName(int id, StringBuilder name);

        // AOReturn AOFocuserSetFriendlyName(int id, const char* name);
        [DllImport(DLLNAME, EntryPoint = "AOFocuserSetFriendlyName", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserSetFriendlyName(int id, string name);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserGetConfig", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserGetConfig(int id, out AOFocuserConfig config);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserSetConfig", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserSetConfig(int id, ref AOFocuserConfig config);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserGetStatus", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserGetStatus(int id, out AOFocuserStatus status);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserFactoryReset", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserFactoryReset(int id);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserSetZeroPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserSetZeroPosition(int id);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserSyncPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserSyncPosition(int id, int position);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserMove", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserMove(int id, int step);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserMoveTo", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserMoveTo(int id, int position);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserStopMove", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserStopMove(int id);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserClearStall", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserClearStall(int id);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserGetSDKVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserGetSDKVersion(StringBuilder version);

        [DllImport(DLLNAME, EntryPoint = "AOFocuserSetLogLevel", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn AOFocuserSetLogLevel(int level);
    }
}
