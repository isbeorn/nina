using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

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