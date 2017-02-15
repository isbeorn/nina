using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ToastNotifications;

namespace NINA.Utility {
    static class Notification {

        private static NotificationsSource _notificationSource;

        private static Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        public static NotificationsSource NotificationSource {
            get {
                if (_notificationSource == null) {
                    _notificationSource = new NotificationsSource {
                        MaximumNotificationCount = 1                       
                    };
                }
                return _notificationSource;
            }
            set {
                _notificationSource = value;
                if (NotificationChanged != null)
                    NotificationChanged(null, EventArgs.Empty);
            }
        }

        public static event EventHandler NotificationChanged;

        public static void ShowInformation(string message) {
            Show(message, TimeSpan.FromSeconds(3), NotificationType.Information);            
        }

        public static void ShowInformation(string message, TimeSpan lifetime) {
            Show(message, lifetime, NotificationType.Information);
        }

        public static void ShowSuccess(string message) {
            Show(message, TimeSpan.FromSeconds(3), NotificationType.Success);
        }

        public static void ShowWarning(string message) {
            Show(message, TimeSpan.FromSeconds(3), NotificationType.Warning);
        }

        public static void ShowWarning(string message, TimeSpan lifetime) {
            Show(message, lifetime, NotificationType.Warning);
        }

        public static void ShowError(string message) {
            Show(message, NotificationsSource.NeverEndingNotification, NotificationType.Error);
        }


        private static void Show(string message, TimeSpan lifetime, NotificationType type) {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                NotificationSource.NotificationLifeTime = lifetime;
                NotificationSource.Show(message, type);
            }));
            
        }

    }
}
