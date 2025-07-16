#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using System.Windows.Media;

namespace NINA.Profile.Interfaces {

    public interface IGuiderSettings : ISettings {
        string GuiderName { get; set; }
        string LastDeviceName { get; set; }
        double DitherPixels { get; set; }
        bool DitherRAOnly { get; set; }
        GuiderScaleEnum PHD2GuiderScale { get; set; }
        double MaxY { get; set; }
        int PHD2HistorySize { get; set; }
        int PHD2ServerPort { get; set; }
        string PHD2ServerUrl { get; set; }
        int PHD2InstanceNumber { get; set; }
        int SettleTime { get; set; }
        double SettlePixels { get; set; }
        int SettleTimeout { get; set; }
        string PHD2Path { get; set; }
        bool AutoRetryStartGuiding { get; set; }
        int AutoRetryStartGuidingTimeoutSeconds { get; set; }
        bool MetaGuideUseIpAddressAny { get; set; }
        int MetaGuidePort { get; set; }
        int MGENFocalLength { get; set; }
        int MGENPixelMargin { get; set; }
        int MetaGuideMinIntensity { get; set; }
        int MetaGuideDitherSettleSeconds { get; set; }
        bool MetaGuideLockWhenGuiding { get; set; }
        int PHD2ROIPct { get; set; }
        int? PHD2ProfileId { get; set; }
        int SkyGuardServerPort { get; set; }
        string SkyGuardServerUrl { get; set; }
        string SkyGuardPath { get; set; }
        int SkyGuardCallbackPort { get; set; }
        bool SkyGuardTimeLapsChecked { get; set; }
        double SkyGuardValueMaxGuiding { get; set; }
        double SkyGuardTimeLapsGuiding { get; set; }
        bool SkyGuardTimeLapsDitherChecked { get; set; }
        double SkyGuardValueMaxDithering { get; set; }
        double SkyGuardTimeLapsDithering { get; set; }
        double SkyGuardTimeOutGuiding { get; set; }
        Color GuideChartRightAscensionColor { get; set; }
        Color GuideChartDeclinationColor { get; set; }
        bool GuideChartShowCorrections { get; set; }
    }
}