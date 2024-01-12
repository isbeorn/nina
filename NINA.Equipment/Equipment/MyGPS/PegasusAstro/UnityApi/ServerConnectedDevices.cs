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
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyGPS.PegasusAstro.UnityApi {

    public class ServerConnectedDevices {

        public class Device {

            [JsonProperty("uniqueKey")]
            public string UniqueKey { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("fullName")]
            public string FullName { get; set; }

            [JsonProperty("deviceID")]
            public string DeviceID { get; set; }

            [JsonProperty("firmware")]
            public string Firmware { get; set; }

            [JsonProperty("revision")]
            public string Revision { get; set; }
        }

        public class Response {

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("code")]
            public int? Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public List<Device> Devices { get; set; }
        }
    }
}