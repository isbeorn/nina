using System.Windows;
using ToastNotifications.Core;

namespace NINA.Utility.Notification {

    /// <summary>
    /// Interaction logic for CustomDisplayPart.xaml
    /// </summary>
    public partial class CustomDisplayPart : NotificationDisplayPart {
        private CustomNotification _customNotification;

        public CustomDisplayPart(CustomNotification customNotification) {
            _customNotification = customNotification;
            DataContext = customNotification; // this allows to bind ui with data in notification
            InitializeComponent();
        }

        private void OnClose(object sender, RoutedEventArgs e) {
            _customNotification.Close();
        }
    }
}