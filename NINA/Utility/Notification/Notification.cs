#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Lifetime.Clear;
using ToastNotifications.Position;
using ToastNotifications.Utilities;

namespace NINA.Utility.Notification {

    internal static class Notification {

        static Notification() {
            lock (_lock) {
                Initialize();
            }
        }

        private static Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        private static Notifier notifier;

        private static object _lock = new object();

        private static void Initialize() {
            notifier = new Notifier(cfg => {
                /*cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 0);*/
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 1,
                    offsetY: 1);

                cfg.LifetimeSupervisor = new CustomLifetimeSupervisor();
            });
        }

        public static void ShowInformation(string message) {
            ShowInformation(message, TimeSpan.FromSeconds(5));
        }

        public static void ShowInformation(string message, TimeSpan lifetime) {
            lock (_lock) {
                if (notifier != null) {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                        notifier.Notify<CustomNotification>(() => new CustomNotification(message, lifetime));
                    }));
                }
            }
        }

        public static void ShowSuccess(string message) {
            lock (_lock) {
                if (notifier != null) {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                        var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CheckedCircledSVG"];
                        notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol, TimeSpan.FromSeconds(5)));
                    }));
                }
            }
        }

        public static void ShowWarning(string message) {
            ShowWarning(message, TimeSpan.FromSeconds(30));
        }

        public static void ShowWarning(string message, TimeSpan lifetime) {
            lock (_lock) {
                if (notifier != null) {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                        var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ExclamationCircledSVG"];
                        var brush = (Brush)System.Windows.Application.Current.Resources["NotificationWarningBrush"];
                        var foregroundBrush = (Brush)System.Windows.Application.Current.Resources["NotificationWarningTextBrush"];
                        notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol, brush, foregroundBrush, lifetime));
                    }));
                }
            }
        }

        public static void ShowError(string message) {
            lock (_lock) {
                if (notifier != null) {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                        var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CancelCircledSVG"];
                        var brush = (Brush)System.Windows.Application.Current.Resources["NotificationErrorBrush"];
                        var foregroundBrush = (Brush)System.Windows.Application.Current.Resources["NotificationErrorTextBrush"];
                        notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol, brush, foregroundBrush, TimeSpan.FromHours(24)));
                    }));
                }
            }
        }

        /// <summary>
        /// Disposes the notifier instance and supresses further notifications
        /// </summary>
        public static void Dispose() {
            lock (_lock) {
                notifier.Dispose();
                notifier = null;
            }
        }
    }

    public class CustomNotification : NotificationBase, INotifyPropertyChanged {
        private CustomDisplayPart _displayPart;

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = new CustomDisplayPart(this));

        public CustomNotification(string message, TimeSpan lifetime) : base(message, new MessageOptions()) {
            Color = (Brush)System.Windows.Application.Current.Resources["ButtonBackgroundBrush"];
            ForegroundColor = (Brush)System.Windows.Application.Current.Resources["ButtonForegroundBrush"];
            Lifetime = lifetime;
        }

        public CustomNotification(string message, Geometry symbol, TimeSpan lifetime) : base(message, new MessageOptions()) {
            Symbol = symbol;
            Color = (Brush)System.Windows.Application.Current.Resources["ButtonBackgroundBrush"];
            ForegroundColor = (Brush)System.Windows.Application.Current.Resources["ButtonForegroundBrush"];
            Lifetime = lifetime;
        }

        public CustomNotification(string message, Geometry symbol, Brush color, Brush foregroundColor, TimeSpan lifetime) : base(message, new MessageOptions()) {
            Symbol = symbol;
            Color = color;
            ForegroundColor = foregroundColor;
            Lifetime = lifetime;
        }

        public DateTime DateTime { get; private set; } = DateTime.Now;

        public TimeSpan Lifetime { get; }

        private Geometry _symbol;

        public Geometry Symbol {
            get {
                return _symbol;
            }
            set {
                _symbol = value;
                RaisePropertyChanged();
            }
        }

        private Brush _color;

        public Brush Color {
            get {
                return _color;
            }
            set {
                _color = value;
                RaisePropertyChanged();
            }
        }

        private Brush _foregroundColor;

        public Brush ForegroundColor {
            get {
                return _foregroundColor;
            }
            set {
                _foregroundColor = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName]string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CustomLifetimeSupervisor : INotificationsLifetimeSupervisor {
        private readonly int _maximumNotificationCount;

        private Dispatcher _dispatcher;
        private CustomNotificationsList _notifications;
        private Queue<INotification> _notificationsPending;

        private IInterval _interval;

        public CustomLifetimeSupervisor() {
            _notifications = new CustomNotificationsList();
            _maximumNotificationCount = 5;

            _notifications = new CustomNotificationsList();
            _interval = new Interval();
        }

        public void PushNotification(INotification notification) {
            var lifetime = TimeSpan.FromSeconds(3);
            if (notification.GetType() == typeof(CustomNotification)) {
                var customNotification = (CustomNotification)notification;
                lifetime = customNotification.Lifetime;
            }

            if (_interval.IsRunning == false)
                TimerStart();

            if (_notifications.Count == _maximumNotificationCount) {
                if (_notificationsPending == null) {
                    _notificationsPending = new Queue<INotification>();
                }
                _notificationsPending.Enqueue(notification);
                return;
            }

            int numberOfNotificationsToClose = Math.Max(_notifications.Count - _maximumNotificationCount + 1, 0);

            var notificationsToRemove = _notifications
                .OrderBy(x => x.Key)
                .Take(numberOfNotificationsToClose)
                .Select(x => x.Value)
                .ToList();

            foreach (var n in notificationsToRemove)
                CloseNotification(n.Notification);

            _notifications.Add(notification, lifetime);
            RequestShowNotification(new ShowNotificationEventArgs(notification));
        }

        public void CloseNotification(INotification notification) {
            NotificationMetaData removedNotification;
            _notifications.TryRemove(notification.Id, out removedNotification);
            RequestCloseNotification(new CloseNotificationEventArgs(removedNotification.Notification));

            if (_notificationsPending != null && _notificationsPending.Any()) {
                var not = _notificationsPending.Dequeue();
                PushNotification(not);
            }
        }

        public void Dispose() {
            _interval.Stop();
            _interval = null;
            _notifications?.Clear();
            _notifications = null;
        }

        public void UseDispatcher(Dispatcher dispatcher) {
            _dispatcher = dispatcher;
        }

        protected virtual void RequestShowNotification(ShowNotificationEventArgs e) {
            try {
                ShowNotificationRequested?.Invoke(this, e);
            } catch (InvalidOperationException) {
            }
        }

        protected virtual void RequestCloseNotification(CloseNotificationEventArgs e) {
            CloseNotificationRequested?.Invoke(this, e);
        }

        private void TimerStart() {
            _interval.Invoke(TimeSpan.FromMilliseconds(200), OnTimerTick, _dispatcher);
        }

        private void TimerStop() {
            _interval.Stop();
        }

        private void OnTimerTick() {
            TimeSpan now = DateTimeNow.Local.TimeOfDay;

            var notificationsToRemove = _notifications
                .Where(x => {
                    return x.Value.Notification.CanClose && x.Value.CreateTime <= now;
                })
                .Select(x => x.Value)
                .ToList();

            foreach (var n in notificationsToRemove)
                CloseNotification(n.Notification);

            if (_notifications.IsEmpty)
                TimerStop();
        }

        public void ClearMessages(string msg) {
            if (string.IsNullOrWhiteSpace(msg)) {
                var notificationsToRemove = _notifications
                    .Select(x => x.Value)
                    .ToList();
                foreach (var item in notificationsToRemove) {
                    CloseNotification(item.Notification);
                }
                return;
            }
        }

        public void ClearMessages(IClearStrategy clearStrategy) {
            var notifications = clearStrategy.GetNotificationsToRemove(_notifications);
            foreach (var notification in notifications) {
                CloseNotification(notification);
            }
        }

        public event EventHandler<ShowNotificationEventArgs> ShowNotificationRequested;

        public event EventHandler<CloseNotificationEventArgs> CloseNotificationRequested;
    }

    public class CustomNotificationsList : NotificationsList {
        private int _id = 0;

        public NotificationMetaData Add(INotification notification, TimeSpan lifetime) {
            Interlocked.Increment(ref _id);
            var time = DateTimeNow.Local.TimeOfDay.Add(lifetime);
            var metaData = new NotificationMetaData(notification, _id, time);
            this[_id] = metaData;
            return metaData;
        }
    }
}