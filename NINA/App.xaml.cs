#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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