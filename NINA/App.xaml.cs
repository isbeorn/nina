#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility;
using NINA.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace NINA {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            Logger.Error(e.Exception);

            if (Application.Current != null) {
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
        }

        protected override void OnStartup(StartupEventArgs e) {
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            EventManager.RegisterClassHandler(typeof(TextBox),
                TextBox.GotFocusEvent,
                new RoutedEventHandler(TextBox_GotFocus));

            base.OnStartup(e);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            (sender as TextBox).SelectAll();
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
        }
    }
}