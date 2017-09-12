using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {
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
}
