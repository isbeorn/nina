#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment {
    public partial class HubDeviceStatusView : UserControl {
        public static readonly DependencyProperty ShowSeparatorProperty = DependencyProperty.Register(
            nameof(ShowSeparator), typeof(bool), typeof(HubDeviceStatusView), new PropertyMetadata(true));

        public HubDeviceStatusView() {
            InitializeComponent();
        }

        public bool ShowSeparator {
            get => (bool)GetValue(ShowSeparatorProperty);
            set => SetValue(ShowSeparatorProperty, value);
        }
    }
}
