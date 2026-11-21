#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;

namespace NINA.Profile.Interfaces {

    public interface IAstrometrySettings : ISettings {
        double Latitude { get; set; }
        double Longitude { get; set; }
        double Elevation { get; set; }

        string HorizonFilePath { get; set; }
        CustomHorizon Horizon { get; set; }

        // The member below was added later. It has a default implementation so that existing
        // implementors of this interface (e.g. in plugins) keep compiling.
        // It is derived from Horizon, including an upper horizon embedded in it.

        /// <summary>Combination of the lower and the optional upper horizon (obstacles above, e.g. a balcony)</summary>
        SkyWindow SkyWindow => SkyWindow.From(Horizon);
        string Observer { get; set; }
        string Observatory { get; set; }
        string Site { get; set; }
    }
}