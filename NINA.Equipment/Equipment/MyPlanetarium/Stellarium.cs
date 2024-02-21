#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyPlanetarium {

    public class Stellarium : IPlanetarium {
        private string baseUrl;

        public Stellarium(IProfileService profileService) {
            var baseAddress = profileService.ActiveProfile.PlanetariumSettings.StellariumHost;
            var port = profileService.ActiveProfile.PlanetariumSettings.StellariumPort;
            this.baseUrl = $"http://{baseAddress}:{port}";
        }

        public string Name => "Stellarium";

        public bool CanGetRotationAngle => true;

        public async Task<Location> GetSite(CancellationToken token) {
            string route = "/api/main/status";

            try {
                var request = new HttpGetRequest(this.baseUrl + route, rethrowOnError: true);
                var response = await request.Request(token);

                if (string.IsNullOrEmpty(response)) throw new PlanetariumFailedToConnect();

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumStatus>();

                Location loc = new Location {
                    Latitude = status.Location.Latitude,
                    Longitude = status.Location.Longitude,
                    Elevation = status.Location.Altitude
                };

                return loc;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import site info: {ex.Message}");

                if (ex.InnerException is SocketException) {
                    throw new PlanetariumFailedToConnect();
                }

                throw;
            }
        }

        public async Task<DeepSkyObject> GetTarget() {
            try {
                var isOcularsCcdEnabled = await IsOcularEnabled();

                // Get the view coordinates if there is a CCD ocular active or if there is no object selected
                return isOcularsCcdEnabled ? await GetCenterView() : await GetSelectedObjectInfo();
            } catch (WebException ex) {
                if (ex.InnerException is SocketException) {
                    throw new PlanetariumFailedToConnect();
                }

                throw;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import object info: {ex.Message}");

                if (ex.InnerException is SocketException) {
                    throw new PlanetariumFailedToConnect();
                }

                throw;
            }
        }

        public async Task<double> GetRotationAngle() {
            string route = "/api/stelproperty/list?format=json";

            try {
                var request = new HttpGetRequest(this.baseUrl + route, rethrowOnError: true);
                var response = await request.Request(new CancellationToken());

                double angle = double.NaN;

                if (string.IsNullOrEmpty(response)) return double.NaN;

                var jobj = JObject.Parse(response);

                // Can do this only if an occular is turned on
                bool isOcularsCcdEnabled = ParseEnableCCD(jobj);

                if (isOcularsCcdEnabled) {
                    angle = ParseRotationAngle(jobj);

                }

                return angle;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import rotation angle: {ex.Message}");

                if (ex.InnerException is SocketException) {
                    throw new PlanetariumFailedToConnect();
                }

                return double.NaN;
            }
        }

        private async Task<DeepSkyObject> GetCenterView(bool getName = true) {
            string route = "/api/main/view";

            try {
                var request = new HttpGetRequest(this.baseUrl + route, rethrowOnError: true);
                var response = await request.Request(new CancellationToken());

                /* The api returns arrays in an invalid json array format so we need to remove the quotes first */
                response = response.Replace("\"[", "[").Replace("]\"", "]");

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumView>();

                var x = Angle.ByRadians(status.J2000[0]);
                var y = Angle.ByRadians(status.J2000[1]);
                var z = Angle.ByRadians(status.J2000[2]);

                var dec = z.Asin();
                var ra = Angle.Atan2(y, x);

                // A bug in Stellarium >= 0.20 will cause it to report negative y values which translates to a negative RA value. This is not desired.
                if (ra.Radians < 0d) {
                    ra = (2 * Math.PI) + ra;
                }

                var coordinates = new Coordinates(ra, dec, Epoch.J2000);
                var dso = new DeepSkyObject(string.Empty, coordinates, string.Empty, null);

                if (getName) {
                    try {
                        var selectedObject = await GetSelectedObjectInfo();
                        dso.Name = selectedObject.Name;
                    } catch (PlanetariumObjectNotSelectedException) {
                        Logger.Info($"An ocular is active but no object is selected. An object name will not be populated.");
                    }
                }

                return dso;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import view info: {ex.Message}");

                if (ex.InnerException is SocketException) {
                    throw new PlanetariumFailedToConnect();
                }

                throw;
            }
        }

        private async Task<DeepSkyObject> GetSelectedObjectInfo() {
            string route = "/api/objects/info?format=json";

            try {
                var request = new HttpGetRequest(this.baseUrl + route, rethrowOnError: true);
                var response = await request.Request(new CancellationToken());

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumObject>();

                // Some objects have no "name" field, but do have a "localized-name" field.
                // Yet other objects have neither, but have a "designations" field that can hold multiple, hyphen-separated catalog designations.
                // Prefer in the following order: localized-name, name, the first name in the designations field, and lastly the type.
                var name = string.Empty;

                if (!string.IsNullOrEmpty(status?.LocalizedName)) {
                    name = status.LocalizedName;
                } else if (!string.IsNullOrEmpty(status?.Name)) {
                    name = status.Name;
                } else if (!string.IsNullOrEmpty(status?.Designations)) {
                    name = status.Designations.Split(new[] { " - " }, StringSplitOptions.None)[0];
                } else if (!string.IsNullOrEmpty(status?.Type)) {
                    name = status.Type;
                }

                var ra = AstroUtil.EuclidianModulus(status.RightAscension, 360d);
                var dec = status.Declination;

                var coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                var dso = new DeepSkyObject(name, coordinates, string.Empty, null);
                return dso;
            } catch (WebException ex) {
                if (ex.InnerException is SocketException) {
                    throw new PlanetariumFailedToConnect();
                }

                if (ex.Status == WebExceptionStatus.ProtocolError) {
                    var response = ex.Response as HttpWebResponse;

                    // Stellarium API replied with a 404. This means no object was selected.
                    // Try to get the center view instead.
                    if (response.StatusCode == HttpStatusCode.NotFound) {
                        return await GetCenterView(false);
                    }
                }

                throw;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import selected object info: {ex.Message}");

                if (ex.InnerException is SocketException) {
                    throw new PlanetariumFailedToConnect();
                }

                throw;
            }
        }

        private async Task<bool> IsOcularEnabled() {
            string route = "/api/stelproperty/list?format=json";

            try {
                var request = new HttpGetRequest(this.baseUrl + route, rethrowOnError: true);
                var response = await request.Request(new CancellationToken());

                var jobj = JObject.Parse(response);
                return ParseEnableCCD(jobj);
            } catch (Exception ex) {
                return ex.InnerException is SocketException ? throw new PlanetariumFailedToConnect() : false;
            }
        }

        public bool ParseEnableCCD(JObject jObject) {
            if (bool.TryParse((string)jObject["Oculars.enableCCD"]["value"], out bool enableCCD)) {
                return enableCCD;
            } else {
                return false;
            }
        }

        public double ParseRotationAngle(JObject jObject) {
            if (double.TryParse((string)jObject["Oculars.selectedCCDRotationAngle"]["value"], out double angle)) {
                // Stellatium ocular rotation is clockwise, so it needs to be reversed
                return AstroUtil.EuclidianModulus(360 - angle, 360);
            } else {
                return 0d;
            }
        }

        public class StellariumView {

            [JsonProperty(PropertyName = "altAz")]
            public double[] AltAz;

            [JsonProperty(PropertyName = "j2000")]
            public double[] J2000;

            [JsonProperty(PropertyName = "jNow")]
            public double[] JNOW;

            public StellariumView(double[] altAz, double[] j2000, double[] jnow) {
                AltAz = altAz;
                J2000 = j2000;
                JNOW = jnow;
            }
        }

        public class StellariumObject {

            [JsonProperty(PropertyName = "raJ2000")]
            public double RightAscension;

            [JsonProperty(PropertyName = "decJ2000")]
            public double Declination;

            [JsonProperty(PropertyName = "name")]
            public string Name;

            [JsonProperty(PropertyName = "localized-name")]
            public string LocalizedName;

            [JsonProperty(PropertyName = "designations")]
            public string Designations;

            [JsonProperty(PropertyName = "type")]
            public string Type;

            public StellariumObject(double rightAscension, double declination, string name, string localizedName, string designations, string type) {
                RightAscension = rightAscension;
                Declination = declination;
                Name = name;
                LocalizedName = localizedName;
                Designations = designations;
                Type = type;
            }
        }

        public class StellariumLocation {

            [JsonProperty(PropertyName = "altitude")]
            public double Altitude;

            [JsonProperty(PropertyName = "latitude")]
            public double Latitude;

            [JsonProperty(PropertyName = "longitude")]
            public double Longitude;

            public StellariumLocation(double altitude, double latitude, double longitude) {
                Altitude = altitude;
                Latitude = latitude;
                Longitude = longitude;
            }
        }

        public class StellariumStatus {

            [JsonProperty(PropertyName = "location")]
            public StellariumLocation Location;

            public StellariumStatus(StellariumLocation location) {
                Location = location;
            }
        }
    }
}