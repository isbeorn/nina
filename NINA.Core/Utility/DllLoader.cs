#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

namespace NINA.Core.Utility {

    public static class DllLoader {

        [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr dlerror();

        // dlopen flags
        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 0x100;

        private static object lockobj = new object();

        public static void LoadDll(string dllSubPath) {
            var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
            var platformFolder = IsX86() ? $"linux-{arch}" : $"linux-{arch}";
            var extension = ".so";

            // Add extension if not present
            if (!Path.HasExtension(dllSubPath)) {
                dllSubPath = Path.ChangeExtension(dllSubPath, extension);
            }

            // On Linux, try system libraries first (using LD_LIBRARY_PATH)
            var libraryName = Path.GetFileName(dllSubPath);
            Logger.Info($"DllLoader: Trying system library via LD_LIBRARY_PATH: {libraryName}");

            if (!LoadDllFromAbsolutePath(libraryName, global: true)) {
                // If system library loading failed, fall back to bundled library
                var path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", platformFolder, dllSubPath);
                if (File.Exists(path)) {
                    Logger.Info($"DllLoader: Falling back to bundled library: {path}");
                    LoadDllFromAbsolutePath(path);
                }
            }
        }

        public static bool LoadDllFromAbsolutePath(string dllPath, bool global = false) {
            lock (lockobj) {
                IntPtr handle = IntPtr.Zero;

                int flags = RTLD_NOW;
                if (global) {
                    flags |= RTLD_GLOBAL;
                }

                // Linux implementation
                handle = dlopen(dllPath, flags);
                if (handle == IntPtr.Zero) {
                    var errorPtr = dlerror();
                    var errorMessage = errorPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(errorPtr) : "Unknown error";
                    var message = $"DllLoader failed to load library {dllPath}: {errorMessage}";
                    Logger.Error(message);
                    return false;
                }
                return true;
            }
        }

        public static FileVersionInfo DllVersion(string dllSubPath) {
            var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
            var platformFolder = IsX86() ? $"linux-{arch}" : $"linux-{arch}";
            var extension = ".so";

            // Add extension if not present
            if (!Path.HasExtension(dllSubPath)) {
                dllSubPath = Path.ChangeExtension(dllSubPath, extension);
            }

            var path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", platformFolder, dllSubPath);

            // On Unix-like systems, FileVersionInfo might not work well with native libraries
            // Return a minimal version info if the file exists
            if (File.Exists(path)) {
                return FileVersionInfo.GetVersionInfo(path);
            }
            return null;
        }

        public static bool IsX86() {
            return !Environment.Is64BitProcess;
        }
    }
}
