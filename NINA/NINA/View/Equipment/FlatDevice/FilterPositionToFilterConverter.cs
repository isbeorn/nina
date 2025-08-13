using NINA.Core.Model.Equipment;
using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using NINA.Profile.Interfaces;

namespace NINA.View.Equipment {
    public class FilterPositionToFilterConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var number = System.Convert.ToInt32(value);
            if (number == -1) {
                return NullFilter.Instance;
            }
            if(parameter is IProfileService p) {
                var f = p.ActiveProfile.FilterWheelSettings.FilterWheelFilters.FirstOrDefault(x => x.Position == number);
                if(f != null) { return f; }
            }
            return NullFilter.Instance;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is NullFilter) {
                return -1;
            }
            if (value is FilterInfo f) {
                return f.Position;
            }
            return -1;
        }
    }
}
