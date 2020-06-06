#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

namespace NINA.Utility {

    public class GreaterZeroRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double dbl = 0.0d;
            Double.TryParse(value.ToString(), out dbl);
            if (dbl < 0) {
                return new ValidationResult(false, "Value must be greater than or equals 0");
            } else {
                return new ValidationResult(true, null);
            }
        }
    }

    public class GreaterThanZeroRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double dbl;
            double.TryParse(value.ToString(), out dbl);
            if (dbl <= 0) {
                return new ValidationResult(false, "Value must be greater than 0");
            } else {
                return new ValidationResult(true, null);
            }
        }
    }

    public class HoursRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            int intval = 0;
            int.TryParse(value.ToString(), out intval);
            if (intval < 0) {
                return new ValidationResult(false, "Value must be greater than or equals 0");
            } else if (intval > 24) {
                return new ValidationResult(false, "Value must be less than or equals 24");
            } else {
                return new ValidationResult(true, null);
            }
        }
    }

    public class MinutesRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            int intval = 0;
            int.TryParse(value.ToString(), out intval);
            if (intval < 0) {
                return new ValidationResult(false, "Value must be greater than or equals 0");
            } else if (intval > 59) {
                return new ValidationResult(false, "Value must be less than or equals 59");
            } else {
                return new ValidationResult(true, null);
            }
        }
    }

    public class SecondsRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double doubleval = 0.0d;
            double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out doubleval);
            if (doubleval < 0) {
                return new ValidationResult(false, "Value must be greater than or equals 0");
            } else if (doubleval >= 60) {
                return new ValidationResult(false, "Value must be less than 60");
            } else {
                return new ValidationResult(true, null);
            }
        }
    }

    public class DegreesRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            int intval = 0;
            int.TryParse(value.ToString(), out intval);
            if (intval < -90) {
                return new ValidationResult(false, "Value must be greater than or equals -90");
            } else if (intval > 90) {
                return new ValidationResult(false, "Value must be less than or equals 90");
            } else {
                return new ValidationResult(true, null);
            }
        }
    }

    public class ShortRangeRule : ValidationRule {
        private ShortRangeChecker _validRange;

        public ShortRangeChecker ValidRange {
            get { return _validRange; }
            set { _validRange = value; }
        }

        public override ValidationResult Validate(object value,
                                                   CultureInfo cultureInfo) {
            short parameter = 0;

            try {
                // using ""+value because (string)value will not work when value is int16
                if (("" + value).Length > 0) {
                    parameter = short.Parse("" + value);
                }
            } catch (Exception e) {
                return new ValidationResult(false, "Illegal characters or "
                                             + e.Message);
            }

            // allow default value
            if (((parameter < ValidRange.Minimum) || (parameter > ValidRange.Maximum)) && parameter != (short)-1) {
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + ValidRange.Minimum + " - " + ValidRange.Maximum + ".");
            }
            return new ValidationResult(true, null);
        }
    }

    public class ShortRangeChecker : DependencyObject {

        public short Minimum {
            get { return (short)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(short), typeof(ShortRangeChecker), new UIPropertyMetadata(short.MinValue));

        public short Maximum {
            get { return (short)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(short), typeof(ShortRangeChecker), new UIPropertyMetadata(short.MaxValue));
    }
}