#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Image.ImageAnalysis;
using System.Collections.Generic;
using System.ComponentModel;

namespace NINA.Image.Interfaces {

    public interface IStarDetectionAnalysis : INotifyPropertyChanged {
        double HFR { get; set; }
        double HFRStDev { get; set; }
        int DetectedStars { get; set; }
        List<DetectedStar> StarList { get; set; }
    }
}