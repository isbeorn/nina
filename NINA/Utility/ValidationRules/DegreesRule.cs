#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System.Globalization;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {

    public class DegreesRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            int intval = 0;
            if (int.TryParse(value.ToString(), out intval)) {
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