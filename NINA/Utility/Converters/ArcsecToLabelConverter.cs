using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Utility.Converters {
    public class ArcsecToLabelConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return null;
            }
            var arcsecs = (double)value;
            if (arcsecs > 3600) {
                return Astrometry.Astrometry.ArcsecToDegree(arcsecs).ToString("0.00", CultureInfo.InvariantCulture) + "°";
            } else if (arcsecs > 60) {
                return Astrometry.Astrometry.ArcsecToArcmin(arcsecs).ToString("0.00", CultureInfo.InvariantCulture) + "'";
            } else {
                return arcsecs.ToString("0.00", CultureInfo.InvariantCulture) + "''";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
