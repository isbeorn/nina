#region "copyright"

/*
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

/*
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>
 * Copyright 2020 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class IntRangeRule : ValidationRule {
        public IntRangeChecker ValidRange { get; set; }

        public override ValidationResult Validate(object value,
                                                   CultureInfo cultureInfo) {
            int parameter = 0;

            try {
                if (value.ToString().Length > 0) {
                    parameter = int.Parse(value.ToString(), NumberStyles.Integer, cultureInfo);
                }
            } catch (Exception e) {
                return new ValidationResult(false, "Illegal characters or "
                                             + e.Message);
            }

            if ((parameter < ValidRange.Minimum) || (parameter > ValidRange.Maximum)) {
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + ValidRange.Minimum + " - " + ValidRange.Maximum + ".");
            }

            return new ValidationResult(true, null);
        }
    }

    public class IntRangeChecker : DependencyObject {

        public int Minimum {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(IntRangeChecker), new UIPropertyMetadata(int.MinValue));

        public int Maximum {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(IntRangeChecker), new UIPropertyMetadata(int.MaxValue));
    }
}