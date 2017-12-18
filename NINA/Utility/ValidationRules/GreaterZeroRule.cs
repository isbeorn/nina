using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {
    public class GreaterZeroRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double dbl = 0.0d;
            if (Double.TryParse(value.ToString(), out dbl)) {
                if (dbl < 0) {
                    return new ValidationResult(false, "Value must be greater than or equals 0");
                } else {
                    return new ValidationResult(true, null);
                }
            } else {
                return new ValidationResult(false, "Invalid value");
            }            
        }
    }
}
