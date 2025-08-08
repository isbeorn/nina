using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Astrometry.Converters {
    public class ReverseAngleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double dbl) {
                return Math.Round(AstroUtil.EuclidianModulus(360 - dbl, 360), 2);
            }
            if (value is float flt) {
                return Math.Round(AstroUtil.EuclidianModulus(360 - flt, 360), 2);
            }
            if (value is int integer) {
                return Math.Round(AstroUtil.EuclidianModulus(360 - integer, 360),2);
            }
            return double.NaN;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double dbl) {
                return Math.Round(AstroUtil.EuclidianModulus(360 - dbl, 360), 2);
            }
            if (value is float flt) {
                return Math.Round(AstroUtil.EuclidianModulus(360 - flt, 360), 2);
            }
            if (value is int integer) {
                return Math.Round(AstroUtil.EuclidianModulus(360 - integer, 360), 2);
            }
            if (value is string str) {
                if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl2)) {
                    return Math.Round(AstroUtil.EuclidianModulus(360 - dbl2, 360), 2);
                }
            }
            return value;
        }
    }
}
