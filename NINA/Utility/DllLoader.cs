using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NINA.Utility {

    public static class DllLoader {

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private extern static IntPtr LoadLibrary(string librayName);

        public static void LoadDll(string dllSubPath) {
            String path;

            //IntPtr.Size will be 4 in 32-bit processes, 8 in 64-bit processes
            if (IsX86())
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x86/" + dllSubPath;
            else
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x64/" + dllSubPath;

            LoadLibrary(path);
        }

        public static FileVersionInfo DllVersion(string dllSubPath) {
            String path;

            //IntPtr.Size will be 4 in 32-bit processes, 8 in 64-bit processes
            if (IsX86())
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x86/" + dllSubPath;
            else
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x64/" + dllSubPath;

            return FileVersionInfo.GetVersionInfo(path.Replace('/', '\\'));
        }

        public static bool IsX86() {
            return IntPtr.Size == 4;
        }
    }
}