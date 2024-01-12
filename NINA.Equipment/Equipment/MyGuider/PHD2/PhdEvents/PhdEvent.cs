#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System.Runtime.Serialization;
using NINA.Core.Interfaces;
using Newtonsoft.Json;

namespace NINA.Equipment.Equipment.MyGuider.PHD2.PhdEvents {

    [DataContract]
    public class PhdEvent : BaseINPC, IGuideEvent {

        [DataMember]
        [JsonProperty]
        public string Event { get; set; }

        [DataMember]
        [JsonProperty]
        public string TimeStamp { get; set; }

        [DataMember]
        [JsonProperty]
        public string Host { get; set; }

        [DataMember]
        [JsonProperty]
        public int Inst { get; set; }
    }
}