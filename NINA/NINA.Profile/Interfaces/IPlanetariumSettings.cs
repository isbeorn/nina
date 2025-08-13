#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;

namespace NINA.Profile.Interfaces {

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
        string C2AHost { get; set; }
        int C2APort { get; set; }
        string SkytechXHost { get; set; }
        int SkytechXPort { get; set; }
        PlanetariumEnum PreferredPlanetarium { get; set; }
    }
}