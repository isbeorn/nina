using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
