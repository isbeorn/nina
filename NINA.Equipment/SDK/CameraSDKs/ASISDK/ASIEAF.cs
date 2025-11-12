using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace ZWOptical.ASISDK {
    public class ASIEAF {
        private const string DLLNAME = "EAF_focuser.dll";

        static ASIEAF() {
            DllLoader.LoadDll(Path.Combine("ASI", DLLNAME));
        }
        public enum EAF_ERROR_CODE {
            EAF_SUCCESS = 0,
            EAF_ERROR_INVALID_INDEX,
            EAF_ERROR_INVALID_ID,
            EAF_ERROR_INVALID_VALUE,
            EAF_ERROR_REMOVED, //failed to find the focuser, maybe the focuser has been removed
            EAF_ERROR_MOVING,//focuser is moving
            EAF_ERROR_ERROR_STATE,//focuser is in error state
            EAF_ERROR_GENERAL_ERROR,//other error
            EAF_ERROR_NOT_SUPPORTED,
            EAF_ERROR_CLOSED,
            EAF_ERROR_END = -1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EAF_INFO {
            public int ID;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            public byte[] name;

            public int MaxStep;

            public string Name {
                get { return Encoding.ASCII.GetString(name).TrimEnd((char)0); }
            }
        }; 
        
        [SecurityCritical]
        public static int GetNum() {
            return EAFGetNum();
        }
        [SecurityCritical]
        public static EAF_ERROR_CODE GetID(int index, out int ID) {
            return EAFGetID(index, out ID);
        }


        [SecurityCritical]
        public static EAF_ERROR_CODE Open(int id) {
            return EAFOpen(id);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE Close(int id) {
            return EAFClose(id);
        }
        [SecurityCritical]
        public static EAF_ERROR_CODE GetProperty(int id, out EAF_INFO info) {
            return EAFGetProperty(id, out info);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE Move(int id, int step) {
            return EAFMove(id, step);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE Stop(int id) {
            return EAFStop(id);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE IsMoving(int id, out bool isMoving) {
            var code = EAFIsMoving(id, out var standardMove, out var handControllerMove);
            isMoving = standardMove || handControllerMove;
            return code;
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE GetPosition(int id, out int step) {
            return EAFGetPosition(id, out step);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE GetTemperature(int id, out float temp) {
            return EAFGetTemp(id, out temp);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE GetMaxStep(int id, out int step) {
            return EAFGetMaxStep(id, out step);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE GetStepRange(int id, out int range) {
            return EAFStepRange(id, out range);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE GetFirmwareVersion(int ID, out byte bMajor, out byte bMinor, out byte bBuild) {
            return EAFGetFirmwareVersion(ID, out bMajor, out bMinor, out bBuild);
        }

        [SecurityCritical]
        public static string GetSDKVersion() {
            return Marshal.PtrToStringAnsi(EAFGetSDKVersion());
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE GetReverse(int id, out bool isReversed) {
            return EAFGetReverse(id, out isReversed);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE SetReverse(int id, bool isReversed) {
            return EAFSetReverse(id, isReversed);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE SetMaxStep(int id, int maxStep) {
            return EAFSetMaxStep(id, maxStep);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE SetZeroPosition(int id, int zeroPosition) {
            return EAFResetPostion(id, zeroPosition);
        }

        [SecurityCritical]
        public static EAF_ERROR_CODE SetID(int ID, string id) {
            EAF_ID asiId = default;
            asiId.id = new byte[8];

            byte[] bytes = Encoding.Default.GetBytes(id);
            bytes.CopyTo(asiId.id, 0);

            return EAFSetID(ID, asiId);
        }

        public struct EAF_ID {
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

        [DllImport(DLLNAME, EntryPoint = "EAFGetNum", CallingConvention = CallingConvention.Cdecl)]
        private static extern int EAFGetNum();

        [DllImport(DLLNAME, EntryPoint = "EAFGetID", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFGetID(int index, out int ID);

        [DllImport(DLLNAME, EntryPoint = "EAFOpen", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFOpen(int ID);

        [DllImport(DLLNAME, EntryPoint = "EAFGetProperty", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFGetProperty(int ID, out EAF_INFO pInfo);

        [DllImport(DLLNAME, EntryPoint = "EAFMove", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFMove(int ID, int iStep);

        [DllImport(DLLNAME, EntryPoint = "EAFStop", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFStop(int ID);

        [DllImport(DLLNAME, EntryPoint = "EAFIsMoving", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFIsMoving(int ID, out bool pbVal, out bool pbHandControl);

        [DllImport(DLLNAME, EntryPoint = "EAFGetPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFGetPosition(int ID, out int piStep);

        [DllImport(DLLNAME, EntryPoint = "EAFGetTemp", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFGetTemp(int ID, out float pfTemp);

        [DllImport(DLLNAME, EntryPoint = "EAFGetMaxStep", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFGetMaxStep(int ID, out int piVal);

        [DllImport(DLLNAME, EntryPoint = "EAFStepRange", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFStepRange(int ID, out int piVal);

        [DllImport(DLLNAME, EntryPoint = "EAFSetID", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFSetID(int ID, EAF_ID alias);

        [DllImport(DLLNAME, EntryPoint = "EAFClose", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFClose(int ID);

        [DllImport(DLLNAME, EntryPoint = "EAFGetFirmwareVersion", CallingConvention = CallingConvention.StdCall)]
        private static extern EAF_ERROR_CODE EAFGetFirmwareVersion(int ID, out byte major, out byte minor, out byte build);

        [DllImport(DLLNAME, EntryPoint = "EAFGetSDKVersion", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr EAFGetSDKVersion();

        [DllImport(DLLNAME, EntryPoint = "EAFGetReverse", CallingConvention = CallingConvention.StdCall)]
        private static extern EAF_ERROR_CODE EAFGetReverse(int ID, out bool pbVal);

        [DllImport(DLLNAME, EntryPoint = "EAFSetReverse", CallingConvention = CallingConvention.StdCall)]
        private static extern EAF_ERROR_CODE EAFSetReverse(int ID, bool pbVal);

        [DllImport(DLLNAME, EntryPoint = "EAFSetBacklash", CallingConvention = CallingConvention.StdCall)]
        private static extern EAF_ERROR_CODE EAFSetBacklash(int ID, int iVal);

        [DllImport(DLLNAME, EntryPoint = "EAFSetMaxStep", CallingConvention = CallingConvention.StdCall)]
        private static extern EAF_ERROR_CODE EAFSetMaxStep(int ID, int iVal);

        [DllImport(DLLNAME, EntryPoint = "EAFResetPostion", CallingConvention = CallingConvention.StdCall)]
        private static extern EAF_ERROR_CODE EAFResetPostion(int ID, int pos);

    }
}
