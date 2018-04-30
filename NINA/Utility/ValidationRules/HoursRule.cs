using System.Globalization;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class HoursRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            int intval = 0;
            if (int.TryParse(value.ToString(), out intval)) {
                if (intval < 0) {
                    return new ValidationResult(false, "Value must be greater than or equals 0");
                } else if (intval > 24) {
                    return new ValidationResult(false, "Value must be less than or equals 24");
                } else {
                    return new ValidationResult(true, null);
                }
            } else {
                return new ValidationResult(false, "Invalid value");
            }
        }
    }
}