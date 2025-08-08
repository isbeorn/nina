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
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;

namespace NINA.Equipment.Equipment.MyPlanetarium {

    public class PlanetariumFactory : IPlanetariumFactory {

        public PlanetariumFactory(IProfileService profileService) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        /// <summary>
        /// Overrides the default planetarium in the settings
        /// </summary>
        /// <param name="profileService"></param>
        /// <param name="planetarium"></param>
        /// <returns></returns>
        public IPlanetarium GetPlanetarium(PlanetariumEnum planetarium) {
            switch (planetarium) {
                case PlanetariumEnum.CDC:
                    return new CartesDuCiel(profileService);

                case PlanetariumEnum.STELLARIUM:
                    return new Stellarium(profileService);

                case PlanetariumEnum.THESKYX:
                    return new TheSkyX(profileService);

                case PlanetariumEnum.HNSKY:
                    return new HNSKY(profileService);

                case PlanetariumEnum.C2A:
                    return new C2A(profileService);

                case PlanetariumEnum.SKYTECHX:
                    return new SkytechX(profileService);

                default:
                    return null;
            }
        }

        /// <summary>
        /// returns the default planetarium
        /// </summary>
        /// <returns></returns>
        public IPlanetarium GetPlanetarium() {
            return GetPlanetarium(profileService.ActiveProfile.PlanetariumSettings.PreferredPlanetarium);
        }
    }
}