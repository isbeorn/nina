using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.Utility {
    class BoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            System.Windows.Visibility result;
            if ((bool)value == true) {
                result = System.Windows.Visibility.Visible;
            } else {
                result = System.Windows.Visibility.Hidden;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            bool result;
            if((System.Windows.Visibility) value == System.Windows.Visibility.Visible) {
                result = true;
            } else {
                result = false;
            }
            return result;
        }
    }

    class InverseBoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            System.Windows.Visibility result;
            if ((bool)value != true) {
                result = System.Windows.Visibility.Visible;
            }
            else {
                result = System.Windows.Visibility.Hidden;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            bool result;
            if ((System.Windows.Visibility)value != System.Windows.Visibility.Visible) {
                result = true;
            }
            else {
                result = false;
            }
            return result;
        }
    }

    public class NullToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value == null ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityCollapsedConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class InverseNullToBooleanConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return !(value == null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class InverseNullToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value == null ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class SetAlphaToColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            if (value != null) {
                Color c = (Color)value;
                string s = (string)parameter;
                byte p;
                byte.TryParse(s, out p);
                c = Color.FromArgb(p, c.R, c.G, c.B);
                return c;
            }
            else { 
                return null;
            }


        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");
            
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }

        #endregion
    }


    /// <summary>
    /// This converter does nothing except breaking the
    /// debugger into the convert method
    /// </summary>
    public class DatabindingDebugConverter : IValueConverter {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture) {
            Debugger.Break();
            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture) {
            Debugger.Break();
            return value;
        }
    }
}
