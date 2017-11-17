using NINA.Utility.Notification;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using ToastNotifications.Utilities;

namespace NINA.Utility.Notification {
    static class Notification {


        private static Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        static Notifier notifier;

        public static void Initialize() {
            notifier = new Notifier(cfg => {
                /*cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 0);*/
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 1,
                    offsetY: 40);

                cfg.LifetimeSupervisor = new CustomLifetimeSupervisor();

                cfg.Dispatcher = dispatcher;
            });
        }

        public static void ShowInformation(string message) {
            ShowInformation(message, TimeSpan.FromSeconds(3));
        }

        public static void ShowInformation(string message, TimeSpan lifetime) {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                notifier.Notify<CustomNotification>(() => new CustomNotification(message));
            }));
        }

        public static void ShowSuccess(string message) {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CheckedCircledSVG"];
                notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol));
            }));
        }

        public static void ShowWarning(string message) {
            ShowWarning(message, TimeSpan.FromSeconds(3));
        }

        public static void ShowWarning(string message, TimeSpan lifetime) {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ExclamationCircledSVG"];
                var brush = (Brush)System.Windows.Application.Current.Resources["NotificationWarningBrush"];
                notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol, brush));
            }));
        }

        public static void ShowError(string message) {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                var symbol = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CancelCircledSVG"];
                var brush = (Brush)System.Windows.Application.Current.Resources["NotificationErrorBrush"];
                notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol, brush, true));
            }));
        }
    }

    public class CustomNotification : NotificationBase, INotifyPropertyChanged {
        private CustomDisplayPart _displayPart;

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = new CustomDisplayPart(this));

        public CustomNotification(string message, bool isNeverEnding = false) {
            Message = message;
            Color = (Brush)System.Windows.Application.Current.Resources["ButtonBackgroundBrush"];
            IsNeverEnding = isNeverEnding;
        }

        public CustomNotification(string message, Geometry symbol, bool isNeverEnding = false) {
            Message = message;
            Symbol = symbol;
            Color = (Brush)System.Windows.Application.Current.Resources["ButtonBackgroundBrush"];
            IsNeverEnding = isNeverEnding;
        }

        public CustomNotification(string message, Geometry symbol, Brush color, bool isNeverEnding = false) {
            Message = message;
            Symbol = symbol;
            Color = color;
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
            if(notification.GetType() == typeof(CustomNotification)) {
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
            ShowNotificationRequested?.Invoke(this, e);
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



        public event EventHandler<ShowNotificationEventArgs> ShowNotificationRequested;
        public event EventHandler<CloseNotificationEventArgs> CloseNotificationRequested;
    }

    public class CustomNotificationsList : ConcurrentDictionary<int, NotificationMetaData> {
        private int _id = 0;

        public NotificationMetaData Add(INotification notification, bool neverEnding) {
            Interlocked.Increment(ref _id);
            var time = DateTimeNow.Local.TimeOfDay;
            if(neverEnding) { time = DateTime.MaxValue.TimeOfDay; }
            var metaData = new NotificationMetaData(notification, _id, time);
            this[_id] = metaData;
            return metaData;
        }        
    }
}
