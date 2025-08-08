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
using System.IO;
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

            var schema = await NJsonSchema.JsonSchema.FromJsonAsync(TpvMessage.Schema);
            var location = new Location();

            try {
                using TcpClient client = new();
                await client.ConnectAsync(gpsdIp, gpsdPort).WaitAsync(TimeSpan.FromSeconds(10), token);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII);

                // Send the WATCH command to start receiving JSON data
                string watchCommand = "?WATCH={\"enable\":true,\"json\":true}\n";
                byte[] watchCommandBytes = Encoding.ASCII.GetBytes(watchCommand);
                await stream.WriteAsync(watchCommandBytes, token);
                await stream.FlushAsync(token);

                int tries = 0;

                while (tries < 8 && !token.IsCancellationRequested) {
                    string? line = await reader.ReadLineAsync(token);

                    if (string.IsNullOrEmpty(line)) continue;

                    var validationErrors = schema.Validate(line);
                    if (validationErrors.Count > 0) continue;

                    var tpv = JsonConvert.DeserializeObject<TpvMessage>(line);

                    if (tpv.Class.Equals("TPV")) {
                        Logger.Debug(tpv.ToString());

                        if (tpv.Mode == 3) {
                            location.Latitude = tpv.Latitude;
                            location.Longitude = tpv.Longitude;
                            location.Elevation = tpv.Altitude;

                            return location;
                        } else {
                            if (tpv.Mode == 2) {
                                throw new GnssNoFixException(Loc.Instance["LblGnssFix2D"]);
                            } else {
                                throw new GnssNoFixException(string.Empty);
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

        public class TpvMessage {
            public static string Schema => @"{
                  '$schema': 'http://json-schema.org/draft-04/schema#',
                  'type': 'object',
                  'properties': {
                    'class': {
                      'type': 'string'
                    },
                    'device': {
                      'type': 'string'
                    },
                    'mode': {
                      'type': 'integer'
                    },
                    'time': {
                      'type': 'string'
                    },
                    'leapseconds': {
                      'type': 'integer'
                    },
                    'ept': {
                      'type': 'number'
                    },
                    'lat': {
                      'type': 'number'
                    },
                    'lon': {
                      'type': 'number'
                    },
                    'altHAE': {
                      'type': 'number'
                    },
                    'altMSL': {
                      'type': 'number'
                    },
                    'alt': {
                      'type': 'number'
                    },
                    'epx': {
                      'type': 'number'
                    },
                    'epy': {
                      'type': 'number'
                    },
                    'epv': {
                      'type': 'number'
                    },
                    'track': {
                      'type': 'number'
                    },
                    'magtrack': {
                      'type': 'number'
                    },
                    'magvar': {
                      'type': 'number'
                    },
                    'speed': {
                      'type': 'number'
                    },
                    'climb': {
                      'type': 'number'
                    },
                    'epd': {
                      'type': 'number'
                    },
                    'eps': {
                      'type': 'number'
                    },
                    'epc': {
                      'type': 'number'
                    },
                    'ecefx': {
                      'type': 'number'
                    },
                    'ecefy': {
                      'type': 'number'
                    },
                    'ecefz': {
                      'type': 'number'
                    },
                    'ecefvx': {
                      'type': 'number'
                    },
                    'ecefvy': {
                      'type': 'number'
                    },
                    'ecefvz': {
                      'type': 'number'
                    },
                    'ecefpAcc': {
                      'type': 'number'
                    },
                    'ecefvAcc': {
                      'type': 'number'
                    },
                    'geoidSep': {
                      'type': 'number'
                    },
                    'eph': {
                      'type': 'number'
                    },
                    'sep': {
                      'type': 'number'
                    }
                  },
                  'required': ['class', 'mode' ]
                }";

            [JsonProperty("class", Required = Required.Always)]
            public string Class { get; set; } = string.Empty;

            [JsonProperty("lat")]
            public double Latitude { get; set; }

            [JsonProperty("lon")]
            public double Longitude { get; set; }

            [JsonProperty("altMSL")]
            public double Altitude { get; set; }

            [JsonProperty("mode", Required = Required.Always)]
            public int Mode { get; set; }

            public override string ToString() {
                return $"Class: {Class}, Mode: {Mode}, Latitude: {Latitude}, Longitude: {Longitude}, Altitude: {Altitude}";
            }
        }
    }
}
