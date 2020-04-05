using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NINA.Utility.FlatDeviceSDKs.AllPro {
    internal class USBD {
        static USBD() {
            DllLoader.LoadDll(Path.Combine("AllPro", "spikausbd.dll"));
        }

        [DllImport("spikausbd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr USBD_Open();

        [DllImport("spikausbd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void USBD_Close(IntPtr usbd);

        [DllImport("spikausbd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int USBD_Connect(IntPtr usbd);

        [DllImport("spikausbd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void USBD_Disconnect(IntPtr usbd);

        [DllImport("spikausbd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int USBD_SetBrightness(IntPtr usbd, uint val);

        [DllImport("spikausbd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int USBD_GetBrightness(IntPtr usbd);

        [DllImport("spikausbd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int USBD_IsLightOn(IntPtr usbd);

        [DllImport("spikausbd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int USBD_LightOn(IntPtr usbd, bool on);
    }
}
