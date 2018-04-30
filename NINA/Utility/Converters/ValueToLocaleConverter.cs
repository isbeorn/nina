using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    internal class ValueToLocaleConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return null;
            }
            var s = value.ToString();
            if (string.IsNullOrWhiteSpace(s)) {
                return string.Empty;
            }
            s = parameter + s;
            return Locale.Loc.Instance[s];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}