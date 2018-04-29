using NINA.Utility;
using NINA.ViewModel;
using System;
using System.Windows;

namespace NINA {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            Logger.Error(e.Exception);

            var result = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblApplicationInBreakMode"], Locale.Loc.Instance["LblUnhandledException"], MessageBoxButton.YesNo, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes) {
                e.Handled = true;
            } else {
                var appvm = (ApplicationVM)this.Resources["AppVM"];
                try {
                    appvm.DisconnectEquipment();
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                e.Handled = true;
                Application.Current.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
        }
    }
}