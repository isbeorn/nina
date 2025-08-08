using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK {
    public class PlayerOneFilterWheelSDK {
        private const string DLLNAME = "PlayerOnePW.dll";
        static PlayerOneFilterWheelSDK() {
            DllLoader.LoadDll(Path.Combine("PlayerOne", DLLNAME));

        }

        public enum PWErrors              // Return Error Code Definition
            {
            PW_OK = 0,                     // operation successful
            PW_ERROR_INVALID_INDEX,        // invalid index, means the index is < 0 or >= the count
            PW_ERROR_INVALID_HANDLE,       // invalid PW handle
            PW_ERROR_INVALID_ARGU,         // invalid argument(parameter)
            PW_ERROR_NOT_OPENED,           // PW not opened
            PW_ERROR_NOT_FOUND,            // PW not found, may be removed
            PW_ERROR_IS_MOVING,            // PW is moving
            PW_ERROR_POINTER,              // invalid pointer, when get some value, do not pass the NULL pointer to the function
            PW_ERROR_OPERATION_FAILED,     // operation failed (Usually, this is caused by sending commands too often)
            PW_ERROR_FIRMWARE_ERROR,       // firmware error (If you encounter this error, try calling POAResetPW)
        }

        public enum PWState               // PW State Definition
        {
            PW_STATE_CLOSED = 0,           // PW was closed
            PW_STATE_OPENED,               // PW was opened, but not moving(Idle)
            PW_STATE_MOVING                // PW is moving
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PWProperties              // PW Properties Definition
        {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            public byte[] PWName;                      // the PW name
            public int Handle;                         // it's unique,PW can be controlled and set by the handle
            public int PositionCount;                  // the filter capacity, eg: 5-position

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 32)]
            public byte[] serialNumber;                        // the serial number of PW,it's unique

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 32)]
            public byte[] Reserved;                  // reserved

            public string Name {
                get { return Encoding.ASCII.GetString(PWName).TrimEnd((Char)0); }
            }

            public string SN {
                get { return Encoding.ASCII.GetString(serialNumber).TrimEnd((Char)0); }
            }
        }

        public const int MAX_FILTER_NAME_LEN = 24; // custom name and filter alias max length


        [DllImport(DLLNAME, EntryPoint = "POAGetPWCount", CallingConvention = CallingConvention.Cdecl)]
        private static extern int POAGetPWCount64();

        [DllImport(DLLNAME, EntryPoint = "POAGetPWProperties", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetPWProperties64(int index, out PWProperties pProp);

        [DllImport(DLLNAME, EntryPoint = "POAGetPWPropertiesByHandle", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetPWPropertiesByHandle64(int Handle, out PWProperties pProp);

        [DllImport(DLLNAME, EntryPoint = "POAOpenPW", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAOpenPW64(int Handle);

        [DllImport(DLLNAME, EntryPoint = "POAClosePW", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAClosePW64(int Handle);

        [DllImport(DLLNAME, EntryPoint = "POAGetCurrentPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetCurrentPosition64(int Handle, out int pPosition);

        [DllImport(DLLNAME, EntryPoint = "POAGotoPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGotoPosition64(int Handle, int position);

        [DllImport(DLLNAME, EntryPoint = "POAGetOneWay", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetOneWay64(int Handle, out int pIsOneWay);

        [DllImport(DLLNAME, EntryPoint = "POASetOneWay", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POASetOneWay64(int Handle, int isOneWay);

        [DllImport(DLLNAME, EntryPoint = "POAGetPWState", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetPWState64(int Handle, out PWState pPWState);

        [DllImport(DLLNAME, EntryPoint = "POAGetPWFilterAlias", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetPWFilterAlias64(int Handle, int position, IntPtr pNameBufOut, int bufLen);

        [DllImport(DLLNAME, EntryPoint = "POASetPWFilterAlias", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POASetPWFilterAlias64(int Handle, int position, IntPtr pNameBufIn, int bufLen);

        [DllImport(DLLNAME, EntryPoint = "POAGetPWFocusOffset", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetPWFocusOffset64(int Handle, int position, out int pFocusOffsets);

        [DllImport(DLLNAME, EntryPoint = "POASetPWFocusOffset", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POASetPWFocusOffset64(int Handle, int position, int focusOffsets);
        [DllImport("PlayerOnePW.dll", EntryPoint = "POAGetPWCustomName", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetPWCustomName32(int Handle, IntPtr pNameBufOut, int bufLen);

        [DllImport(DLLNAME, EntryPoint = "POAGetPWCustomName", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAGetPWCustomName64(int Handle, IntPtr pNameBufOut, int bufLen);

        [DllImport(DLLNAME, EntryPoint = "POASetPWCustomName", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POASetPWCustomName64(int Handle, IntPtr pNameBufIn, int bufLen);

        [DllImport(DLLNAME, EntryPoint = "POAResetPW", CallingConvention = CallingConvention.Cdecl)]
        private static extern PWErrors POAResetPW64(int Handle);

        [DllImport(DLLNAME, EntryPoint = "POAGetPWErrorString", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr POAGetPWErrorString64(PWErrors err);

        [DllImport(DLLNAME, EntryPoint = "POAGetPWAPIVer", CallingConvention = CallingConvention.Cdecl)]
        private static extern int POAGetPWAPIVer64();

        [DllImport(DLLNAME, EntryPoint = "POAGetPWSDKVer", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr POAGetPWSDKVer64();


        //define c sharp interface

        public static int POAGetPWCount() { return  POAGetPWCount64(); }


        public static PWErrors POAGetPWProperties(int index, out PWProperties pProp) { return  POAGetPWProperties64(index, out pProp); }


        public static PWErrors POAGetPWPropertiesByHandle(int Handle, out PWProperties pProp) { return  POAGetPWPropertiesByHandle64(Handle, out pProp); }


        public static PWErrors POAOpenPW(int Handle) { return  POAOpenPW64(Handle); }


        public static PWErrors POAClosePW(int Handle) { return  POAClosePW64(Handle); }


        public static PWErrors POAGetCurrentPosition(int Handle, out int pPosition) { return  POAGetCurrentPosition64(Handle, out pPosition); }


        public static PWErrors POAGotoPosition(int Handle, int position) { return  POAGotoPosition64(Handle, position); }


        public static PWErrors POAGetOneWay(int Handle, out bool pIsOneWay) {
            int isOneway;
            PWErrors error;
            error = POAGetOneWay64(Handle, out isOneway);

            pIsOneWay = (isOneway != 0);

            return error;
        }


        public static PWErrors POASetOneWay(int Handle, bool isOneWay) { return  POASetOneWay64(Handle, isOneWay ? 1 : 0); }


        public static PWErrors POAGetPWState(int Handle, out PWState pPWState) { return  POAGetPWState64(Handle, out pPWState); }


        public static PWErrors POAGetPWFilterAlias(int Handle, int position, out string strfilterAlias) {
            int bufLen = MAX_FILTER_NAME_LEN + 8;//more 8 char for the'\0'
            IntPtr pNameBuf = Marshal.AllocCoTaskMem(bufLen);
            byte[] myByteArray = Enumerable.Repeat((byte)0, bufLen).ToArray();
            Marshal.Copy(myByteArray, 0, pNameBuf, bufLen); // memset to 0

            PWErrors error = POAGetPWFilterAlias64(Handle, position, pNameBuf, bufLen);


            strfilterAlias = Marshal.PtrToStringAnsi(pNameBuf);

            Marshal.FreeCoTaskMem(pNameBuf);

            return error;
        }


        public static PWErrors POASetPWFilterAlias(int Handle, int position, string strfilterAlias) {
            IntPtr pNameBuf = Marshal.StringToCoTaskMemAnsi(strfilterAlias);
            int bufLen = strfilterAlias.Length;

            PWErrors error = POASetPWFilterAlias64(Handle, position, pNameBuf, bufLen);


            Marshal.FreeCoTaskMem(pNameBuf);

            return error;
        }

        public static PWErrors POAGetPWFocusOffset(int Handle, int position, out int pFocusOffset) { return  POAGetPWFocusOffset64(Handle, position, out pFocusOffset); }


        public static PWErrors POASetPWFocusOffset(int Handle, int position, int focusOffset) { return  POASetPWFocusOffset64(Handle, position, focusOffset); }


        public static PWErrors POAGetPWCustomName(int Handle, out string strCustomName) {
            int bufLen = MAX_FILTER_NAME_LEN + 8;//more 8 char for the'\0'
            IntPtr pNameBuf = Marshal.AllocCoTaskMem(bufLen);
            byte[] myByteArray = Enumerable.Repeat((byte)0, bufLen).ToArray();
            Marshal.Copy(myByteArray, 0, pNameBuf, bufLen); // memset to 0

            PWErrors error = POAGetPWCustomName64(Handle, pNameBuf, bufLen);

            strCustomName = Marshal.PtrToStringAnsi(pNameBuf);

            Marshal.FreeCoTaskMem(pNameBuf);

            return error;
        }


        public static PWErrors POASetPWCustomName(int Handle, string strCustomName) {
            IntPtr pNameBuf = Marshal.StringToCoTaskMemAnsi(strCustomName);
            int bufLen = strCustomName.Length;

            PWErrors error = POASetPWCustomName64(Handle, pNameBuf, bufLen);

            Marshal.FreeCoTaskMem(pNameBuf);

            return error;
        }


        public static PWErrors POAResetPW(int Handle) { return POAResetPW64(Handle); }


        public static string POAGetPWErrorString(PWErrors err) { return  Marshal.PtrToStringAnsi(POAGetPWErrorString64(err)); }


        public static int POAGetPWAPIVer() { return  POAGetPWAPIVer64(); }


        public static string POAGetPWSDKVer() { return  Marshal.PtrToStringAnsi(POAGetPWSDKVer64()); }
    }
}
