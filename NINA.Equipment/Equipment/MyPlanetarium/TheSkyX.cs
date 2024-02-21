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
using System.Threading.Tasks;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using System.Threading;

namespace NINA.Equipment.Equipment.MyPlanetarium {

    internal class TheSkyX : IPlanetarium {
        private string address;
        private int port;
        private bool useSelectedObject;

        public TheSkyX(IProfileService profileService) {
            this.address = profileService.ActiveProfile.PlanetariumSettings.TSXHost;
            this.port = profileService.ActiveProfile.PlanetariumSettings.TSXPort;
            this.useSelectedObject = profileService.ActiveProfile.PlanetariumSettings.TSXUseSelectedObject;
        }

        public string Name => "TheSkyX";

        public bool CanGetRotationAngle => true;

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

                return new DeepSkyObject(raDecName[2], newCoordinates, string.Empty, null);
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }

        public async Task<Location> GetSite(CancellationToken token) {
            try {
                Location loc = new Location();
                string[] coords;

                /*
                 * Get the location (latitude, longitude, elevation) that is currently set
                 * in TSX. Elevation is in meters.
                 */
                var script = string.Join("\r\n", new string[] {
                    @"/* Java Script */",
                    @"/* Socket Start Packet */",
                    @"var Out = """";",
                    @"var Lat = 0;",
                    @"var Long = 0;",
                    @"var Elevation = 0;",
                    @"sky6StarChart.DocumentProperty(0);",
                    @"Lat = sky6StarChart.DocPropOut;",
                    @"sky6StarChart.DocumentProperty(1);",
                    @"Long = sky6StarChart.DocPropOut;",
                    @"sky6StarChart.DocumentProperty(3);",
                    @"Elevation = sky6StarChart.DocPropOut;",
                    @"Out = String(Lat) + "","" + String(Long) + "","" + String(Elevation);",
                    @"/* Socket End Packet */"
                });

                var query = new BasicQuery(address, port, script);
                string reply = await query.SendQuery(token);

                string[] response = reply.Split('|');

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
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }

        public async Task<double> GetRotationAngle() {
            try {
                double rotationAngle = double.NaN;

                // User needs to turn off "Get coordinates from selected object" for this to really work right
                if (useSelectedObject) { return rotationAngle; }

                /*
                 * Iterate through the list of configured FOVIs in TSX and find the first one that
                 * is both visible and uses the screen center its reference. If we find such a FOVI,
                 * we get its rotation angle.
                 */
                var script = string.Join("\r\n", new string[] {
                    @"/* Java Script */",
                    @"/* Socket Start Packet */",
                    @"var angle = NaN;",
                    @"var fov = sky6MyFOVs;",
                    @"for (var i = 0; i < fov.Count; i++) {",
                    @"fov.Name(i);",
                    @"var name = fov.OutString;",
                    @"fov.Property(name, 0, 0);",
                    @"var isVisible = fov.OutVar;",
                    @"fov.Property(name, 0, 2);",
                    @"var refFrame = fov.OutVar;",
                    @"if (isVisible == 1 && refFrame == 0) {",
                    @"fov.Property(name, 0, 1);",
                    @"angle = fov.OutVar;",
                    @"break; } }",
                    @"Out = String(angle);",
                    @"/* Socket End Packet */"
                });

                var query = new BasicQuery(address, port, script);
                string reply = await query.SendQuery();

                string[] response = reply.Split('|');

                if (response[1].Equals("No error. Error = 0.")) {
                    if (double.TryParse(response[0], out rotationAngle)) {
                    }
                }

                return rotationAngle;
            } catch {
                return double.NaN;
            }
        }

        private async Task<string[]> GetSelectedObject() {
            string[] raDecName;

            /*
             * Get J2000 coordinates of currently selected target in TheSkyX
             * Object RA is returned in hours, Dec is returned in degrees.
             * We use the primary object name provided by TSX.
             *
             * Special thanks to Kenneth Sturrock for providing examples and advice on
             * how to accomplish this with TSX.
             */
            var script = string.Join("\r\n", new string[] {
                @"/* Java Script */",
                @"/* Socket Start Packet */",
                @"var Out = """";",
                @"var Target56 = 0;",
                @"var Target57 = 0;",
                @"var Name0 = """";",
                @"sky6ObjectInformation.Property(56);",
                @"Target56 = sky6ObjectInformation.ObjInfoPropOut;",
                @"sky6ObjectInformation.Property(57);",
                @"Target57 = sky6ObjectInformation.ObjInfoPropOut;",
                @"sky6ObjectInformation.Property(0);",
                @"Name0 = sky6ObjectInformation.ObjInfoPropOut;",
                @"Out = String(Target56) + "","" + String(Target57) + "","" + String(Name0);",
                @"/* Socket End Packet */"
            });

            var query = new BasicQuery(address, port, script);
            string reply = await query.SendQuery();

            string[] response = reply.Split('|');

            if (response[1].Equals("No error. Error = 0.")) {
                // put the RA, Dec, and object name into an array
                raDecName = response[0].Split(',');
            } else {
                throw new PlanetariumFailedToGetCoordinates();
            }

            return raDecName;
        }

        private async Task<string[]> GetSkyChartCenter() {
            string[] raDecName = new string[3];

            /*
             * Get J2000 coordinates of the center of the TSX star chart.
             * There is no way to get an object name, so we set that to an empty string.
             */
            var script = string.Join("\r\n", new string[] {
                @"/* Java Script */",
                @"/* Socket Start Packet */",
                @"var Out = """";",
                @"var chartRA = 0;",
                @"var chartDec = 0;",
                @"chartRA = sky6StarChart.RightAscension;",
                @"chartDec = sky6StarChart.Declination;",
                @"Out = String(chartRA) + "","" + String(chartDec);",
                @"/* Socket End Packet */"
            });

            var query = new BasicQuery(address, port, script);
            string reply = await query.SendQuery();

            string[] response = reply.Split('|');

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