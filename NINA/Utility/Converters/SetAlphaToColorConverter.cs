using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.Utility.Converters {

    public class SetAlphaToColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                Color c = (Color)value;
                string s = (string)parameter;
                byte p;
                byte.TryParse(s, out p);
                c = Color.FromArgb(p, c.R, c.G, c.B);
                return c;
            } else {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}