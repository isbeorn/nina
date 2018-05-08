using System.Globalization;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class SecondsRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double doubleval = 0.0d;
            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out doubleval)) {
                if (doubleval < 0) {
                    return new ValidationResult(false, "Value must be greater than or equals 0");
                } else if (doubleval >= 60) {
                    return new ValidationResult(false, "Value must be less than 60");
                } else {
                    return new ValidationResult(true, null);
                }
            } else {
                return new ValidationResult(false, "Invalid value");
            }
        }
    }
}