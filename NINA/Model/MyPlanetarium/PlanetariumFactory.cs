#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
