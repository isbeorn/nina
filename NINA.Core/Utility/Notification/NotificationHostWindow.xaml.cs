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
        private NotificationManager Manager => DataContext as NotificationManager;

        public NotificationHostWindow() {
            InitializeComponent();

            Loaded += OnLoaded;
            SizeChanged += (_, __) => Reposition();
            IsVisibleChanged += (_, __) => { if (IsVisible) Reposition(); };
            Activated += (_, __) => Reposition();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Reposition();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            SubscribeToMainWindowEvents();
        }

        private void SubscribeToMainWindowEvents() {
            var main = Application.Current?.MainWindow;
            if (main == null) return;

            main.LocationChanged += OnMainWindowLayoutChanged;
            main.SizeChanged += OnMainWindowLayoutChanged;
            main.StateChanged += OnMainWindowLayoutChanged;
        }

        private void UnsubscribeFromMainWindowEvents() {
            var main = Application.Current?.MainWindow;
            if (main == null) return;

            main.LocationChanged -= OnMainWindowLayoutChanged;
            main.SizeChanged -= OnMainWindowLayoutChanged;
            main.StateChanged -= OnMainWindowLayoutChanged;
        }

        private void OnMainWindowLayoutChanged(object sender, EventArgs e) {
            Dispatcher.BeginInvoke(new Action(Reposition));
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e) {
            Dispatcher.BeginInvoke(new Action(Reposition));
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            UnsubscribeFromMainWindowEvents();
        }

        private Rect GetWorkArea() {
            var provider = Manager?.WorkAreaProvider ?? new PrimaryScreenWorkAreaProvider();
            return provider.GetWorkArea();
        }

        public void Reposition() {
            var workArea = GetWorkArea();

            if (double.IsNaN(Width) || Width == 0 || double.IsNaN(Height) || Height == 0) {
                UpdateLayout();
            }

            var corner = Manager?.Corner ?? NotificationCorner.BottomRight;
            var offsetX = Manager?.OffsetX ?? 15;
            var offsetY = Manager?.OffsetY ?? 10;

            double left, top;

            switch (corner) {
                case NotificationCorner.TopLeft:
                    left = workArea.Left + offsetX;
                    top = workArea.Top + offsetY;
                    break;

                case NotificationCorner.TopRight:
                    left = workArea.Right - ActualWidth - offsetX;
                    top = workArea.Top + offsetY;
                    break;

                case NotificationCorner.BottomLeft:
                    left = workArea.Left + offsetX;
                    top = workArea.Bottom - ActualHeight - offsetY;
                    break;

                case NotificationCorner.BottomRight:
                default:
                    left = workArea.Right - ActualWidth - offsetX;
                    top = workArea.Bottom - ActualHeight - offsetY;
                    break;
            }

            Left = left;
            Top = top;
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