#region "copyright"

/*
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

/*
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Api;
using NINA.Utility.Astrometry;
using NINA.Utility.Profile;
using System.Threading.Tasks;

namespace NINA.Model.MyPlanetarium {

    internal class HNSKY : RawTcp, IPlanetarium {
        private IProfileService profileService;

        public HNSKY(IProfileService profileService) {
            this.profileService = profileService;
            baseAddress = profileService.ActiveProfile.PlanetariumSettings.HNSKYHost;
            port = profileService.ActiveProfile.PlanetariumSettings.HNSKYPort;
            timeout = profileService.ActiveProfile.PlanetariumSettings.HNSKYTimeout;
        }

        public string Name {
            get {
                return "HNSKY";
            }
        }

        /// <summary>
        /// Get the selected object in TheSkyX
        /// </summary>
        /// <returns></returns>
        public async Task<DeepSkyObject> GetTarget() {
            DeepSkyObject ret = null;
            string response;
            string[] objinfo;

            /*
             * Connect to HNSKY
             */
            if (!IsConnected) {
                Connect();
            }

            response = await SendTextCommand("GET_TARGET");
            Logger.Debug($"HNSKY: GET_TARGET = {response}");

            if (!response.Contains("?")) {
                /*
                 * Split the coordinates and object name from the returned message.
                 * GET_TARGET returns 4 fields, space-separated:
                 * RA Dec Name Position_angle
                 *
                 * RA and Dec are in radians. Epoch is J2000.
                 */
                objinfo = response.Split(' ');

                var newCoordinates = new Coordinates(Astrometry.RadianToHour(double.Parse(objinfo[0], System.Globalization.CultureInfo.InvariantCulture)),
                                                     Astrometry.ToDegree(double.Parse(objinfo[1], System.Globalization.CultureInfo.InvariantCulture)),
                                                     Epoch.J2000, Coordinates.RAType.Hours);

                ret = new DeepSkyObject(objinfo[2], newCoordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            }

            Disconnect();
            return ret;
        }

        /// <summary>
        /// Return the configured user location from HNSKY
        /// </summary>
        /// <returns></returns>
        public async Task<Coords> GetSite() {
            Coords loc = new Coords();
            string response;
            string[] locinfo;

            /*
             * Connect to HNSKY
             * HNSKY 4.0.3a and later supports retrieving location information via the GET_LOCATION command.
             */
            if (!IsConnected) {
                Connect();
            }

            response = await SendTextCommand("GET_LOCATION");
            Logger.Debug($"HNSKY: GET_LOCATION = {response}");

            if (!response.Contains("?")) {
                /*
                 * Split the latitude and longitude from the returned message.
                 * GET_LOCATION returns 3 fields, space-separated:
                 * Latitude Longitude Julian_Date
                 *
                 * Latitude and Logitude are in radians.
                 */
                locinfo = response.Split(' ');

                /*
                 * East is negative and West is positive in HNSKY.
                 * We must flip longitude's sign here.
                 */
                loc.Latitude = Astrometry.ToDegree(double.Parse(locinfo[1], System.Globalization.CultureInfo.InvariantCulture));
                loc.Longitude = Astrometry.ToDegree(double.Parse(locinfo[0], System.Globalization.CultureInfo.InvariantCulture)) * -1;
                loc.Elevation = 0;
            }

            Disconnect();
            return loc;
        }
    }
}