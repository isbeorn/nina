using System;
using System.Collections;
using System.Windows;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    public class CollectionContainsItemsToVisibilityConverter : IValueConverter {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be Visibility");
            Visibility result;
            if (value != null && ((ICollection)value).Count > 0) {
                result = System.Windows.Visibility.Visible;
            } else {
                result = System.Windows.Visibility.Collapsed;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }

        #endregion
    }
}