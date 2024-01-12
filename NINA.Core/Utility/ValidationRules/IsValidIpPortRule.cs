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
using System;
using System.Globalization;
using System.Net;
using System.Windows.Controls;

namespace NINA.Core.Utility.ValidationRules {

    public class IsValidIpPortRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            try {
                int port = int.Parse(value.ToString());

                if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                    throw new FormatException();
                } else {
                    return new ValidationResult(true, null);
                }
            } catch {
                return new ValidationResult(false, string.Format(Loc.Instance["LblIsValidIpPortRule"], IPEndPoint.MinPort, IPEndPoint.MaxPort));
            }
        }
    }
}