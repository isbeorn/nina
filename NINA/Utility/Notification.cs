using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace NINA.Utility {
    static class Notification {

        
        private static Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        static Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 10,
                offsetY: 40);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            cfg.Dispatcher = dispatcher;
        });

        public static void Initialize() {

        }

        public static void ShowInformation(string message) {
            notifier.ShowInformation(message);         
        }

        public static void ShowInformation(string message, TimeSpan lifetime) {
            notifier.ShowInformation(message);
        }

        public static void ShowSuccess(string message) {
            notifier.ShowSuccess(message);
        }

        public static void ShowWarning(string message) {
            notifier.ShowWarning(message);
        }

        public static void ShowWarning(string message, TimeSpan lifetime) {
            notifier.ShowWarning(message);
        }

        public static void ShowError(string message) {
            notifier.ShowError(message);            
        }

        

    }
}
