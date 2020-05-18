#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Enum;

namespace NINA.Profile {

    public interface IImageSettings : ISettings {
        bool AnnotateImage { get; set; }
        bool DebayerImage { get; set; }
        bool DebayeredHFR { get; set; }
        bool UnlinkedStretch { get; set; }
        double AutoStretchFactor { get; set; }
        double BlackClipping { get; set; }
        StarSensitivityEnum StarSensitivity { get; set; }
        NoiseReductionEnum NoiseReduction { get; set; }
        string SharpCapSensorAnalysisFolder { get; set; }
        bool DetectStars { get; set; }
        bool AutoStretch { get; set; }
    }
}
