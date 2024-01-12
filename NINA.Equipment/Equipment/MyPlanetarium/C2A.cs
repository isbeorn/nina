#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Core.Utility.TcpRaw;
using NINA.Profile.Interfaces;
using System.Threading.Tasks;
using System;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MyPlanetarium {

    internal class C2A : IPlanetarium {
        private string address;
        private int port;

        public C2A(IProfileService profileService) {
            this.address = profileService.ActiveProfile.PlanetariumSettings.C2AHost;
            this.port = profileService.ActiveProfile.PlanetariumSettings.C2APort;
        }

        public string Name => "C2A";

        public bool CanGetRotationAngle => false;

        /// <summary>
        /// Get the selected object in C2A
        /// </summary>
        /// <returns></returns>
        public async Task<DeepSkyObject> GetTarget() {
            try {
                string command = "GetRa;GetDe;\r\n";

                var query = new BasicQuery(address, port, command);
                string response = await query.SendQuery();
                response = response.TrimEnd('\r', '\n');

                if (!string.IsNullOrEmpty(response)) {
                    string[] info = response.Split(';');

                    Coordinates newCoordinates = new Coordinates(double.Parse(info[0].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
                                                         double.Parse(info[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
                                                         Epoch.J2000, Coordinates.RAType.Hours);

                    return new DeepSkyObject(info[2], newCoordinates, string.Empty, null);
                } else {
                    throw new PlanetariumFailedToGetCoordinates();
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Return the configured user location from C2A
        /// </summary>
        /// <returns></returns>
        public async Task<Location> GetSite() {
            try {
                string command = "GetLatitude;GetLongitude;\r\n";

                var query = new BasicQuery(address, port, command);
                string response = await query.SendQuery();

                response = response.TrimEnd('\r', '\n');

                if (!string.IsNullOrEmpty(response)) {
                    var info = response.Split(';');

                    return new Location {
                        Latitude = double.Parse(info[0].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
                        Longitude = double.Parse(info[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
                        Elevation = 0
                    };
                } else {
                    throw new PlanetariumFailedToGetCoordinates();
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }

        public Task<double> GetRotationAngle() {
            return Task.FromResult(double.NaN);
        }
    }
}