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
using System.Windows.Controls;

namespace NINACustomControlLibrary {

    public class HintTextBox : TextBox {

        static HintTextBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HintTextBox), new FrameworkPropertyMetadata(typeof(HintTextBox)));
        }

        public static readonly DependencyProperty HintTextProperty =
           DependencyProperty.Register(nameof(HintText), typeof(string), typeof(HintTextBox), new UIPropertyMetadata(string.Empty));

        public string HintText {
            get {
                return (string)GetValue(HintTextProperty);
            }
            set {
                SetValue(HintTextProperty, value);
            }
        }
    }
}
