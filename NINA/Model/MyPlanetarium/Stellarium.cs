#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Exceptions;
using NINA.Profile;
using System;
using System.Threading.Tasks;
using NINA.Utility.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NINA.Model.MyPlanetarium {

    internal class Stellarium : IPlanetarium {
        private string baseUrl;

        public Stellarium(IProfileService profileService) {
            var baseAddress = profileService.ActiveProfile.PlanetariumSettings.StellariumHost;
            var port = profileService.ActiveProfile.PlanetariumSettings.StellariumPort;
            this.baseUrl = $"http://{baseAddress}:{port}";
        }

        public string Name {
            get {
                return "Stellarium";
            }
        }

        public async Task<Coords> GetSite() {
            string route = "/api/main/status";

            var request = new HttpGetRequest(this.baseUrl + route);
            try {
                var response = await request.Request(new CancellationToken());
                if (string.IsNullOrEmpty(response)) throw new PlanetariumFailedToConnect();

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumStatus>();

                Coords loc = new Coords {
                    Latitude = status.Location.Latitude,
                    Longitude = status.Location.Longitude,
                    Elevation = status.Location.Altitude
                };

                return loc;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        private async Task<DeepSkyObject> GetView() {
            string route = "/api/main/view";

            var request = new HttpGetRequest(this.baseUrl + route);
            try {
                var response = await request.Request(new CancellationToken());
                if (string.IsNullOrEmpty(response)) throw new PlanetariumFailedToConnect();

                /* The api returns arrays in an invalid json array format so we need to remove the quotes first */
                response = response.Replace("\"[", "[").Replace("]\"", "]");

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumView>();

                var x = Angle.ByRadians(status.J2000[0]);
                var y = Angle.ByRadians(status.J2000[1]);
                var z = Angle.ByRadians(status.J2000[2]);

                var dec = z.Asin();
                var ra = Angle.Atan2(y, x);

                var coordinates = new Coordinates(ra, dec, Epoch.J2000);
                var dso = new DeepSkyObject(string.Empty, coordinates, string.Empty);

                return dso;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        public async Task<DeepSkyObject> GetTarget() {
            string route = "/api/objects/info?format=json";

            var request = new HttpGetRequest(this.baseUrl + route);
            try {
                var response = await request.Request(new CancellationToken());
                if (string.IsNullOrEmpty(response)) return await GetView();

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumObject>();

                var ra = Astrometry.EuclidianModulus(status.RightAscension, 360);
                var dec = status.Declination;

                var coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                var dso = new DeepSkyObject(status.Name, coordinates, string.Empty);
                return dso;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        private class StellariumView {

            [JsonProperty(PropertyName = "altAz")]
            public double[] AltAz;

            [JsonProperty(PropertyName = "j2000")]
            public double[] J2000;

            [JsonProperty(PropertyName = "jNow")]
            public double[] JNOW;
        }

        private class StellariumObject {

            [JsonProperty(PropertyName = "raJ2000")]
            public double RightAscension;

            [JsonProperty(PropertyName = "decJ2000")]
            public double Declination;

            [JsonProperty(PropertyName = "name")]
            public string Name;
        }

        private class StellariumLocation {

            [JsonProperty(PropertyName = "altitude")]
            public double Altitude;

            [JsonProperty(PropertyName = "latitude")]
            public double Latitude;

            [JsonProperty(PropertyName = "longitude")]
            public double Longitude;
        }

        private class StellariumStatus {

            [JsonProperty(PropertyName = "location")]
            public StellariumLocation Location;
        }
    }
}
