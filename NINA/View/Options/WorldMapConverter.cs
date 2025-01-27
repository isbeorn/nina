using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NINA.View.Options {
    public class LongitudeWorldMapConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if(values.Length < 3) return null;
            if(values.Any(x => x == DependencyProperty.UnsetValue)) return null;

            var longitude = (double)values[0];
            var canvasWidth = (double)values[1];
            var selfWidth = (double)values[2];

            var x = (longitude + 180d) * (canvasWidth / 360d);

            return x - selfWidth / 2d;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    public class LatitudeWorldMapConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 3) return null;
            if (values.Any(x => x == DependencyProperty.UnsetValue)) return null;

            var latitude = (double)values[0];
            var canvasHeight = (double)values[1];
            var selfHeight = (double)values[2];

            var y = (90 - latitude) * (canvasHeight / 180d);            
            return y - selfHeight / 2d;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
