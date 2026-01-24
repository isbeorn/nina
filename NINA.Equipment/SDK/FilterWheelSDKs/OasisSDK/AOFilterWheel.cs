using NINA.Core.Utility;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Astroasis.AstroasisSDK {
    public class AOFilterWheel {
        private const string DLLNAME = "OasisFilterWheel.dll";

        static AOFilterWheel() {
            DllLoader.LoadDll(Path.Combine("Oasis", DLLNAME));
        }

        public const int OFW_MAX_NUM = 32;		    /* Maximum filter wheel numbers supported by this SDK */
        public const int OFW_VERSION_LEN = 32;      /* Buffer length for version strings */
        public const int OFW_NAME_LEN = 32;         /* Buffer length for name strings */
        public const int OFW_SLOT_NAME_LEN = 16;	/* Buffer length for slot name strings */

        public const int AO_LOG_LEVEL_QUIET = 0;    /* No log */
        public const int AO_LOG_LEVEL_ERROR = 1;    /* Error log */
        public const int AO_LOG_LEVEL_INFO = 2;     /* Info and error log */
        public const int AO_LOG_LEVEL_DEBUG = 3;    /* Debug, info and error log */

        public enum AOReturn : int {
            AO_SUCCESS = 0,                 /* Success */
            AO_ERROR_INVALID_ID,            /* Device ID is invalid */
            AO_ERROR_INVALID_PARAMETER,     /* One or more parameters are invalid */
            AO_ERROR_INVALID_STATE,         /* Device is not in correct state for specific API call */
            AO_ERROR_BUFFER_TOO_SMALL,      /* Size of buffer is too small */
            AO_ERROR_COMMUNICATION,         /* Data communication error such as device has been removed from USB port */
            AO_ERROR_TIMEOUT,               /* Timeout occured */
            AO_ERROR_BUSY,                  /* Device is being used by another application */
            AO_ERROR_NULL_POINTER,          /* Caller passes null-pointer parameter which is not expected */
            AO_ERROR_OUT_OF_RESOURCE,       /* Out of resouce such as lack of memory */
            AO_ERROR_NOT_IMPLEMENTED,       /* The interface is not currently supported */
            AO_ERROR_FAULT,                 /* Significant fault which means the device may not work correctly and hard to recovery it */
            AO_ERROR_INVALID_SIZE,          /* Size is invalid */
            AO_ERROR_INVALID_VERSION,       /* Version is invalid */
            AO_ERROR_UNKNOWN = 0x40,        /* Any other errors */
        }

        public enum AOConfig : uint {
            MASK_SPEED = 0x00000001,        /* Used to set speed field */
            MASK_AUTORUN = 0x00000002,      /* Used to set autorun field */
            MASK_BLUETOOTH = 0x00000004,    /* Used to set bluetoothOn field */
            MASK_TURBO = 0x00000008,        /* Used to set turbo field */
            MASK_ALL = 0xFFFFFFFF           /* Used to set all fields */
        }

        public enum AOStatus : int {
            STATUS_IDLE = 0,
            STATUS_MOVING = 1,
            STATUS_CALIBRATING = 2,
            STATUS_BENCHMARKING = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OFWVersion {
            public uint protocal;   /* Version of protocal over USB and Bluetooth communication */
            public uint hardware;   /* Device hardware version */
            public uint firmware;   /* Device firmware version */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public string built;    /* Null-terminated string which indicates firmware building time */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OFWConfig {
            public uint mask;           /* Used by OFWSetConfig() to indicate which field wants to be set */
            public int speed;           /* Motor speed. 0 - Fast, 1 - Normal, 2 - Slow */
            public int autorun;         /* Automatic switch to the target slot when power on. 0 - Do not switch, 1 - Auto switch  */
            public int bluetoothOn;     /* 0 - Turn off Bluetooth, others - Turn on Bluetooth */
            public int turbo;           /* 0 - Turn off turbo mode, others - Turn on turbo mode */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OFWStatus {
            public int temperature;         /* Internal (on board) temperature in 0.01 degree unit */
            public AOStatus filterStatus;   /* Current motor position */
            public int filterPosition;      /* Current motor position, zero - unknown position */
            public int seq;                 /* Sequence number for debug purpose */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OFWCalibrateData {
            public int index;           /* Index of the calibration data */
            public int active;          /* 0 - Non-active calibration data, 1 - Active calibration data */
            public int temperature;     /* Calibration temperature */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] low;           /* Calibration low value */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] high;          /* Calibration high value */
        }

        [SecurityCritical]
        public static AOReturn FilterWheelScan(out int number, [Out] int[] ids) {
            return OFWScan(out number, ids);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelOpen(int id) {
            return OFWOpen(id);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelClose(int id) {
            return OFWClose(id);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetProductModel(int id, out string model) {
            StringBuilder buf = new StringBuilder(OFW_NAME_LEN);
            var err = OFWGetProductModel(id, buf);
            model = buf.ToString();
            return err;
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetVersion(int id, out OFWVersion version) {
            return OFWGetVersion(id, out version);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetFriendlyName(int id, out string name) {
            StringBuilder buf = new StringBuilder(OFW_NAME_LEN);
            var err = OFWGetFriendlyName(id, buf);
            name = buf.ToString();
            return err;
        }

        [SecurityCritical]
        public static AOReturn FilterWheelSetFriendlyName(int id, string name) {
            return OFWSetFriendlyName(id, name);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetConfig(int id, out OFWConfig config) {
            return OFWGetConfig(id, out config);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelSetConfig(int id, ref OFWConfig config) {
            return OFWSetConfig(id, ref config);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetStatus(int id, out OFWStatus status) {
            return OFWGetStatus(id, out status);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetSlotNum(int id, out int num) {
            return OFWGetSlotNum(id, out num);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetSlotName(int id, int slot, out string name) {
            StringBuilder buf = new StringBuilder(OFW_SLOT_NAME_LEN);
            var err = OFWGetSlotName(id, slot, buf);
            name = buf.ToString();
            return err;
        }

        [SecurityCritical]
        public static AOReturn FilterWheelSetSlotName(int id, int slot, string name) {
            return OFWSetSlotName(id, slot, name);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetFocusOffset(int id, int num, out int[] offset) {
            offset = null;
            IntPtr buffer = Marshal.AllocHGlobal(num * sizeof(int));
            try {
                var err = OFWGetFocusOffset(id, num, buffer);
                if (err == AOReturn.AO_SUCCESS) {
                    offset = new int[num];
                    Marshal.Copy(buffer, offset, 0, num);
                }
                return err;
            } finally {
                Marshal.FreeHGlobal(buffer);

            }
        }

        [SecurityCritical]
        public static AOReturn FilterWheelSetFocusOffset(int id, int num, int[] offset) {
            IntPtr buffer = Marshal.AllocHGlobal(num * sizeof(int));
            try {
                Marshal.Copy(offset, 0, buffer, num);
                return OFWSetFocusOffset(id, num, buffer);
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [SecurityCritical]
        public static AOReturn FilterWheelSetPosition(int id, int position) {
            return OFWSetPosition(id, position);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelCalibrate(int id, int mode) {
            return OFWCalibrate(id, mode);
        }

        [SecurityCritical]
        public static AOReturn FilterWheelGetSDKVersion(out string version) {
            StringBuilder buf = new StringBuilder(OFW_VERSION_LEN);
            var err = OFWGetSDKVersion(buf);
            version = buf.ToString();
            return err;
        }

        [DllImport(DLLNAME, EntryPoint = "OFWScan", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWScan(out int number, [Out] int[] ids);

        [DllImport(DLLNAME, EntryPoint = "OFWOpen", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWOpen(int id);

        [DllImport(DLLNAME, EntryPoint = "OFWClose", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWClose(int id);

        [DllImport(DLLNAME, EntryPoint = "OFWGetProductModel", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetProductModel(int id, StringBuilder model);

        [DllImport(DLLNAME, EntryPoint = "OFWGetVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetVersion(int id, out OFWVersion version);

        [DllImport(DLLNAME, EntryPoint = "OFWGetFriendlyName", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetFriendlyName(int id, StringBuilder name);

        [DllImport(DLLNAME, EntryPoint = "OFWSetFriendlyName", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWSetFriendlyName(int id, string name);

        [DllImport(DLLNAME, EntryPoint = "OFWGetConfig", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetConfig(int id, out OFWConfig config);

        [DllImport(DLLNAME, EntryPoint = "OFWSetConfig", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWSetConfig(int id, ref OFWConfig config);

        [DllImport(DLLNAME, EntryPoint = "OFWGetStatus", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetStatus(int id, out OFWStatus status);

        [DllImport(DLLNAME, EntryPoint = "OFWGetSlotNum", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetSlotNum(int id, out int num);

        [DllImport(DLLNAME, EntryPoint = "OFWGetSlotName", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetSlotName(int id, int slot, StringBuilder name);

        [DllImport(DLLNAME, EntryPoint = "OFWSetSlotName", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWSetSlotName(int id, int slot, string name);

        [DllImport(DLLNAME, EntryPoint = "OFWGetFocusOffset", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetFocusOffset(int id, int num, IntPtr offset);

        [DllImport(DLLNAME, EntryPoint = "OFWSetFocusOffset", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWSetFocusOffset(int id, int num, IntPtr offset);

        [DllImport(DLLNAME, EntryPoint = "OFWSetPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWSetPosition(int id, int position);

        [DllImport(DLLNAME, EntryPoint = "OFWCalibrate", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWCalibrate(int id, int mode);

        [DllImport(DLLNAME, EntryPoint = "OFWGetSDKVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern AOReturn OFWGetSDKVersion(StringBuilder version);
    }
}
