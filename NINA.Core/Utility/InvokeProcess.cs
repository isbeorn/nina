#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
namespace NINA.Core.Utility {
    public static class InvokeProcess {
        const int NormalPriorityClass = 0x0020;
        [StructLayout(LayoutKind.Sequential)]
        internal class ProcessInformation {
            public IntPtr hProcess = IntPtr.Zero;
            public IntPtr hThread = IntPtr.Zero;
            public int dwProcessId;
            public int dwThreadId;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class StartupInfo {
            public int cb;
            public IntPtr lpReserved = IntPtr.Zero;
            public IntPtr lpDesktop = IntPtr.Zero;
            public IntPtr lpTitle = IntPtr.Zero;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2 = IntPtr.Zero;
            public SafeFileHandle hStdInput = new SafeFileHandle(IntPtr.Zero, false);
            public SafeFileHandle hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
            public SafeFileHandle hStdError = new SafeFileHandle(IntPtr.Zero, false);
            public StartupInfo() {
                cb = Marshal.SizeOf(this);
            }
            public void Dispose() {
                // close the handles created for child process
                if (hStdInput != null && !hStdInput.IsInvalid) {
                    hStdInput.Close();
                    hStdInput = null;
                }
                if (hStdOutput != null && !hStdOutput.IsInvalid) {
                    hStdOutput.Close();
                    hStdOutput = null;
                }
                if (hStdError == null || hStdError.IsInvalid) return;
                hStdError.Close();
                hStdError = null;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class SecurityAttributes {
            public int nLength = 12;
            public SafeLocalMemHandle lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, false);
            public bool bInheritHandle;
        }
        [SuppressUnmanagedCodeSecurity, HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
        internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid {
            internal SafeLocalMemHandle() : base(true) { }
            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle) {
                SetHandle(existingHandle);
            }
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string stringSecurityDescriptor,
                int stringSDRevision, out SafeLocalMemHandle pSecurityDescriptor, IntPtr securityDescriptorSize);
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll")]
            private static extern IntPtr LocalFree(IntPtr hMem);
            protected override bool ReleaseHandle() {
                return (LocalFree(handle) == IntPtr.Zero);
            }
        }
        [DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern bool CreateProcess(
             [MarshalAs(UnmanagedType.LPTStr)] string applicationName,
             StringBuilder commandLine,
             SecurityAttributes processAttributes,
             SecurityAttributes threadAttributes,
             bool inheritHandles,
             int creationFlags,
             IntPtr environment,
             [MarshalAs(UnmanagedType.LPTStr)] string currentDirectory,
             StartupInfo startupInfo,
             ProcessInformation processInformation
        );
        public static void CreateProcess(string processPath) {
            var processInformation = new ProcessInformation();
            var startupInfo = new StartupInfo();
            var processSecurity = new SecurityAttributes();
            var threadSecurity = new SecurityAttributes();
            processSecurity.nLength = Marshal.SizeOf(processSecurity);
            threadSecurity.nLength = Marshal.SizeOf(threadSecurity);
            bool inheritHandles = false;
            if (CreateProcess(null, new StringBuilder(processPath), processSecurity, threadSecurity, inheritHandles, NormalPriorityClass,
                 IntPtr.Zero, null, startupInfo, processInformation)) {
                return;
            }
            // We couldn't create the process, so raise an exception with the details.
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }
}