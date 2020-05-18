#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using System.Globalization;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class GreaterThanZeroRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (double.TryParse(value.ToString(), NumberStyles.Number, cultureInfo, out var dbl)) {
                if (dbl <= 0) {
                    return new ValidationResult(false, "Value must be greater than 0");
                } else {
                    return new ValidationResult(true, null);
                }
            } else {
                return new ValidationResult(false, "Invalid value");
            }
        }
    }
}
