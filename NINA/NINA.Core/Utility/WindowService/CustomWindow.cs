#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Windows;
using System.Windows.Input;

namespace NINA.Core.Utility.WindowService {

    public class CustomWindow : Window {
        public CustomWindow() {
            FixLayout();
        }

        public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(Window), null);

        public ICommand CloseCommand {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        private void FixLayout() {
            void Window_SourceInitialized(object sender, EventArgs e) {
                this.InvalidateMeasure();
                this.SourceInitialized -= Window_SourceInitialized;
            }

            this.SourceInitialized += Window_SourceInitialized;
        }
    }
}