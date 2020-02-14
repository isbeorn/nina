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

using NINA.Utility.Astrometry;
using NINA.Utility.Exceptions;
using NINA.Profile;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using NINA.Utility;
using System.Linq;

namespace NINA.Model.MyPlanetarium {

    internal class CartesDuCiel : IPlanetarium {
        private string address;
        private int port;

        public CartesDuCiel(IProfileService profileService) {
            this.address = profileService.ActiveProfile.PlanetariumSettings.CdCHost;
            this.port = profileService.ActiveProfile.PlanetariumSettings.CdCPort;
        }

        public string Name {
            get { return "Cartes Du Ciel"; }
        }

        /// <summary>
        /// Get the selected object in CdC
        /// </summary>
        /// <returns></returns>
        public async Task<DeepSkyObject> GetTarget() {
            try {
                var response = await Query("GETSELECTEDOBJECT");

                if (!response.StartsWith("OK!")) { throw new PlanetariumObjectNotSelectedException(); }

                var columns = response.Split('\t');

                // An "OK!" response with fewer than 2 columns means that CdC is listening ok but the user has not selected an object.
                if (columns.Count() < 2) { throw new PlanetariumObjectNotSelectedException(); }

                if (!Match(columns[0].Replace("OK!", ""), @"(([0-9]{1,2})([h|:]|[?]{2})([0-9]{1,2})([m|:]|[?]{2})?([0-9]{1,2}(?:\.[0-9]+){0,1})?([s|:]|[?]{2}))", out var raString)) { throw new PlanetariumObjectNotSelectedException(); }
                var ra = Astrometry.HMSToDegrees(raString);

                if (!Match(columns[1], @"([\+|-]([0-9]{1,2})([d|°|:]|[?]{2})([0-9]{1,2})([m|'|:]|[?]{2})?([0-9]{1,2}(?:\.[0-9]+){0,1})?([s|""|:]|[?]{2}))", out var decString)) { throw new PlanetariumObjectNotSelectedException(); }
                var dec = Astrometry.DMSToDegrees(decString);

                if (!Match(columns.Last(), @"(?<=Equinox:).*", out var equinox)) { throw new PlanetariumObjectNotSelectedException(); }
                equinox = equinox.Replace("\r", "").Replace("\n", "");

                var coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), equinox.ToLower() == "now" ? Epoch.JNOW : Epoch.J2000);

                var dso = new DeepSkyObject(columns[3].Trim(), coordinates.Transform(Epoch.J2000), string.Empty);

                return dso;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        public async Task<Coords> GetSite() {
            try {
                var response = await Query("GETOBS");

                if (!response.StartsWith("OK!")) { throw new PlanetariumFailedToGetCoordinates(); }

                if (!Match(response, @"(?<=LAT:)[\+|-]([0-9]{1,2})[:|d]([0-9]{1,2})[:|m]?([0-9]{1,2}(?:\.[0-9]+){0,1})?[:|s]", out var latutideString)) { throw new PlanetariumFailedToGetCoordinates(); }

                if (!Match(response, @"(?<=LON:)[\+|-]([0-9]{1,2})[:|d]([0-9]{1,2})[:|m]?([0-9]{1,2}(?:\.[0-9]+){0,1})?[:|s]", out var longitudeString)) { throw new PlanetariumFailedToGetCoordinates(); }

                if (!Match(response, @"(?<=ALT:)([0-9]{0,5})[m]", out var altitudeString)) { throw new PlanetariumFailedToGetCoordinates(); }

                var coords = new Coords {
                    Latitude = Astrometry.DMSToDegrees(latutideString),
                    Longitude = -Astrometry.DMSToDegrees(longitudeString),
                    Elevation = Astrometry.DMSToDegrees(altitudeString)
                };

                return coords;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        private bool Match(string input, string pattern, out string result) {
            result = string.Empty;
            var regex = new Regex(pattern);
            var match = regex.Match(input);
            if (!match.Success) {
                return false;
            }
            result = match.Value;
            return true;
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
    }
}