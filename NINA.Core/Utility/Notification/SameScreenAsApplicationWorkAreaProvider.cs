#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace NINA.Core.Utility.Notification {
    public sealed class SameScreenAsApplicationWorkAreaProvider : INotificationWorkAreaProvider {

        public Rect GetWorkArea() {
            var app = Application.Current;
            var main = app?.MainWindow;

            // No main window? Fallback to primary work area
            if (main == null) {
                return SystemParameters.WorkArea;
            }

            var helper = new WindowInteropHelper(main);
            if (helper.Handle == IntPtr.Zero) {
                return SystemParameters.WorkArea;
            }

            // Get monitor for the window
            const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr hMonitor = MonitorFromWindow(helper.Handle, MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero) {
                return SystemParameters.WorkArea;
            }

            // Get monitor info
            var mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            if (!GetMonitorInfo(hMonitor, ref mi)) {
                return SystemParameters.WorkArea;
            }

            // Use the monitor's work area (taskbar aware)
            var wa = mi.rcWork;
            return ConvertToWpfRect(wa, main);
        }

        private static Rect ConvertToWpfRect(RECT wa, Visual visualForDpi) {
            // If we have a visual, use its device->DIP transform
            if (visualForDpi != null) {
                var source = PresentationSource.FromVisual(visualForDpi);
                var ct = source?.CompositionTarget;
                if (ct != null) {
                    var transform = ct.TransformFromDevice;

                    var topLeft = transform.Transform(new Point(wa.Left, wa.Top));
                    var bottomRight = transform.Transform(new Point(wa.Right, wa.Bottom));

                    return new Rect(topLeft, bottomRight);
                }
            }

            // Fallback: assume 1:1 pixels to DIPs (96 DPI)
            return new Rect(wa.Left, wa.Top, wa.Right - wa.Left, wa.Bottom - wa.Top);
        }

        #region Win32

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        #endregion
    }
}