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

    public interface IGuiderSettings : ISettings {
        string GuiderName { get; set; }
        double DitherPixels { get; set; }
        bool DitherRAOnly { get; set; }
        GuiderScaleEnum PHD2GuiderScale { get; set; }
        double MaxY { get; set; }
        int PHD2HistorySize { get; set; }
        int PHD2ServerPort { get; set; }
        string PHD2ServerUrl { get; set; }
        int SettleTime { get; set; }
        double SettlePixels { get; set; }
        int SettleTimeout { get; set; }
        double DirectGuideDuration { get; set; }
        string PHD2Path { get; set; }
        bool AutoRetryStartGuiding { get; set; }
        int AutoRetryStartGuidingTimeoutSeconds { get; set; }
        int MGENFocalLength { get; set; }
    }
}