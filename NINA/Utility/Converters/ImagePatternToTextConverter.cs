#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    internal class ImagePatternToTextConverter : IValueConverter {
        private static ImagePatterns patt = null;

        private static ImagePatterns ImagePattern {
            get {
                if (patt == null) { patt = ImagePatterns.CreateExample(); }
                return patt;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var text = (string)value;

            return ImagePattern.GetImageFileString(text).Replace("\\", " › ");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
