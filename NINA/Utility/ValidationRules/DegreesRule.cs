#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Globalization;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class DegreesRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (int.TryParse(value.ToString(), NumberStyles.Integer, cultureInfo, out var intval)) {
                if (intval < -90) {
                    return new ValidationResult(false, "Value must be greater than or equals -90");
                } else if (intval > 90) {
                    return new ValidationResult(false, "Value must be less than or equals 90");
                } else {
                    return new ValidationResult(true, null);
                }
            } else {
                return new ValidationResult(false, "Value invalid");
            }
        }
    }
}
