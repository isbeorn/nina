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

using NINA.Utility.Api;
using NINA.Utility.Astrometry;
using NINA.Utility.Profile;
using System;
using System.Threading.Tasks;

namespace NINA.Model.MyPlanetarium {

    internal class CartesDuCiel : RawTcp, IPlanetarium {
        private IProfileService profileService;

        public CartesDuCiel(IProfileService profileService) {
            this.profileService = profileService;
            baseAddress = profileService.ActiveProfile.PlanetariumSettings.CdCHost;
            port = profileService.ActiveProfile.PlanetariumSettings.CdCPort;
            timeout = profileService.ActiveProfile.PlanetariumSettings.CdCTimeout;
        }

        public string Name {
            get { return "Cartes Du Ciel"; }
        }

        /// <summary>
        /// Get the selected object in CdC
        /// </summary>
        /// <returns></returns>
        public async Task<DeepSkyObject> GetTarget() {
            if (!this.IsConnected) this.Connect();
            DeepSkyObject ret;
            string response = await SendTextCommand("GETSELECTEDOBJECT\r\n");
            if (response == "") return null;
            else {
                int idx = response.IndexOf("OK!");
                if (idx < 0) return null;
                response = response.Substring(idx + 3).Trim();
                idx = response.IndexOf('\t');
                if (idx < 0) return null;
                string[] scoords = new string[3];
                scoords[0] = response.Substring(0, idx);
                response = response.Substring(idx + 1);
                idx = response.IndexOf('\t');
                if (idx < 0) return null;
                scoords[1] = response.Substring(0, idx);
                response = response.Substring(idx + 1);
                idx = response.IndexOf('\t');
                if (idx > -1) {
                    response = response.Substring(idx + 1);
                    idx = response.IndexOf('\t');
                    scoords[2] = response.Substring(0, idx).Trim();
                }
                var newCoordinates = new Coordinates(Astrometry.HMSToDegrees(scoords[0]), Astrometry.DMSToDegrees(scoords[1]),
                                  Epoch.J2000, Coordinates.RAType.Degrees);
                ret = new DeepSkyObject(scoords[2], newCoordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            }

            this.Disconnect();
            return ret;
        }

        public async Task<Coords> GetSite() {
            if (!this.IsConnected) this.Connect();
            string response = await SendTextCommand("GETOBS\r\n");
            Coords loc;
            if (response == "") return null;
            else {
                int idx = response.IndexOf("OK!");
                if (idx < 0) return null;

                response = response.Substring(idx + 3).Trim();
                idx = response.IndexOf("LAT:");
                if (idx < 0) return null;
                idx += 4;
                response = response.Substring(idx);
                idx = response.IndexOf("LON:");
                if (idx < 0) return null;
                string lat = response.Substring(0, idx).Trim();
                idx += 4;
                response = response.Substring(idx);
                idx = response.IndexOf("ALT:");
                if (idx < 0) return null;
                string lon = response.Substring(0, idx).Trim();
                idx += 4;
                response = response.Substring(idx);
                idx = response.IndexOf("OBS:");
                string alt = "0";
                if (idx >= 0)
                    alt = response.Substring(0, idx).Trim();
                if ((alt.Length > 0) && (alt.Substring(alt.Length - 1) == "m"))
                    alt = alt.Substring(0, alt.Length - 1);
                loc = new Coords();

                loc.Latitude = Astrometry.DMSToDegrees(lat);
                loc.Longitude = Astrometry.DMSToDegrees(lon);
                double elev;
                if (Double.TryParse(alt, out elev))
                    loc.Elevation = elev;
                else loc.Elevation = 0;
            }
            this.Disconnect();
            return loc;
        }
    }
}