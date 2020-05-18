#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;

namespace NINA.MyMessageBox {

    /// <summary>
    /// Interaction logic for MyMessageBoxView.xaml
    /// </summary>
    public partial class MyMessageBoxView : Window {

        public MyMessageBoxView() {
            InitializeComponent();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void Window_ContentRendered(object sender, System.EventArgs e) {
            InvalidateVisual();
        }
    }
}
