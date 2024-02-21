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
using System;
using System.Globalization;
using System.Threading.Tasks;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using System.Threading;

namespace NINA.Equipment.Equipment.MyPlanetarium {

    internal class SkytechX : IPlanetarium {
        private string address;
        private int port;

        public SkytechX(IProfileService profileService) {
            this.address = profileService.ActiveProfile.PlanetariumSettings.SkytechXHost;
            this.port = profileService.ActiveProfile.PlanetariumSettings.SkytechXPort;
        }

        public string Name => "SkytechX";

        public bool CanGetRotationAngle => false;

        /// <summary>
        /// Get the selected object
        /// </summary>
        /// <returns></returns>
        public async Task<DeepSkyObject> GetTarget() {
            try {
                string command = "SetMode 0\r\n";
                var query = new BasicQuery(address, port, command, "OK!\r\n");
                string response = await query.SendQuery();

                command = "GetPos\r\n";
                query = new BasicQuery(address, port, command, "OK!\r\n");
                response = await query.SendQuery();

                response = response.TrimEnd('\r', '\n');
                string[] info = response.Split(',');

                if (!string.IsNullOrEmpty(info[0]) && !string.IsNullOrEmpty(info[1])) {
                    var coordinates = new Coordinates(double.Parse(info[0], CultureInfo.InvariantCulture),
                                                  double.Parse(info[1], CultureInfo.InvariantCulture),
                                                  Epoch.J2000, Coordinates.RAType.Degrees);

                    return new DeepSkyObject(string.Empty, coordinates, string.Empty, null);
                } else {
                    throw new PlanetariumObjectNotSelectedException();
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Return the configured user location
        /// </summary>
        /// <returns></returns>
        public Task<Location> GetSite(CancellationToken token) {
            throw new InvalidOperationException();
        }

        public Task<double> GetRotationAngle() {
            throw new InvalidOperationException();
        }
    }
}