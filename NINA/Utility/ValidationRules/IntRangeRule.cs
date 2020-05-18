#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 
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
