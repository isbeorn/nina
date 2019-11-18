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
using NINA.Utility.Astrometry;
using NINA.Profile;
using System.Threading.Tasks;
using System.Text;
using System;
using System.Net.Sockets;

namespace NINA.Model.MyPlanetarium {

    internal class HNSKY : IPlanetarium {
        private string address;
        private int port;

        public HNSKY(IProfileService profileService) {
            this.address = profileService.ActiveProfile.PlanetariumSettings.HNSKYHost;
            this.port = profileService.ActiveProfile.PlanetariumSettings.HNSKYPort;
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
            try {
                var response = await Query("GET_TARGET");

                if (!response.Contains("?")) {
                    /*
                     * Split the coordinates and object name from the returned message.
                     * GET_TARGET returns 4 fields, space-separated:
                     * RA Dec Name Position_angle
                     *
                     * RA and Dec are in radians. Epoch is J2000.
                     */
                    var info = response.Split(' ');

                    var newCoordinates = new Coordinates(Astrometry.RadianToHour(double.Parse(info[0].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture)),
                                                         Astrometry.ToDegree(double.Parse(info[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture)),
                                                         Epoch.J2000, Coordinates.RAType.Hours);

                    var dso = new DeepSkyObject(info[2], newCoordinates, string.Empty);
                    return dso;
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            return null;
        }

        /// <summary>
        /// Return the configured user location from HNSKY
        /// </summary>
        /// <returns></returns>
        public async Task<Coords> GetSite() {
            try {
                var response = await Query("GET_LOCATION");

                if (!response.Contains("?")) {
                    /*
                     * Split the latitude and longitude from the returned message.
                     * GET_LOCATION returns 3 fields, space-separated:
                     * Latitude Longitude Julian_Date
                     *
                     * Latitude and Logitude are in radians.
                     */
                    var info = response.Split(' ');

                    /*
                     * East is negative and West is positive in HNSKY.
                     * We must flip longitude's sign here.
                     */
                    var loc = new Coords();
                    loc.Latitude = Astrometry.ToDegree(double.Parse(info[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture));
                    loc.Longitude = Astrometry.ToDegree(double.Parse(info[0].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture)) * -1;
                    loc.Elevation = 0;
                    return loc;
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            return null;
            //Coords loc = new Coords();
            //string response;
            //string[] locinfo;

            ///*
            // * Connect to HNSKY
            // * HNSKY 4.0.3a and later supports retrieving location information via the GET_LOCATION command.
            // */
            //if (!IsConnected) {
            //    Connect();
            //}

            //response = await SendTextCommand("GET_LOCATION");
            //Logger.Debug($"HNSKY: GET_LOCATION = {response}");

            //if (!response.Contains("?")) {
            //    /*
            //     * Split the latitude and longitude from the returned message.
            //     * GET_LOCATION returns 3 fields, space-separated:
            //     * Latitude Longitude Julian_Date
            //     *
            //     * Latitude and Logitude are in radians.
            //     */
            //    locinfo = response.Split(' ');

            //    /*
            //     * East is negative and West is positive in HNSKY.
            //     * We must flip longitude's sign here.
            //     */
            //    loc.Latitude = Astrometry.ToDegree(double.Parse(locinfo[1], System.Globalization.CultureInfo.InvariantCulture));
            //    loc.Longitude = Astrometry.ToDegree(double.Parse(locinfo[0], System.Globalization.CultureInfo.InvariantCulture)) * -1;
            //    loc.Elevation = 0;
            //}

            //Disconnect();
            //return loc;
        }

        private async Task<string> Query(string command) {
            using (var client = new TcpClient()) {
                await Task.Factory.FromAsync((callback, stateObject) => client.BeginConnect(this.address, this.port, callback, stateObject), client.EndConnect, TaskCreationOptions.RunContinuationsAsynchronously);

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
    }
}