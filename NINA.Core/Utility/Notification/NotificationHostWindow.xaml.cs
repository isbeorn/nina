#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Win32;
using System;
using System.Windows;

namespace NINA.Core.Utility.Notification {

    public partial class NotificationHostWindow : Window {
        public NotificationHostWindow() {
            InitializeComponent();

            Loaded += OnLoaded;
            SizeChanged += (_, __) => Reposition();
            IsVisibleChanged += (_, __) => {
                if (IsVisible) {
                    Reposition();
                }
            };
            Activated += (_, __) => Reposition();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Reposition();

            // Subscribe to display changes
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e) {
            // This event is not on the UI thread, so marshal back
            Dispatcher.BeginInvoke(new Action(Reposition));
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }

        private void Reposition() {
            // Always use primary screen work area 
            var workArea = SystemParameters.WorkArea;

            // In case ActualWidth/Height are 0 (not rendered yet), let WPF arrange once
            if (double.IsNaN(Width) || Width == 0 || double.IsNaN(Height) || Height == 0) {
                UpdateLayout();
            }

            Left = workArea.Right - ActualWidth;
            Top = workArea.Bottom - ActualHeight;
        }

        public void ShowIfNeeded() {
            if (!IsVisible) {
                Reposition();
                Show();
            } else {
                Reposition();
            }
        }

        public void HideIfPossible() {
            if (IsVisible) {
                Hide();
            }
        }
    }
}