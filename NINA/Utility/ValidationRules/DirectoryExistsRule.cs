using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class DirectoryExistsRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            var dir = value.ToString();
            if (!Directory.Exists(dir)) {
                return new ValidationResult(false, "Invalid Directory");
            } else {
                return new ValidationResult(true, null);
            }
        }
    }
}