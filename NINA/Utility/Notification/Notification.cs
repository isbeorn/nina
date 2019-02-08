#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Collections.Concurrent;
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
            ShowInformation(message, TimeSpan.FromSeconds(3));
        }

        public static void ShowInformation(string message, TimeSpan lifetime) {
            lock (_lock) {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    notifier.Notify<CustomNotification>(() => new CustomNotification(message));
                }));
            }
        }

        public static void ShowSuccess(string message) {
            lock (_lock) {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CheckedCircledSVG"];
                    notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol));
                }));
            }
        }

        public static void ShowWarning(string message) {
            ShowWarning(message, TimeSpan.FromSeconds(3));
        }

        public static void ShowWarning(string message, TimeSpan lifetime) {
            lock (_lock) {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ExclamationCircledSVG"];
                    var brush = (Brush)System.Windows.Application.Current.Resources["NotificationWarningBrush"];
                    var foregroundBrush = (Brush)System.Windows.Application.Current.Resources["NotificationWarningTextBrush"];
                    notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol, brush, foregroundBrush));
                }));
            }
        }

        public static void ShowError(string message) {
            lock (_lock) {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CancelCircledSVG"];
                    var brush = (Brush)System.Windows.Application.Current.Resources["NotificationErrorBrush"];
                    var foregroundBrush = (Brush)System.Windows.Application.Current.Resources["NotificationErrorTextBrush"];
                    notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol, brush, foregroundBrush, true));
                }));
            }
        }
    }

    public class CustomNotification : NotificationBase, INotifyPropertyChanged {
        private CustomDisplayPart _displayPart;

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = new CustomDisplayPart(this));

        public CustomNotification(string message, bool isNeverEnding = false) {
            Message = message;
            Color = (Brush)System.Windows.Application.Current.Resources["ButtonBackgroundBrush"];
            ForegroundColor = (Brush)System.Windows.Application.Current.Resources["ButtonForegroundBrush"];
            IsNeverEnding = isNeverEnding;
        }

        public CustomNotification(string message, Geometry symbol, bool isNeverEnding = false) {
            Message = message;
            Symbol = symbol;
            Color = (Brush)System.Windows.Application.Current.Resources["ButtonBackgroundBrush"];
            ForegroundColor = (Brush)System.Windows.Application.Current.Resources["ButtonForegroundBrush"];
            IsNeverEnding = isNeverEnding;
        }

        public CustomNotification(string message, Geometry symbol, Brush color, Brush foregroundColor, bool isNeverEnding = false) {
            Message = message;
            Symbol = symbol;
            Color = color;
            ForegroundColor = foregroundColor;
            IsNeverEnding = isNeverEnding;
        }

        public bool IsNeverEnding { get; }

        private string _message;

        public string Message {
            get {
                return _message;
            }
            set {
                _message = value;
                RaisePropertyChanged();
            }
        }

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
        private readonly TimeSpan _notificationLifetime;
        private readonly int _maximumNotificationCount;

        private Dispatcher _dispatcher;
        private CustomNotificationsList _notifications;
        private Queue<INotification> _notificationsPending;

        private IInterval _interval;

        public CustomLifetimeSupervisor() {
            _notifications = new CustomNotificationsList();

            _notificationLifetime = TimeSpan.FromSeconds(3);
            _maximumNotificationCount = 5;

            _notifications = new CustomNotificationsList();
            _interval = new Interval();
        }

        public void PushNotification(INotification notification) {
            var neverEnding = false;
            if (notification.GetType() == typeof(CustomNotification)) {
                var customNotification = (CustomNotification)notification;
                neverEnding = customNotification.IsNeverEnding;
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

            _notifications.Add(notification, neverEnding);
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
                .Where(x => x.Value.Notification.CanClose && x.Value.CreateTime + _notificationLifetime <= now)
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

            var notificationsToRemove2 = _notifications
                .Where(x => x.Value.Notification.DisplayPart.GetMessage() == msg)
                .Select(x => x.Value)
                .ToList();
            foreach (var item in notificationsToRemove2) {
                CloseNotification(item.Notification);
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

        public NotificationMetaData Add(INotification notification, bool neverEnding) {
            Interlocked.Increment(ref _id);
            var time = DateTimeNow.Local.TimeOfDay;
            if (neverEnding) { time = DateTime.MaxValue.TimeOfDay; }
            var metaData = new NotificationMetaData(notification, _id, time);
            this[_id] = metaData;
            return metaData;
        }
    }
}