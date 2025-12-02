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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace NINA.Core.Utility.Notification {
    internal sealed class NotificationManager : IDisposable {
        private readonly Dispatcher dispatcher;
        private readonly int maxVisible;
        private INotificationWorkAreaProvider workAreaProvider;
        private NotificationCorner corner;
        private double offsetX;
        private double offsetY;
        private readonly Queue<CustomNotification> pendingNotifications = new();
        private readonly ObservableCollection<CustomNotification> notifications = new ();
        private readonly Dictionary<CustomNotification, DateTime> expirationTimes = new ();
        private DispatcherTimer lifetimeTimer;
        private NotificationHostWindow hostWindow;
        private bool disposed;

        public NotificationManager(Dispatcher dispatcher,
                                   int maxVisible,
                                   INotificationWorkAreaProvider workAreaProvider,
                                   NotificationCorner corner,
                                   double offsetX,
                                   double offsetY) {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.maxVisible = maxVisible;
            this.workAreaProvider = workAreaProvider ?? new PrimaryScreenWorkAreaProvider();
            this.corner = corner;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
        }

        public INotificationWorkAreaProvider WorkAreaProvider => workAreaProvider;
        public NotificationCorner Corner => corner;
        public double OffsetX => offsetX;
        public double OffsetY => offsetY;

        public void UpdatePosition(
            INotificationWorkAreaProvider provider,
            NotificationCorner corner,
            double offsetX,
            double offsetY) {

            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                this.workAreaProvider = provider ?? new PrimaryScreenWorkAreaProvider();
                this.corner = corner;
                this.offsetX = offsetX;
                this.offsetY = offsetY;

                hostWindow?.Reposition();
            }));
        }

        public ObservableCollection<CustomNotification> Notifications => notifications;

        private void EnsureHostWindow() {
            if (hostWindow != null) {
                return;
            }

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

        private void EnsureLifetimeTimer() {
            if (lifetimeTimer != null) {
                return;
            }

            lifetimeTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher) {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            lifetimeTimer.Tick += (_, __) => OnLifetimeTick();
        }

        private void StartLifetimeTimerIfNeeded() {
            if (expirationTimes.Count == 0 || lifetimeTimer == null) {
                return;
            }

            if (!lifetimeTimer.IsEnabled) {
                lifetimeTimer.Start();
            }
        }

        private void StopLifetimeTimerIfPossible() {
            if (lifetimeTimer == null) {
                return;
            }

            if (expirationTimes.Count == 0) {
                lifetimeTimer.Stop();
            }
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
            EnsureLifetimeTimer();

            notifications.Add(notification);
            hostWindow.ShowIfNeeded();

            if (notification.Lifetime > TimeSpan.Zero) {
                expirationTimes[notification] = DateTime.UtcNow + notification.Lifetime;
                StartLifetimeTimerIfNeeded();
            }
        }

        public void Close(CustomNotification notification) {
            if (disposed || notification == null) return;

            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                if (!notifications.Contains(notification)) {
                    // Might be queued but not yet shown; ensure it is not in queue either.
                    RemoveFromQueue(notification);
                    expirationTimes.Remove(notification);
                    StopLifetimeTimerIfPossible();
                    return;
                }

                notifications.Remove(notification);
                expirationTimes.Remove(notification);

                // Show next from queue, if any
                if (pendingNotifications.Count > 0) {
                    var next = pendingNotifications.Dequeue();
                    ShowInternal(next);
                }

                if (notifications.Count == 0) {
                    hostWindow.HideIfPossible();
                }

                StopLifetimeTimerIfPossible();
            }));
        }

        private void RemoveFromQueue(CustomNotification notification) {
            if (pendingNotifications.Count == 0) {
                return;
            }

            // Rebuild queue without the item
            var temp = pendingNotifications.ToList();
            pendingNotifications.Clear();
            foreach (var n in temp) {
                if (!ReferenceEquals(n, notification)) {
                    pendingNotifications.Enqueue(n);
                }
            }
        }

        public void CloseAll() {
            if (disposed) return;

            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                notifications.Clear();
                pendingNotifications.Clear();
                expirationTimes.Clear();

                if (lifetimeTimer != null) {
                    lifetimeTimer.Stop();
                }

                hostWindow?.HideIfPossible();
            }));
        }

        private void OnLifetimeTick() {
            if (disposed) return;

            var now = DateTime.UtcNow;
            var toClose = expirationTimes
                .Where(kv => kv.Value <= now)
                .Select(kv => kv.Key)
                .ToList();

            if (toClose.Count == 0) {
                return;
            }

            foreach (var n in toClose) {
                Close(n);
            }
        }

        public void Dispose() {
            if (disposed) {
                return;
            }

            disposed = true;

            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                lifetimeTimer?.Stop();
                lifetimeTimer = null;

                notifications.Clear();
                pendingNotifications.Clear();
                expirationTimes.Clear();

                hostWindow?.Close();
                hostWindow = null;
            }));
        }
    }
}