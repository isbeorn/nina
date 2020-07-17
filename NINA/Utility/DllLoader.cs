#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace NINA.Utility {

    public static class DllLoader {

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private extern static IntPtr LoadLibrary(string librayName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static object lockobj = new object();

        public static void LoadDll(string dllSubPath) {
            lock (lockobj) {
                String path;

                //IntPtr.Size will be 4 in 32-bit processes, 8 in 64-bit processes
                if (IsX86())
                    path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", "x86", dllSubPath);
                else
                    path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", "x64", dllSubPath);

                SetDllDirectory(System.IO.Path.GetDirectoryName(path));

                if (LoadLibrary(path) == IntPtr.Zero) {
                    var error = Marshal.GetLastWin32Error().ToString();
                    var message = $"DllLoader failed to load library {dllSubPath} due to error code {error}";
                    Logger.Error(message);
                }

                SetDllDirectory(string.Empty);
            }
        }

        public static FileVersionInfo DllVersion(string dllSubPath) {
            String path;

            //IntPtr.Size will be 4 in 32-bit processes, 8 in 64-bit processes
            if (IsX86())
                path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", "x86", dllSubPath);
            else
                path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", "x64", dllSubPath);

            return FileVersionInfo.GetVersionInfo(path.Replace('/', '\\'));
        }

        public static bool IsX86() {
            return !Environment.Is64BitProcess;
        }
    }
}