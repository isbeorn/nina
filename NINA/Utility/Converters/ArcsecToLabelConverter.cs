#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
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
