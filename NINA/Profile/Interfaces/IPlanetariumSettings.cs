#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Enum;

namespace NINA.Profile {

    public interface IPlanetariumSettings : ISettings {
        string StellariumHost { get; set; }
        int StellariumPort { get; set; }
        string CdCHost { get; set; }
        int CdCPort { get; set; }
        string TSXHost { get; set; }
        int TSXPort { get; set; }
        bool TSXUseSelectedObject { get; set; }
        string HNSKYHost { get; set; }
        int HNSKYPort { get; set; }
        PlanetariumEnum PreferredPlanetarium { get; set; }
    }
}