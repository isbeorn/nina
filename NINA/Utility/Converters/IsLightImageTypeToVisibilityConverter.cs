using NINA.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    internal class IsLightImageTypeToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return Visibility.Collapsed;
            }
            var imageType = value as string;
            if (imageType == CaptureSequence.ImageTypes.SNAPSHOT || imageType == CaptureSequence.ImageTypes.LIGHT) {
                return Visibility.Visible;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}