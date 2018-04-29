using System.Globalization;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class DegreesRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            int intval = 0;
            if (int.TryParse(value.ToString(), out intval)) {
                if (intval < -90) {
                    return new ValidationResult(false, "Value must be greater than or equals -90");
                } else if (intval > 90) {
                    return new ValidationResult(false, "Value must be less than or equals 90");
                } else {
                    return new ValidationResult(true, null);
                }
            } else {
                return new ValidationResult(false, "Value invalid");
            }
        }
    }
}