using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Utility.Profile;
using NINA.Utility.Enum;

namespace NINA.Model.MyPlanetarium {

    internal static class PlanetariumFactory   {

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
