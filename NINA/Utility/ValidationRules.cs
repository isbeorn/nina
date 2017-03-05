using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NINA.Utility {
    public class GreaterZeroRule : ValidationRule {        

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double dbl = 0.0d;
            Double.TryParse(value.ToString(), out dbl);
            if(dbl < 0) {
                return new ValidationResult(false, "Value must be greater than or equals 0");
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
            }
            else {
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
            }
            else if (intval > 59) {
                return new ValidationResult(false, "Value must be less than or equals 59");
            }
            else {
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
            }
            else if (doubleval >= 60) {
                return new ValidationResult(false, "Value must be less than 60");
            }
            else {
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
            }
            else if (intval > 90) {
                return new ValidationResult(false, "Value must be less than or equals 90");
            }
            else {
                return new ValidationResult(true, null);
            }
        }
    }
}
