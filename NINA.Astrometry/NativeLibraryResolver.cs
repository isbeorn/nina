#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NINA.Astrometry {
    /// <summary>
    /// Centralized native library resolver for NOVAS and SOFA libraries
    /// </summary>
    internal static class NativeLibraryResolver {
        private static bool _resolverSet = false;
        private static readonly object _resolverLock = new object();

        internal static void EnsureResolverSet() {
            lock (_resolverLock) {
                if (!_resolverSet) {
                    NativeLibrary.SetDllImportResolver(typeof(NativeLibraryResolver).Assembly, ResolveLibrary);
                    _resolverSet = true;
                }
            }
        }

        private static IntPtr ResolveLibrary(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath) {
            var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
            var platformFolder = Environment.Is64BitProcess ? $"linux-{arch}" : $"linux-{arch}";
            string subfolder = null;

            // Determine which library and subfolder
            if (libraryName == "libnovas_c.so") {
                subfolder = "NOVAS";
            } else if (libraryName == "libsofa_c.so") {
                subfolder = "SOFA";
            }

            if (subfolder != null) {
                var libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "External", platformFolder, subfolder, libraryName);

                if (File.Exists(libPath)) {
                    Logger.Debug($"NativeLibraryResolver: Loading {libPath}");
                    if (NativeLibrary.TryLoad(libPath, out var handle)) {
                        return handle;
                    }
                }
            }

            return IntPtr.Zero;
        }
    }
}
