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

namespace NINA.Astrometry {

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ObserverInfo {

        /// <summary>
        /// Observers's latitude (degrees)
        /// </summary>
        [JsonProperty]
        public double Latitude { get; set; } = 0d;

        /// <summary>
        /// Observer's longitude (degrees)
        /// </summary>
        [JsonProperty]
        public double Longitude { get; set; } = 0d;

        /// <summary>
        /// Observer's elevation above mean sea level (meters)
        /// </summary>
        [JsonProperty]
        public double Elevation { get; set; } = 0d;

        /// <summary>
        /// Observer's local air pressure (millibars or hectopascals)
        /// </summary>
        [JsonProperty]
        public double Pressure { get; set; } = 1013.25;

        /// <summary>
        /// Observer's local temperature (degrees C)
        /// </summary>
        [JsonProperty]
        public double Temperature { get; set; } = 20d;

        /// <summary>
        /// Observer's local humidity (percent)
        /// </summary>
        [JsonProperty]
        public double Humidity { get; set; } = 0d;
    }
}