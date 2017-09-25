using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace NINA.Utility.Notification {
    static class Notification {


        private static Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        static Notifier notifier = new Notifier(cfg => {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.BottomRight,
                offsetX: 10,
                offsetY: 0);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            cfg.Dispatcher = dispatcher;
        });

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
                notifier.Notify<CustomNotification>(() => new CustomNotification(message, symbol,brush));
            }));
        }
    }

    public class CustomNotification : NotificationBase, INotifyPropertyChanged {
        private CustomDisplayPart _displayPart;

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = new CustomDisplayPart(this));

        public CustomNotification(string message) {
            Message = message;
            Color = (Brush)System.Windows.Application.Current.Resources["ButtonBackgroundBrush"];
        }

        public CustomNotification(string message, Geometry symbol) {
            Message = message;
            Symbol = symbol;
            Color = (Brush)System.Windows.Application.Current.Resources["ButtonBackgroundBrush"];
        }

        public CustomNotification(string message, Geometry symbol, Brush color) {
            Message = message;
            Symbol = symbol;
            Color = color;
        }

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
}
