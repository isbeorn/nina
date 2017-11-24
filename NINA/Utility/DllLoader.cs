using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    public static class DllLoader {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private extern static IntPtr LoadLibrary(string librayName);

        public static void LoadDll(string dllSubPath) {
            String path;

            //IntPtr.Size will be 4 in 32-bit processes, 8 in 64-bit processes 
            if (IntPtr.Size == 4)
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x86/" + dllSubPath;
            else
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x64/" + dllSubPath;

            LoadLibrary(path);
        }
    }
}
