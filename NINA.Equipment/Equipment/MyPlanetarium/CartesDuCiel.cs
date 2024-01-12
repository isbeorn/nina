#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Utility.TcpRaw;
using NINA.Profile.Interfaces;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NINA.Core.Utility;
using System.Linq;
using System.Globalization;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MyPlanetarium {

    internal class CartesDuCiel : IPlanetarium {
        private string address;
        private int port;

        public CartesDuCiel(IProfileService profileService) {
            this.address = profileService.ActiveProfile.PlanetariumSettings.CdCHost;
            this.port = profileService.ActiveProfile.PlanetariumSettings.CdCPort;
        }

        public string Name => "Cartes Du Ciel";

        public bool CanGetRotationAngle => false;

        /// <summary>
        /// Get the selected object in CdC
        /// </summary>
        /// <returns></returns>
        public async Task<DeepSkyObject> GetTarget() {
            try {
                string command = "GETSELECTEDOBJECT\r\n";

                var query = new BasicQuery(address, port, command);
                string response = await query.SendQuery();

                if (!response.StartsWith("OK!")) {
                    return await GetView();
                }

                var columns = response.Split('\t');

                // An "OK!" response with fewer than 2 columns means that CdC is listening ok but the user has not selected an object.
                if (columns.Length < 2) {
                    return await GetView();
                }

                if (!Match(columns[0].Replace("OK!", string.Empty), @"(([0-9]{1,2})([h|:]|[?]{2})([0-9]{1,2})([m|:]|[?]{2})?([0-9]{1,2}(?:\.[0-9]+){0,1})?([s|:]|[?]{2}))", out var raString)) { throw new PlanetariumObjectNotSelectedException(); }
                var ra = AstroUtil.HMSToDegrees(raString);

                if (!Match(columns[1], @"([\+|-]([0-9]{1,2})([d|°|:]|[?]{2})([0-9]{1,2})([m|'|:]|[?]{2})?([0-9]{1,2}(?:\.[0-9]+){0,1})?([s|""|:]|[?]{2}))", out var decString)) { throw new PlanetariumObjectNotSelectedException(); }
                var dec = AstroUtil.DMSToDegrees(decString);

                if (!Match(columns.Last(), @"(?<=Equinox:).*", out var equinox)) { throw new PlanetariumObjectNotSelectedException(); }
                equinox = equinox.Replace("\r", string.Empty).Replace("\n", string.Empty);

                var coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), equinox.ToLower().Contains("2000") ? Epoch.J2000 : Epoch.JNOW);

                var dso = new DeepSkyObject(columns[3].Trim(), coordinates.Transform(Epoch.J2000), string.Empty, null);

                return dso;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }

        private async Task<DeepSkyObject> GetView() {
            try {
                string commandRa = "GETRA F\r\n";
                string commandDec = "GETDEC F\r\n";
                string commandEq = "GETCHARTEQSYS F\r\n";

                var queryRa = new BasicQuery(address, port, commandRa);
                string responseRa = await queryRa.SendQuery();
                var queryDec = new BasicQuery(address, port, commandDec);
                string responseDec = await queryDec.SendQuery();
                var queryEq = new BasicQuery(address, port, commandEq);
                string responseEq = await queryEq.SendQuery();

                if (!responseRa.StartsWith("OK!") || !responseDec.StartsWith("OK!")) { throw new PlanetariumObjectNotSelectedException(); }

                var ra = double.Parse(responseRa.Replace("OK!", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty), CultureInfo.InvariantCulture);
                var dec = double.Parse(responseDec.Replace("OK!", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty), CultureInfo.InvariantCulture);
                var equinox = responseEq.Replace("OK!", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);

                var coordinates = new Coordinates(Angle.ByHours(ra), Angle.ByDegree(dec), equinox.ToLower().Contains("2000") ? Epoch.J2000 : Epoch.JNOW);

                return new DeepSkyObject(string.Empty, coordinates.Transform(Epoch.J2000), string.Empty, null);
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }

        public async Task<Location> GetSite() {
            try {
                string command = "GETOBS\r\n";

                var query = new BasicQuery(address, port, command);
                string response = await query.SendQuery();

                if (!response.StartsWith("OK!")) { throw new PlanetariumFailedToGetCoordinates(); }

                if (!Match(response, @"(?<=LAT:)[\+|-]([0-9]{1,2})[:|d]([0-9]{1,2})[:|m]?([0-9]{1,2}(?:\.[0-9]+){0,1})?[:|s]", out var latutideString)) { throw new PlanetariumFailedToGetCoordinates(); }

                if (!Match(response, @"(?<=LON:)[\+|-]([0-9]{1,3})[:|d]([0-9]{1,2})[:|m]?([0-9]{1,2}(?:\.[0-9]+){0,1})?[:|s]", out var longitudeString)) { throw new PlanetariumFailedToGetCoordinates(); }

                if (!Match(response, @"(?<=ALT:)([0-9]{0,5})[m]", out var altitudeString)) { throw new PlanetariumFailedToGetCoordinates(); }

                var coords = new Location {
                    Latitude = AstroUtil.DMSToDegrees(latutideString),
                    Longitude = -AstroUtil.DMSToDegrees(longitudeString),
                    Elevation = AstroUtil.DMSToDegrees(altitudeString)
                };

                return coords;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }

        public Task<double> GetRotationAngle() {
            return Task.FromResult(double.NaN);
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
    }
}