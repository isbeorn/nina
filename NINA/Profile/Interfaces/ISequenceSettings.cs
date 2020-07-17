#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Profile {

    public interface ISequenceSettings : ISettings {
        TimeSpan EstimatedDownloadTime { get; set; }
        string TemplatePath { get; set; }
        long TimeSpanInTicks { get; set; }
        bool ParkMountAtSequenceEnd { get; set; }
        bool WarmCamAtSequenceEnd { get; set; }
        string DefaultSequenceFolder { get; set; }
        string SequenceCompleteCommand { get; set; }
    }
}