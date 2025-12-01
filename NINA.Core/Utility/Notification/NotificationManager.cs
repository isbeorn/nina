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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace NINA.Core.Utility.Notification {
    internal sealed class NotificationManager : IDisposable {
        private readonly Dispatcher dispatcher;
        private readonly int maxVisible;
        private readonly Queue<CustomNotification> pendingNotifications = new();
        private readonly Dictionary<CustomNotification, DispatcherTimer> timers = new();

        private readonly ObservableCollection<CustomNotification> notifications
            = new ObservableCollection<CustomNotification>();

        private NotificationHostWindow hostWindow;
        private bool disposed;

        public NotificationManager(Dispatcher dispatcher, int maxVisible) {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.maxVisible = maxVisible;
        }

        public ObservableCollection<CustomNotification> Notifications => notifications;

        private void EnsureHostWindow() {
            if (hostWindow != null) {
                return;
            }

            // Make sure window is created on the UI thread
            if (!dispatcher.CheckAccess()) {
                dispatcher.Invoke(EnsureHostWindow);
                return;
            }

            hostWindow = new NotificationHostWindow {
                DataContext = this
            };
            hostWindow.Show();
            hostWindow.Hide();
        }

        public void Show(CustomNotification notification) {
            if (disposed || notification == null) return;

            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                if (disposed) return;

                if (notifications.Count >= maxVisible) {
                    pendingNotifications.Enqueue(notification);
                    return;
                }

                ShowInternal(notification);
            }));
        }

        private void ShowInternal(CustomNotification notification) {
            EnsureHostWindow();

            notifications.Add(notification);
            hostWindow.ShowIfNeeded();

            if (notification.Lifetime > TimeSpan.Zero) {
                var timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher) {
                    Interval = notification.Lifetime
                };
                timer.Tick += (s, e) => {
                    timer.Stop();
                    Close(notification);
                };
                timers[notification] = timer;
                timer.Start();
            }
        }


        public void Close(CustomNotification notification) {
            if (disposed || notification == null) return;

            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                if (!notifications.Contains(notification)) {
                    return;
                }

                notifications.Remove(notification);

                if (timers.TryGetValue(notification, out var timer)) {
                    timer.Stop();
                    timers.Remove(notification);
                }

                if (pendingNotifications.Count > 0) {
                    var next = pendingNotifications.Dequeue();
                    ShowInternal(next);
                }

                if (notifications.Count == 0) {
                    hostWindow.HideIfPossible();
                }
            }));
        }

        public void CloseAll() {
            if (disposed) return;

            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                foreach (var kvp in timers.ToList()) {
                    kvp.Value.Stop();
                }
                timers.Clear();

                notifications.Clear();
                pendingNotifications.Clear();
                hostWindow.HideIfPossible();
            }));
        }

        public void Dispose() {
            if (disposed) {
                return;
            }

            disposed = true;

            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                CloseAll();
                hostWindow?.Close();
                hostWindow = null;
            }));
        }
    }
}