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
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyGPS {
    public class Gpsd(IProfileService profileService) : BaseINPC, IGnss {
        private string gpsdHost = string.Empty;
        private ushort gpsdPort = 2947;

        public string Name => "GPSD";

        public async Task<Location> GetLocation(CancellationToken token) {
            IPHostEntry hostEntry;
            IPAddress gpsdIp;

            gpsdHost = profileService.ActiveProfile.GnssSettings.GpsdHost;
            gpsdPort = profileService.ActiveProfile.GnssSettings.GpsdPort;

            if (string.IsNullOrEmpty(gpsdHost)) {
                throw new GnssInvalidHostException(Loc.Instance["LblErrorNoHostSpecified"]);
            }

            try {
                hostEntry = DnsHelper.GetIPHostEntryByName(gpsdHost);
                gpsdIp = hostEntry.AddressList.First();
            } catch (Exception ex) {
                throw new GnssInvalidHostException(ex.Message);
            }

            var location = new Location();

            try {
                using TcpClient client = new();
                await client.ConnectAsync(gpsdIp, gpsdPort).WaitAsync(TimeSpan.FromSeconds(10), token);
                using NetworkStream stream = client.GetStream();

                // Send the WATCH command to start receiving JSON data
                string watchCommand = "?WATCH={\"enable\":true,\"json\":true}\n";
                byte[] watchCommandBytes = Encoding.ASCII.GetBytes(watchCommand);
                await stream.WriteAsync(watchCommandBytes, token);

                byte[] buffer = new byte[4096];
                int tries = 0;

                while (tries < 5 && !token.IsCancellationRequested) {
                    int bytesRead = await stream.ReadAsync(buffer, token);
                    if (bytesRead == 0) break;

                    string json = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (json.Contains("\"class\":\"TPV\"")) {
                        var tpv = JsonConvert.DeserializeObject<TpvMessage>(json);

                        if (tpv != null) {
                            if (tpv.Mode >= 2) {
                                location.Latitude = tpv.Latitude ?? 0;
                                location.Longitude = tpv.Longitude ?? 0;
                                location.Elevation = tpv.Altitude ?? 0;

                                return location;
                            } else {
                                throw new GnssNoFixException();
                            }
                        }
                    }

                    tries++;
                }
            } catch (GnssNoFixException) {
                throw;
            } catch (GnssNotFoundException) {
                throw;
            } catch (Exception ex) {
                throw new GnssFailedToConnectException(ex.Message);
            }

            return location;
        }

        private class TpvMessage {
            [JsonProperty("class")]
            public string Class { get; set; } = string.Empty;

            [JsonProperty("lat")]
            public double? Latitude { get; set; }

            [JsonProperty("lon")]
            public double? Longitude { get; set; }

            [JsonProperty("altMSL")]
            public double? Altitude { get; set; }

            [JsonProperty("mode")]
            public int Mode { get; set; }
        }
    }
}
