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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Core.Utility.ValidationRules {

    public class DoubleRangeRule : ValidationRule {
        public DoubleRangeChecker ValidRange { get; set; }

        public override ValidationResult Validate(object value,
                                                   CultureInfo cultureInfo) {
            double parameter = 0;

            try {
                if (("" + value).Length > 0) {
                    parameter = double.Parse(value.ToString(), NumberStyles.Number, cultureInfo);
                }
            } catch (Exception e) {
                return new ValidationResult(false, "Illegal characters or "
                                             + e.Message);
            }

            if (((parameter < ValidRange.Minimum) || (parameter > ValidRange.Maximum)) && parameter != (double)-1) {
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + ValidRange.Minimum + " - " + ValidRange.Maximum + ".");
            }
            return new ValidationResult(true, null);
        }
    }

    public class DoubleRangeChecker : DependencyObject {

        public double Minimum {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(DoubleRangeChecker), new UIPropertyMetadata(double.MinValue));

        public double Maximum {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(DoubleRangeChecker), new UIPropertyMetadata(double.MaxValue));
    }
}