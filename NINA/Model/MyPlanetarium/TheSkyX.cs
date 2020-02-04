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
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Exceptions;
using NINA.Profile;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace NINA.Model.MyPlanetarium {

    internal class TheSkyX : IPlanetarium {
        private string address;
        private int port;
        private bool useSelectedObject;

        public TheSkyX(IProfileService profileService) {
            this.address = profileService.ActiveProfile.PlanetariumSettings.TSXHost;
            this.port = profileService.ActiveProfile.PlanetariumSettings.TSXPort;
            this.useSelectedObject = profileService.ActiveProfile.PlanetariumSettings.TSXUseSelectedObject;
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
            try {
                string[] raDecName = null;

                if (useSelectedObject) {
                    raDecName = await GetSelectedObject();

                    // RA and Dec of 0 and an empty string for object name means that there is no object selected in TSX
                    if (raDecName[0] == "0" && raDecName[1] == "0" && string.IsNullOrEmpty(raDecName[2])) {
                        throw new PlanetariumObjectNotSelectedException();
                    }
                } else {
                    raDecName = await GetSkyChartCenter();
                }

                if (raDecName == null) {
                    throw new PlanetariumFailedToGetCoordinates();
                }

                var newCoordinates = new Coordinates(double.Parse(raDecName[0], System.Globalization.CultureInfo.InvariantCulture),
                                                     double.Parse(raDecName[1], System.Globalization.CultureInfo.InvariantCulture),
                                                     Epoch.J2000, Coordinates.RAType.Hours);

                return new DeepSkyObject(raDecName[2], newCoordinates, string.Empty);
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        public async Task<Coords> GetSite() {
            try {
                Coords loc = new Coords();
                StringBuilder command = new StringBuilder();
                string[] coords;

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

                string query = await Query(command.ToString());
                string[] response = query.Split('|');

                if (response[1].Equals("No error. Error = 0.")) {
                    // put the RA, Dec, and elevation into an array
                    coords = response[0].Split(',');

                    /*
                     * East is negative and West is positive in TheSkyX.
                     * We must flip longitude's sign here.
                     */
                    loc.Latitude = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                    loc.Longitude = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture) * -1;
                    loc.Elevation = double.Parse(coords[2], System.Globalization.CultureInfo.InvariantCulture);
                } else {
                    throw new PlanetariumFailedToGetCoordinates();
                }
                return loc;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        private async Task<string> Query(string command) {
            using (var client = new TcpClient()) {
                try {
                    await Task.Factory.FromAsync((callback, stateObject) => client.BeginConnect(this.address, this.port, callback, stateObject), client.EndConnect, TaskCreationOptions.RunContinuationsAsynchronously);
                } catch (Exception ex) {
                    throw new PlanetariumFailedToConnect($"{address}:{port}: {ex.ToString()}");
                }

                byte[] data = Encoding.ASCII.GetBytes($"{command}\r\n");
                var stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                byte[] buffer = new byte[2048];
                var length = stream.Read(buffer, 0, buffer.Length);
                string response = System.Text.Encoding.ASCII.GetString(buffer, 0, length);

                stream.Close();
                client.Close();

                Logger.Trace($"{Name} - Received Message {response}");

                return response;
            }
        }

        private async Task<string[]> GetSelectedObject() {
            StringBuilder command = new StringBuilder();
            string[] raDecName;

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

            string query = await Query(command.ToString());

            // split the returned coordinates from the status message
            string[] response = query.Split('|');

            if (response[1].Equals("No error. Error = 0.")) {
                // put the RA, Dec, and object name into an array
                raDecName = response[0].Split(',');
            } else {
                throw new PlanetariumFailedToGetCoordinates();
            }

            return raDecName;
        }

        private async Task<string[]> GetSkyChartCenter() {
            StringBuilder command = new StringBuilder();
            string[] raDecName = new string[3];

            /*
             * Get J2000 coordinates of the center of the TSX star chart.
             * There is no way to get an object name, so we set that to an empty string.
            */
            command.AppendFormat(@"/* Java Script */", Environment.NewLine);
            command.AppendFormat(@"/* Socket Start Packet */", Environment.NewLine);
            command.AppendFormat(@"var Out = """";", Environment.NewLine);
            command.AppendFormat(@"var chartRA = 0;", Environment.NewLine);
            command.AppendFormat(@"var chartDec = 0;", Environment.NewLine);
            command.AppendFormat(@"chartRA = sky6StarChart.RightAscension;", Environment.NewLine);
            command.AppendFormat(@"chartDec = sky6StarChart.Declination;", Environment.NewLine);
            command.AppendFormat(@"Out = String(chartRA) + "","" + String(chartDec);", Environment.NewLine);
            command.AppendFormat(@"/* Socket End Packet */", Environment.NewLine);

            string query = await Query(command.ToString());

            // split the returned coordinates from the status message
            string[] response = query.Split('|');

            if (response[1].Equals("No error. Error = 0.")) {
                // put the RA, Dec, and object name into an array
                string[] raDec = response[0].Split(',');
                raDecName[0] = raDec[0];
                raDecName[1] = raDec[1];
                raDecName[2] = string.Empty;
            } else {
                throw new PlanetariumFailedToGetCoordinates();
            }

            return raDecName;
        }
    }
}