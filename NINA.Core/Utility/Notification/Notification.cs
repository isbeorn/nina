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
using System.Threading.Tasks;

namespace NINA.Core.Utility.Notification {

    public static class Notification {
        /// <summary>
        /// Delegate for broadcasting notifications via SignalR or other mechanisms
        /// </summary>
        public delegate Task NotificationBroadcaster(NotificationMessage message);

        /// <summary>
        /// Static broadcaster instance that can be set by the server to forward notifications
        /// </summary>
        public static NotificationBroadcaster Broadcaster { get; set; }

        static Notification() {
        }

        public static void ShowInformation(string message) {
            ShowInformation(message, TimeSpan.FromSeconds(10));
        }

        public static void ShowInformation(string message, TimeSpan lifetime) {
            BroadcastNotification(new NotificationMessage {
                Type = NotificationType.Information,
                Message = message,
                Lifetime = lifetime
            });
        }

        public static void ShowSuccess(string message) {
            BroadcastNotification(new NotificationMessage {
                Type = NotificationType.Success,
                Message = message,
                Lifetime = TimeSpan.FromSeconds(10)
            });
        }

        public static void ShowWarning(string message) {
            ShowWarning(message, TimeSpan.FromSeconds(30));
        }

        public static void ShowWarning(string message, TimeSpan lifetime) {
            BroadcastNotification(new NotificationMessage {
                Type = NotificationType.Warning,
                Message = message,
                Lifetime = lifetime
            });
        }

        public static void ShowError(string message) {
            BroadcastNotification(new NotificationMessage {
                Type = NotificationType.Error,
                Message = message,
                Lifetime = TimeSpan.FromHours(24)
            });
        }

        public static void ShowExternalError(string message, string header) {
            BroadcastNotification(new NotificationMessage {
                Type = NotificationType.Error,
                Message = message,
                Title = header,
                Lifetime = TimeSpan.FromHours(24)
            });
        }

        public static void ShowExternalWarning(string message, string header) {
            BroadcastNotification(new NotificationMessage {
                Type = NotificationType.Warning,
                Message = message,
                Title = header,
                Lifetime = TimeSpan.FromHours(24)
            });
        }

        private static void BroadcastNotification(NotificationMessage message) {
            try {
                var task = Broadcaster?.Invoke(message);
                if (task != null) {
                    _ = task; // Fire and forget async operation
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to broadcast notification: {ex}");
            }
        }

        public static void CloseAll() {
        }

        public static void Dispose() {
            Broadcaster = null;
        }
    }

    /// <summary>
    /// Represents a notification message to be broadcast
    /// </summary>
    public class NotificationMessage {
        public NotificationType Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public TimeSpan Lifetime { get; set; } = TimeSpan.FromSeconds(30);
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Notification type enumeration
    /// </summary>
    public enum NotificationType {
        Information,
        Success,
        Warning,
        Error
    }
}
