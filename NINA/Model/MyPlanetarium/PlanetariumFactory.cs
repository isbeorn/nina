#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Profile;
using NINA.Utility.Enum;

namespace NINA.Model.MyPlanetarium {

    internal static class PlanetariumFactory {

        /// <summary>
        /// Overrides the default planetarium in the settings
        /// </summary>
        /// <param name="profileService"></param>
        /// <param name="planetarium"></param>
        /// <returns></returns>
        public static IPlanetarium GetPlanetarium(IProfileService profileService, PlanetariumEnum planetarium) {
            switch (planetarium) {
                case PlanetariumEnum.CDC:
                    return new CartesDuCiel(profileService);

                case PlanetariumEnum.STELLARIUM:
                    return new Stellarium(profileService);

                case PlanetariumEnum.THESKYX:
                    return new TheSkyX(profileService);

                case PlanetariumEnum.HNSKY:
                    return new HNSKY(profileService);

                default:
                    return null;
            }
        }

        /// <summary>
        /// returns the default planetarium
        /// </summary>
        /// <param name="profileService"></param>
        /// <returns></returns>
        public static IPlanetarium GetPlanetarium(IProfileService profileService) {
            return GetPlanetarium(profileService,
                                 profileService.ActiveProfile.PlanetariumSettings.PreferredPlanetarium);
        }
    }
}