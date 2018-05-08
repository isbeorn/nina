using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    internal class DegreesToDMSConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double deg;
            if (double.TryParse(value.ToString(), out deg)) {
                return Astrometry.Astrometry.DegreesToDMS(deg);
            } else {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}