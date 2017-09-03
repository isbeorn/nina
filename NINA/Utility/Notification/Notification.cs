using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace NINA.Utility.Notification {
    static class Notification {

        
        private static Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        static Notifier notifier = new Notifier(cfg =>
        {
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
            notifier.Notify<CustomNotification>(() => new CustomNotification(message));
            /*
            var options = new ToastNotifications.Core.MessageOptions();
            options.FreezeOnMouseEnter = false;
            notifier.ShowInformation(message, options);*/
        }

        public static void ShowSuccess(string message) {
            notifier.Notify<CustomNotification>(() => new CustomNotification(message));
            /*var options = new ToastNotifications.Core.MessageOptions();
            options.FreezeOnMouseEnter = false; 
            notifier.ShowSuccess(message, options);*/
        }

        public static void ShowWarning(string message) {
            ShowWarning(message, TimeSpan.FromSeconds(3));
        }

        public static void ShowWarning(string message, TimeSpan lifetime) {
            notifier.ShowWarning(message);
        }

        public static void ShowError(string message) {
            notifier.ShowError(message);            
        }
    }

    public class CustomNotification : NotificationBase, INotifyPropertyChanged {
        private CustomDisplayPart _displayPart;

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = new CustomDisplayPart(this));

        public CustomNotification(string message) {
            Message = message;
        }

        private string _message;
        public string Message {
            get {
                return _message;
            }
            set {
                _message = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null) {
            var handler = PropertyChanged;
            if (handler != null)
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
