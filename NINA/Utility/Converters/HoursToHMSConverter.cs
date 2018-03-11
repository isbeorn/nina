using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Utility.Converters
{
    class HoursToHMSConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double hours;
            if (double.TryParse(value.ToString(), out hours)) {                
                return Astrometry.Astrometry.HoursToHMS(hours);
            } else {
                return string.Empty;
            }            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
