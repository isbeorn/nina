using Accessibility;
using NINA.Core.Utility;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace ZWOptical.EFWSDK {

    public class EFWdll {
        private const string DLLNAME = "EFW_filter.dll";

        static EFWdll() {
            DllLoader.LoadDll(Path.Combine("ASI", DLLNAME));
        }

        public enum EFW_ERROR_CODE {
            EFW_SUCCESS = 0,
            EFW_ERROR_INVALID_INDEX,
            EFW_ERROR_INVALID_ID,
            EFW_ERROR_INVALID_VALUE,
            EFW_ERROR_REMOVED,
            EFW_ERROR_MOVING,
            EFW_ERROR_ERROR_STATE,
            EFW_ERROR_GENERAL_ERROR,//other error
            EFW_ERROR_NOT_SUPPORTED,
            EFW_ERROR_CLOSED,
            EFW_ERROR_END = -1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EFW_INFO {
            public int ID;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            public byte[] name;

            public int slotNum;

            public string Name {
                get { return Encoding.ASCII.GetString(name).TrimEnd((char)0); }
            }
        };

        public struct EFW_ID {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
            public byte[] id;

            public string ID {
                get {
                    string idAscii = Encoding.ASCII.GetString(id);
                    char[] trimChars = new char[1];
                    return idAscii.TrimEnd(trimChars);
                }
            }
        }

        [DllImport(DLLNAME, EntryPoint = "EFWGetNum", CallingConvention = CallingConvention.Cdecl)]
        private static extern int EFWGetNum();

        [DllImport(DLLNAME, EntryPoint = "EFWGetID", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWGetID(int index, out int ID);

        [DllImport(DLLNAME, EntryPoint = "EFWGetProperty", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWGetProperty(int ID, out EFW_INFO pInfo);

        [DllImport(DLLNAME, EntryPoint = "EFWOpen", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWOpen(int index);

        [DllImport(DLLNAME, EntryPoint = "EFWClose", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWClose(int ID);

        [DllImport(DLLNAME, EntryPoint = "EFWGetPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWGetPosition(int ID, out int pPosition);

        [DllImport(DLLNAME, EntryPoint = "EFWSetPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWSetPosition(int ID, int Position);

        [DllImport(DLLNAME, EntryPoint = "EFWSetDirection", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWSetDirection(int ID, [MarshalAs(UnmanagedType.I1)] bool bUnidirectional);

        [DllImport(DLLNAME, EntryPoint = "EFWGetDirection", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWGetDirection(int ID, [MarshalAs(UnmanagedType.I1)] out bool bUnidirectional);

        [DllImport(DLLNAME, EntryPoint = "EFWSetID", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWSetID(int ID, EFW_ID alias);

        [DllImport(DLLNAME, EntryPoint = "EFWCalibrate", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWCalibrate(int ID);

        [DllImport(DLLNAME, EntryPoint = "EFWGetFirmwareVersion", CallingConvention = CallingConvention.StdCall)]
        private static extern EFW_ERROR_CODE EFWGetFirmwareVersion(int ID, out byte pbMajor, out byte pbMinor, out byte pbBuild);

        [DllImport(DLLNAME, EntryPoint = "EFWGetSDKVersion", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr EFWGetSDKVersion();


        [SecurityCritical]
        public static int GetNum() {
            return EFWGetNum();
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE GetID(int index, out int ID) {
            return EFWGetID(index, out ID);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE SetID(int ID, string id) {
            EFW_ID asiId = default;
            asiId.id = new byte[8];

            byte[] bytes = Encoding.Default.GetBytes(id);
            bytes.CopyTo(asiId.id, 0);

            return EFWSetID(ID, asiId);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE GetProperty(int ID, out EFW_INFO pInfo) {
            return EFWGetProperty(ID, out pInfo);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE Open(int index) {
            return EFWOpen(index);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE Close(int ID) {
            return EFWClose(ID);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE GetPosition(int ID, out int pPosition) {
            return EFWGetPosition(ID, out pPosition);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE SetPosition(int ID, int Position) {
            return EFWSetPosition(ID, Position);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE GetDirection(int ID, out bool bUnidirectional) {
            return EFWGetDirection(ID, out bUnidirectional);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE SetDirection(int ID, bool bUnidirectional) {
            return EFWSetDirection(ID, bUnidirectional);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE Calibrate(int ID) {
            return EFWCalibrate(ID);
        }

        [SecurityCritical]
        public static EFW_ERROR_CODE GetFirmwareVersion(int ID, out byte bMajor, out byte bMinor, out byte bBuild) {
            return EFWGetFirmwareVersion(ID, out bMajor, out bMinor, out bBuild);
        }

        [SecurityCritical]
        public static string GetSDKVersion() {
            return Marshal.PtrToStringAnsi(EFWGetSDKVersion());
        }
    }
}