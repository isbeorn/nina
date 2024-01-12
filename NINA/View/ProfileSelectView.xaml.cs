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
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for ProfileSelectView.xaml
    /// </summary>
    public partial class ProfileSelectView : Window {

        public ProfileSelectView() {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e) {
            var tmp = this.WindowState;
            this.WindowState = WindowState.Minimized;
            this.WindowState = tmp;
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
    }
}