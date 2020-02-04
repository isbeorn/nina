#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    public class InverseBooleanToVisibilityCollapsedConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            System.Windows.Visibility result;
            if ((bool)value != true) {
                result = System.Windows.Visibility.Visible;
            } else {
                result = System.Windows.Visibility.Collapsed;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            bool result;
            if ((System.Windows.Visibility)value == System.Windows.Visibility.Visible) {
                result = false;
            } else {
                result = true;
            }
            return result;
        }
    }
}