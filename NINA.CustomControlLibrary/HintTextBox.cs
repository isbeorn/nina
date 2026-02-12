#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Controls;

namespace NINA.CustomControlLibrary {

    public class HintTextBox : TextBox {

        static HintTextBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HintTextBox), new FrameworkPropertyMetadata(typeof(HintTextBox)));
        }

        public static readonly DependencyProperty HintTextProperty =
           DependencyProperty.Register(nameof(HintText), typeof(string), typeof(HintTextBox), new UIPropertyMetadata(string.Empty));

        public string HintText {
            get => (string)GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }

        public static readonly DependencyProperty HintTextOpacityProperty =
           DependencyProperty.Register(nameof(HintTextOpacity), typeof(double), typeof(HintTextBox), new UIPropertyMetadata(0.4));

        public double HintTextOpacity {
            get => (double)GetValue(HintTextOpacityProperty);
            set => SetValue(HintTextOpacityProperty, value);
        }

        public static readonly DependencyProperty HintTextMarginProperty =
           DependencyProperty.Register(nameof(HintTextMargin), typeof(Thickness), typeof(HintTextBox), new UIPropertyMetadata(new Thickness(5, 0, 0, 0)));

        public Thickness HintTextMargin {
            get => (Thickness)GetValue(HintTextMarginProperty);
            set => SetValue(HintTextMarginProperty, value);
        }
    }
}