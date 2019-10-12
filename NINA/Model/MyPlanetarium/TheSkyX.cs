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

using NINA.Utility.Api;
using NINA.Utility.Astrometry;
using NINA.Profile;
using System;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyPlanetarium {

    internal class TheSkyX : RawTcp, IPlanetarium {
        private IProfileService profileService;

        public TheSkyX(IProfileService profileService) {
            this.profileService = profileService;
            baseAddress = profileService.ActiveProfile.PlanetariumSettings.TSXHost;
            port = profileService.ActiveProfile.PlanetariumSettings.TSXPort;
            timeout = profileService.ActiveProfile.PlanetariumSettings.TSXTimeout;
        }

        public string Name {
            get {
                return "TheSkyX";
            }
        }

        /// <summary>
        /// Get the selected object in TheSkyX
        /// </summary>
        /// <returns></returns>
        public async Task<DeepSkyObject> GetTarget() {
            DeepSkyObject ret = null;
            StringBuilder command = new StringBuilder();
            string response;
            string[] coords;
            string[] radec;

            /*
             * Connect to TSX
             */
            if (!IsConnected) {
                Connect();
            }

            /*
             * Get J2000 coordinates of currently selected target in TheSkyX
             * Object RA is returned in hours, Dec is returned in degrees.
             * We use the primary object name provided by TSX.
             *
             * Special thanks to Kenneth Sturrock for providing examples and advice on
             * how to accomplish this with TSX.
             */
            command.AppendFormat(@"/* Java Script */", Environment.NewLine);
            command.AppendFormat(@"/* Socket Start Packet */", Environment.NewLine);
            command.AppendFormat(@"var Out = """";", Environment.NewLine);
            command.AppendFormat(@"var Target56 = 0;", Environment.NewLine);
            command.AppendFormat(@"var Target57 = 0;", Environment.NewLine);
            command.AppendFormat(@"var Name0 = """";", Environment.NewLine);
            command.AppendFormat(@"sky6ObjectInformation.Property(56);", Environment.NewLine);
            command.AppendFormat(@"Target56 = sky6ObjectInformation.ObjInfoPropOut;", Environment.NewLine);
            command.AppendFormat(@"sky6ObjectInformation.Property(57);", Environment.NewLine);
            command.AppendFormat(@"Target57 = sky6ObjectInformation.ObjInfoPropOut;", Environment.NewLine);
            command.AppendFormat(@"sky6ObjectInformation.Property(0);", Environment.NewLine);
            command.AppendFormat(@"Name0 = sky6ObjectInformation.ObjInfoPropOut;", Environment.NewLine);
            command.AppendFormat(@"Out = String(Target56) + "","" + String(Target57) + "","" + String(Name0);", Environment.NewLine);
            command.AppendFormat(@"/* Socket End Packet */", Environment.NewLine);

            response = await SendTextCommand(command.ToString());

            if (response.Contains("|No error. Error = 0")) {
                /* split the returned coordinates from the status message */
                coords = response.Split('|');

                /* put the RA, Dec, and object name into an array */
                radec = coords[0].Split(',');

                var newCoordinates = new Coordinates(double.Parse(radec[0], System.Globalization.CultureInfo.InvariantCulture),
                                                     double.Parse(radec[1], System.Globalization.CultureInfo.InvariantCulture),
                                                     Epoch.J2000, Coordinates.RAType.Hours);

                ret = new DeepSkyObject(radec[2], newCoordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            }

            Disconnect();
            return ret;
        }

        public async Task<Coords> GetSite() {
            Coords loc = new Coords();
            StringBuilder command = new StringBuilder();
            string response;
            string[] rsp;
            string[] coords;

            if (!IsConnected) {
                Connect();
            }

            /*
             * Get the location (latitude, longitude, elevation) that is currently set
             * in TSX. Elevation is in meters.
             */
            command.AppendFormat(@"/* Java Script */", Environment.NewLine);
            command.AppendFormat(@"/* Socket Start Packet */", Environment.NewLine);
            command.AppendFormat(@"var Out = """";", Environment.NewLine);
            command.AppendFormat(@"var Lat = 0;", Environment.NewLine);
            command.AppendFormat(@"var Long = 0;", Environment.NewLine);
            command.AppendFormat(@"var Elevation = 0;", Environment.NewLine);
            command.AppendFormat(@"sky6StarChart.DocumentProperty(0);", Environment.NewLine);
            command.AppendFormat(@"Lat = sky6StarChart.DocPropOut;", Environment.NewLine);
            command.AppendFormat(@"sky6StarChart.DocumentProperty(1);", Environment.NewLine);
            command.AppendFormat(@"Long = sky6StarChart.DocPropOut;", Environment.NewLine);
            command.AppendFormat(@"sky6StarChart.DocumentProperty(3);", Environment.NewLine);
            command.AppendFormat(@"Elevation = sky6StarChart.DocPropOut;", Environment.NewLine);
            command.AppendFormat(@"Out = String(Lat) + "","" + String(Long) + "","" + String(Elevation);", Environment.NewLine);
            command.AppendFormat(@"/* Socket End Packet */", Environment.NewLine);

            response = await SendTextCommand(command.ToString());

            if (response.Contains("|No error. Error = 0")) {
                /* split the returned coordinates from the status message */
                rsp = response.Split('|');

                /* put the RA, Dec, and object name into an array */
                coords = rsp[0].Split(',');

                /*
                 * East is negative and West is positive in TheSkyX.
                 * We must flip longitude's sign here.
                 */
                loc.Latitude = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                loc.Longitude = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture) * -1;
                loc.Elevation = double.Parse(coords[2], System.Globalization.CultureInfo.InvariantCulture);
            }

            Disconnect();
            return loc;
        }
    }
}