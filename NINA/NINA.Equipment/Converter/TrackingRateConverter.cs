#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Equipment.Converter {

    public class TrackingRateConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || !(value is TrackingRate)) {
                return null;
            }

            var trackingRate = (TrackingRate)value;
            if (trackingRate.TrackingMode == TrackingMode.Custom) {
                return String.Format(
                    Loc.Instance["LblTrackingCustomRate"],
                    trackingRate.CustomDeclinationRate.GetValueOrDefault(0.0),
                    trackingRate.CustomRightAscensionRate.GetValueOrDefault(0.0));
            }

            return TrackingModeConverter.TrackingModeToLocalizedString(trackingRate.TrackingMode);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}