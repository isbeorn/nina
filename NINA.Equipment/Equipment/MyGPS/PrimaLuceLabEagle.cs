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
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyGPS {
    public partial class PrimaLuceLabEagle : BaseINPC, IGnss {
        private const string eagleGpsUrl = "http://localhost:1380/getgps";

        public PrimaLuceLabEagle(IProfileService profileService) {
        }

        public string Name => "PrimaLuceLab Eagle";

        public async Task<Location> GetLocation() {
            var http = new HttpGetRequest(eagleGpsUrl);
            var location = new Location();

            try {
                var response = await http.Request(CancellationToken.None);
                Logger.Debug(response);

                var gps = JsonConvert.DeserializeObject<EagleGpsResponse>(response);

                if (!gps.Result.Equals("OK", StringComparison.InvariantCultureIgnoreCase) || gps.Numsat < 4 || gps.Latitude.Contains("--") || string.IsNullOrWhiteSpace(gps.Latitude)) {
                    throw new GnssNoFixException(string.Empty);
                }

                location.Latitude = double.Parse(CleanseValue(gps.Latitude), CultureInfo.InvariantCulture);
                location.Longitude = double.Parse(CleanseValue(gps.Longitude), CultureInfo.InvariantCulture);
                location.Elevation = double.Parse(CleanseValue(gps.Altitude), CultureInfo.InvariantCulture);

                return location;
            } catch (GnssNoFixException) {
                throw;
            } catch (Exception ex) {
                throw new GnssFailedToConnectException(ex.Message);
            }

        }

        // Some versions of Eagle Manager put degree symbols on the lat/long and 'm' on the altitude
        // This method is here to strip out any undesireable characters from what the API gives us
        private static string CleanseValue(string value) {
            return CleanupRegex().Replace(value, string.Empty);
        }

        internal class EagleGpsResponse {
            [JsonProperty("result")]
            internal string Result { get; set; }

            [JsonProperty("latitude")]
            internal string Latitude { get; set; }

            [JsonProperty("longitude")]
            internal string Longitude { get; set; }

            [JsonProperty("altitude")]
            internal string Altitude { get; set; }

            [JsonProperty("date")]
            internal string Date { get; set; }

            [JsonProperty("time")]
            internal string Time { get; set; }

            [JsonProperty("numsat")]
            internal int Numsat { get; set; }
        }

        [GeneratedRegex("[^0-9.-]")]
        private static partial Regex CleanupRegex();
    }
}
