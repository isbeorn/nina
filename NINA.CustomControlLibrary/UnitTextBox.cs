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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.CustomControlLibrary {

    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_SecondTextBox", Type = typeof(TextBox))]
    public class UnitTextBox : TextBox {

        static UnitTextBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UnitTextBox), new FrameworkPropertyMetadata(typeof(UnitTextBox)));
        }

        public static readonly DependencyProperty UnitTextProperty =
           DependencyProperty.Register(nameof(Unit), typeof(string), typeof(UnitTextBox), new UIPropertyMetadata(string.Empty));

        public string Unit {
            get => (string)GetValue(UnitTextProperty);
            set => SetValue(UnitTextProperty, value);
        }

        public void SecondTextBox_Click(object sender, EventArgs e) {
            var textBox = GetTemplateChild("PART_TextBox") as TextBox;
            textBox.SelectAll();
            textBox.Focus();
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            var secondTextBox = GetTemplateChild("PART_SecondTextBox") as TextBox;
            if (secondTextBox != null) {
                secondTextBox.GotFocus += SecondTextBox_Click;
            }
        }
    }
}