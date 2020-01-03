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
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
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
                if (((string)value).Length > 0) {
                    parameter = short.Parse((String)value);
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