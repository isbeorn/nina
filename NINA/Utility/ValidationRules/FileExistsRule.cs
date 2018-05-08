using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class FileExistsRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            var dir = value.ToString();
            if (!File.Exists(dir)) {
                return new ValidationResult(false, "Invalid File");
            } else {
                return new ValidationResult(true, null);
            }
        }
    }
}