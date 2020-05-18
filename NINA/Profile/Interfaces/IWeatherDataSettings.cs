#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

namespace NINA.Profile {

    public interface IWeatherDataSettings : ISettings {
        string Id { get; set; }
        bool DisplayFahrenheit { get; set; }
        bool DisplayImperial { get; set; }
        string OpenWeatherMapAPIKey { get; set; }
    }
}
