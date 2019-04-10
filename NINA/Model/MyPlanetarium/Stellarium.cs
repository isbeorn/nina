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

using NINA.Utility;
using NINA.Utility.Api;
using NINA.Utility.Astrometry;
using NINA.Utility.Profile;
using System;
using System.Threading.Tasks;

namespace NINA.Model.MyPlanetarium {

    internal class Stellarium : JSONApi, IPlanetarium {
        private string name;
        private IProfileService profileService;

        public Stellarium(IProfileService profileService) {
            this.profileService = profileService;
            this.rootPath = "/api";
            this.protocol = "http://";
            base.baseAddress = profileService.ActiveProfile.PlanetariumSettings.StellariumHost;
            base.port = profileService.ActiveProfile.PlanetariumSettings.StellariumPort;
            base.timeout = profileService.ActiveProfile.PlanetariumSettings.StellariumTimeout;
        }

        public string Name {
            get {
                return "Stellarium";
            }
        }

        public async Task<DeepSkyObject> GetTarget() {
            DeepSkyObject ret = null;
            dynamic fromApi;
            try {
                fromApi = await SendCommand("/main/status", null, "GET", null);
                if (fromApi != null) {
                    string searchfor = "J2000.0";//"RA / Dec(J2000.0):";
                    string[] scoords = new string[3];
                    string info = fromApi.selectioninfo.ToString();
                    int idx = info.IndexOf(searchfor);
                    if (idx < 0) {
                        return null;
                    }
                    idx += searchfor.Length + 3;
                    searchfor = "\"<br>";
                    int idx2 = info.IndexOf(searchfor, idx);
                    string test = info.Substring(idx, (idx2 - idx)).Trim();
                    idx = test.IndexOf('/');
                    scoords[0] = test.Substring(0, idx);
                    scoords[1] = test.Substring(idx + 1);
                    idx = info.IndexOf("<br", 4);
                    idx2 = info.IndexOf("</h2", 4);
                    if ((idx2 < idx) || (idx == -1)) idx = idx2;
                    scoords[2] = info.Substring(4, idx - 4);

                    var newCoordinates = new Coordinates(Astrometry.HMSToDegrees(scoords[0]), Astrometry.DMSToDegrees(scoords[1]),
                                  Epoch.J2000, Coordinates.RAType.Degrees);
                    ret = new DeepSkyObject(scoords[2], newCoordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            return ret;
        }

        public async Task<Coords> GetSite() {
            dynamic fromApi;
            try {
                fromApi = await SendCommand("/main/status", null, "GET", null);
                if (fromApi != null) {
                    Coords loc = new Coords();
                    loc.Latitude = fromApi.location.latitude;
                    loc.Longitude = fromApi.location.longitude;
                    loc.Elevation = fromApi.location.altitude;
                    return loc;
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            return null;
        }
    }
}