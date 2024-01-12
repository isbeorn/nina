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
using System;

namespace NINA.Equipment.Equipment.MyGPS.PegasusAstro.UnityApi {

    public class DriverUranusReport {

        public class AbsolutePressure {

            [JsonProperty("hPa")]
            public double HPa { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Altitude {

            [JsonProperty("meters")]
            public double Meters { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class BarometricAltitude {

            [JsonProperty("meters")]
            public double Meters { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class CloudCoverage {

            [JsonProperty("percentage")]
            public double Percentage { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Data {

            [JsonProperty("uniqueKey")]
            public string UniqueKey { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("message")]
            public Message Message { get; set; }
        }

        public class Dd {

            [JsonProperty("decimalDegree")]
            public double DecimalDegree { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class DewPoint {

            [JsonProperty("temperature")]
            public double Temperature { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Dms {

            [JsonProperty("degree")]
            public int Degree { get; set; }

            [JsonProperty("minute")]
            public int Minute { get; set; }

            [JsonProperty("second")]
            public double Second { get; set; }

            [JsonProperty("standardFormat")]
            public string StandardFormat { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Illuminance {

            [JsonProperty("lux")]
            public double Lux { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Latitude {

            [JsonProperty("dms")]
            public Dms Dms { get; set; }

            [JsonProperty("dd")]
            public Dd Dd { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Longitude {

            [JsonProperty("dms")]
            public Dms Dms { get; set; }

            [JsonProperty("dd")]
            public Dd Dd { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Message {

            [JsonProperty("temperature")]
            public AmbientTemperature Temperature { get; set; }

            [JsonProperty("relativeHumidity")]
            public RelativeHumidity RelativeHumidity { get; set; }

            [JsonProperty("dewPoint")]
            public DewPoint DewPoint { get; set; }

            [JsonProperty("absolutePressure")]
            public AbsolutePressure AbsolutePressure { get; set; }

            [JsonProperty("relativePressure")]
            public RelativePressure RelativePressure { get; set; }

            [JsonProperty("barometricAltitude")]
            public BarometricAltitude BarometricAltitude { get; set; }

            [JsonProperty("skyQuality")]
            public SkyQuality SkyQuality { get; set; }

            [JsonProperty("nelm")]
            public Nelm Nelm { get; set; }

            [JsonProperty("illuminance")]
            public Illuminance Illuminance { get; set; }

            [JsonProperty("temperatureDifference")]
            public TemperatureDifference TemperatureDifference { get; set; }

            [JsonProperty("cloudCoverage")]
            public CloudCoverage CloudCoverage { get; set; }

            [JsonProperty("skyTemperature")]
            public SkyTemperature SkyTemperature { get; set; }

            [JsonProperty("isGpsFixed")]
            public bool IsGpsFixed { get; set; }

            [JsonProperty("dateTime")]
            public DateTime DateTime { get; set; }

            [JsonProperty("latitude")]
            public Latitude Latitude { get; set; }

            [JsonProperty("longitude")]
            public Longitude Longitude { get; set; }

            [JsonProperty("totalSatellites")]
            public int TotalSatellites { get; set; }

            [JsonProperty("altitude")]
            public Altitude Altitude { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Nelm {

            [JsonProperty("vMag")]
            public double VMag { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class RelativeHumidity {

            [JsonProperty("percentage")]
            public double Percentage { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class RelativePressure {

            [JsonProperty("hPa")]
            public double HPa { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class Report {

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public Data Data { get; set; }
        }

        public class SkyQuality {

            [JsonProperty("mpsas")]
            public double Mpsas { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class SkyTemperature {

            [JsonProperty("temperature")]
            public double Temperature { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class AmbientTemperature {

            [JsonProperty("temperature")]
            public double Temperature { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }

        public class TemperatureDifference {

            [JsonProperty("temperature")]
            public double Temperature { get; set; }

            [JsonProperty("messageType")]
            public string MessageType { get; set; }
        }
    }
}