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

    public interface IFocuserSettings : ISettings {
        double AutoFocusExposureTime { get; set; }
        int AutoFocusInitialOffsetSteps { get; set; }
        int AutoFocusStepSize { get; set; }
        string Id { get; set; }
        bool UseFilterWheelOffsets { get; set; }
        bool AutoFocusDisableGuiding { get; set; }
        int FocuserSettleTime { get; set; }
        int AutoFocusTotalNumberOfAttempts { get; set; }
        int AutoFocusNumberOfFramesPerPoint { get; set; }
        double AutoFocusInnerCropRatio { get; set; }
        double AutoFocusOuterCropRatio { get; set; }
        int AutoFocusUseBrightestStars { get; set; }
        int BacklashIn { get; set; }
        int BacklashOut { get; set; }
        short AutoFocusBinning { get; set; }
        AFCurveFittingEnum AutoFocusCurveFitting { get; set; }
        AFMethodEnum AutoFocusMethod { get; set; }
        ContrastDetectionMethodEnum ContrastDetectionMethod { get; set; }
        BacklashCompensationModel BacklashCompensationModel { get; set; }
    }
}
