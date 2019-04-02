#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Utility {

    public class ButtonHelper {

        // Boilerplate code to register attached property "bool? DialogResult"
        public static bool? GetDialogResult(DependencyObject obj) { return (bool?)obj.GetValue(DialogResultProperty); }

        public static void SetDialogResult(DependencyObject obj, bool? value) {
            obj.SetValue(DialogResultProperty, value);
        }

        public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(ButtonHelper), new UIPropertyMetadata {
            PropertyChangedCallback = (obj, e) => {
                // Implementation of DialogResult functionality
                Button button = obj as Button;
                if (button == null)
                    throw new InvalidOperationException(
                      "Can only use ButtonHelper.DialogResult on a Button control");
                button.Click += (sender, e2) => {
                    Window.GetWindow(button).DialogResult = GetDialogResult(button);
                    Window.GetWindow(button).Close();
                };
            }
        });
    }
}