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
using NINA.Plugin.Interfaces;
using System;

namespace NINA.Plugin.ManifestDefinition {

    [Serializable]
    public class PluginDescription : IPluginDescription {

        [JsonProperty(Required = Required.Always)]
        public string ShortDescription { get; set; }

        [JsonProperty]
        public string LongDescription { get; set; }

        [JsonProperty]
        public string FeaturedImageURL { get; set; }

        [JsonProperty]
        public string ScreenshotURL { get; set; }

        [JsonProperty]
        public string AltScreenshotURL { get; set; }
    }
}