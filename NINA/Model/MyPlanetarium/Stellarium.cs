#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility;
using NINA.Utility.Astrometry;
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
                if (response == string.Empty) return null;

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumStatus>();

                Coords loc = new Coords();
                loc.Latitude = status.Location.Latitude;
                loc.Longitude = status.Location.Longitude;
                loc.Elevation = status.Location.Altitude;
                return loc;
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            return null;
        }

        public async Task<DeepSkyObject> GetTarget() {
            string route = "/api/objects/info?format=json";

            var request = new HttpGetRequest(this.baseUrl + route);
            try {
                var response = await request.Request(new CancellationToken());
                if (response == string.Empty) return null;

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumObject>();

                var ra = Astrometry.EuclidianModulus(status.RightAscension, 360);
                var dec = status.Declination;

                var coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                var dso = new DeepSkyObject(status.Name, coordinates, string.Empty);
                return dso;
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            return null;
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