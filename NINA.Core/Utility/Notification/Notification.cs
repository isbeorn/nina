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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Core.Utility.Notification {
    public static class Notification {

        static Notification() {
            lock (_lock) {
                Initialize();
            }
        }

        private static Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        private static NotificationManager manager;
        private static readonly object _lock = new object();
        private static void Initialize() {
            if (Application.Current == null) {
                dispatcher = null;
                manager = null;
                return;
            }

            dispatcher = Application.Current.Dispatcher;
            manager = new NotificationManager(dispatcher, maxVisible: 5, workAreaProvider: new PrimaryScreenWorkAreaProvider(), corner: NotificationCorner.BottomRight, offsetX: 15, offsetY: 5);
        }

        public static void ConfigurePosition(
            NotificationWorkArea workArea,
            NotificationCorner corner) {

            INotificationWorkAreaProvider provider;
            int offsetX;
            int offsetY;
            switch (workArea) {
                case NotificationWorkArea.PrimaryScreen:
                    provider = new PrimaryScreenWorkAreaProvider();
                    offsetX = 15;
                    offsetY = 5;
                    break;
                case NotificationWorkArea.SameScreenAsApplication:
                    provider = new SameScreenAsApplicationWorkAreaProvider();
                    offsetX = 15;
                    offsetY = 5;
                    break;
                case NotificationWorkArea.Application:
                    provider = new MainWindowWorkAreaProvider();
                    offsetX = 15;
                    offsetY = 35;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(workArea), workArea, null);
            }

            lock (_lock) {
                manager?.UpdatePosition(provider, corner, offsetX, offsetY);
            }
        }

        // Helpers to construct CustomNotification
        private static CustomNotification CreateNotification(
            string header,
            string message,
            Geometry symbol,
            Brush color,
            Brush background,
            TimeSpan lifetime) {

            return new CustomNotification(
                header,
                message,
                symbol,
                color,
                background,
                lifetime,
                closeAction: n => manager?.Close(n),
                closeAllAction: () => manager?.CloseAll());
        }

        public static void ShowInformation(string message) {
            ShowInformation(message, TimeSpan.FromSeconds(10));
        }

        public static void ShowInformation(string message, TimeSpan lifetime) {
            lock (_lock) {
                if (manager == null) return;

                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    GeometryGroup symbol = null;
                    if (Application.Current != null && Application.Current.Resources.Contains("AboutSVG")) {
                        symbol = (GeometryGroup)Application.Current.Resources["AboutSVG"];
                    }

                    var brush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
                    var background = new SolidColorBrush(Color.FromArgb(255, 42, 42, 42));

                    var notification = CreateNotification(
                        Locale.Loc.Instance["LblInfo"],
                        message,
                        symbol,
                        brush,
                        background,
                        lifetime);

                    manager.Show(notification);
                }));
            }
        }

        public static void ShowSuccess(string message) {
            lock (_lock) {
                if (manager == null) return;

                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    GeometryGroup symbol = null;
                    if (Application.Current != null && Application.Current.Resources.Contains("CheckedCircledSVG")) {
                        symbol = (GeometryGroup)Application.Current.Resources["CheckedCircledSVG"];
                    }

                    var brush = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    var background = new SolidColorBrush(Color.FromArgb(255, 42, 42, 42));

                    var notification = CreateNotification(
                        Locale.Loc.Instance["LblSuccess"],
                        message,
                        symbol,
                        brush,
                        background,
                        TimeSpan.FromSeconds(10));

                    manager.Show(notification);
                }));
            }
        }

        public static void ShowWarning(string message) {
            ShowWarning(message, TimeSpan.FromSeconds(30));
        }

        public static void ShowWarning(string message, TimeSpan lifetime) {
            lock (_lock) {
                if (manager == null) return;

                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    GeometryGroup symbol = null;
                    if (Application.Current != null && Application.Current.Resources.Contains("ExclamationCircledSVG")) {
                        symbol = (GeometryGroup)Application.Current.Resources["ExclamationCircledSVG"];
                    }

                    var brush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                    var background = new SolidColorBrush(Color.FromArgb(255, 42, 42, 42));

                    var notification = CreateNotification(
                        Locale.Loc.Instance["LblWarning"],
                        message,
                        symbol,
                        brush,
                        background,
                        lifetime);

                    manager.Show(notification);
                }));
            }
        }

        public static void ShowError(string message) {
            lock (_lock) {
                if (manager == null) return;

                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    GeometryGroup symbol = null;
                    if (Application.Current != null && Application.Current.Resources.Contains("CancelCircledSVG")) {
                        symbol = (GeometryGroup)Application.Current.Resources["CancelCircledSVG"];
                    }

                    var brush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    var background = new SolidColorBrush(Color.FromArgb(255, 42, 42, 42));

                    var notification = CreateNotification(
                        Locale.Loc.Instance["LblError"],
                        message,
                        symbol,
                        brush,
                        background,
                        TimeSpan.FromHours(24));

                    manager.Show(notification);
                }));
            }
        }

        public static void ShowExternalError(string message, string header) {
            lock (_lock) {
                if (manager == null) return;

                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    GeometryGroup symbol = null;
                    if (Application.Current != null && Application.Current.Resources.Contains("CommunicationErrorSVG")) {
                        symbol = (GeometryGroup)Application.Current.Resources["CommunicationErrorSVG"];
                    }

                    var brush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    var background = new SolidColorBrush(Color.FromArgb(255, 42, 42, 42));

                    if (string.IsNullOrWhiteSpace(header)) {
                        header = Locale.Loc.Instance["LblExternalError"];
                    }

                    var notification = CreateNotification(
                        header,
                        message,
                        symbol,
                        brush,
                        background,
                        TimeSpan.FromHours(24));

                    manager.Show(notification);
                }));
            }
        }

        public static void ShowExternalWarning(string message, string header) {
            lock (_lock) {
                if (manager == null) return;

                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    GeometryGroup symbol = null;
                    if (Application.Current != null && Application.Current.Resources.Contains("CommunicationWarningSVG")) {
                        symbol = (GeometryGroup)Application.Current.Resources["CommunicationWarningSVG"];
                    }

                    var brush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                    var background = new SolidColorBrush(Color.FromArgb(255, 42, 42, 42));

                    if (string.IsNullOrWhiteSpace(header)) {
                        header = Locale.Loc.Instance["LblExternalError"];
                    }

                    var notification = CreateNotification(
                        header,
                        message,
                        symbol,
                        brush,
                        background,
                        TimeSpan.FromHours(24));

                    manager.Show(notification);
                }));
            }
        }

        public static void CloseAll() {
            lock (_lock) {
                manager?.CloseAll();
            }
        }

        public static void Dispose() {
            lock (_lock) {
                manager?.Dispose();
                manager = null;
            }
        }
    }
}