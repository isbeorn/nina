#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility.WindowService;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NINA.View.Equipment {

    public partial class HubConfigurationView : UserControl {

        public HubConfigurationView() {
            InitializeComponent();
            KeyDown += CloseOnEscape;
        }

        private void CloseOnEscape(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape && Window.GetWindow(this) is CustomWindow window) {
                window.CloseCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
