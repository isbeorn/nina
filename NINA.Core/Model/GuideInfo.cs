#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Interfaces;
using System.Runtime.Serialization;

#pragma warning disable 1998

namespace NINA.Core.Model {

    /// <summary>
    /// This class is used to send over guide data from the service to the guider client.
    /// </summary>
    [DataContract]
    public class GuideInfo {

        [DataMember]
        public IGuideStep GuideStep { get; set; }

        [DataMember]
        public string State { get; set; }
    }
}