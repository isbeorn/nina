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

using NINA.Utility.Enum;

namespace NINA.Utility.Profile {

    public interface IPlanetariumSettings : ISettings {
        string StellariumHost { get; set; }
        int StellariumPort { get; set; }
        int StellariumTimeout { get; set; }
        string CdCHost { get; set; }
        int CdCPort { get; set; }
        int CdCTimeout { get; set; }
        string TSXHost { get; set; }
        int TSXPort { get; set; }
        int TSXTimeout { get; set; }
        string HNSKYHost { get; set; }
        int HNSKYPort { get; set; }
        int HNSKYTimeout { get; set; }
        PlanetariumEnum PreferredPlanetarium { get; set; }
    }
}