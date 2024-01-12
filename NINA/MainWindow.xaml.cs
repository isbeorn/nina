#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Microsoft.Win32;
using NINA.Core.Utility;
using SaveWindowState;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace NINA {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e) {
            var tmp = this.WindowState;
            this.WindowState = WindowState.Minimized;
            this.WindowState = tmp;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);

            try {
                // Load window placement details for previous application session from application settings
                // Note - if window was closed on a monitor that is now disconnected from the computer,
                //        SetWindowPlacement will place the window onto a visible monitor.
                var wp = Properties.Settings.Default.WindowPlacement;
                wp.length = Marshal.SizeOf(typeof(WindowPlacement));
                wp.flags = 0;
                wp.showCmd = (wp.showCmd == SwShowminimized ? SwShownormal : wp.showCmd);
                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowPlacement(hwnd, ref wp);
            } catch {
                // ignored
            }
        }

        private IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg == 0x0084 /*WM_NCHITTEST*/ ) {
                // This prevents a crash in WindowChromeWorker._HandleNCHitTest
                try {
                    lParam.ToInt32();
                } catch (OverflowException) {
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        // WARNING - Not fired when Application.SessionEnding is fired
        protected override void OnClosing(CancelEventArgs e) {
            // Persist window placement details to application settings
            WindowPlacement wp;
            var hwnd = new WindowInteropHelper(this).Handle;
            GetWindowPlacement(hwnd, out wp);
            Properties.Settings.Default.WindowPlacement = wp;
            CoreUtil.SaveSettings(Properties.Settings.Default);

            base.OnClosing(e);
        }

        #region Win32 API declarations to set and get window placement

        [DllImport("user32.dll")]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, out WindowPlacement lpwndpl);

        private const int SwShownormal = 1;
        private const int SwShowminimized = 2;

        #endregion
    }
}