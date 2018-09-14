using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    internal class PercentageConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (string.IsNullOrEmpty(value.ToString())) return 0;

            if (value.GetType() == typeof(double)) return (double)value * 100;

            if (value.GetType() == typeof(decimal)) return (decimal)value * 100;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (string.IsNullOrEmpty(value.ToString())) return 0;

            var trimmedValue = value.ToString().TrimEnd(new char[] { '%' });

            if (targetType == typeof(double)) {
                double result;
                if (double.TryParse(trimmedValue, out result))
                    return result / 100d;
                else
                    return value;
            }

            if (targetType == typeof(decimal)) {
                decimal result;
                if (decimal.TryParse(trimmedValue, out result))
                    return result / 100m;
                else
                    return value;
            }
            return value;
        }
    }
}