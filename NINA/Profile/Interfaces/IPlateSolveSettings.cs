#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyFilterWheel;
using NINA.Utility.Enum;

namespace NINA.Profile {

    public interface IPlateSolveSettings : ISettings {
        string AstrometryURL { get; set; }
        string AstrometryAPIKey { get; set; }
        BlindSolverEnum BlindSolverType { get; set; }
        string CygwinLocation { get; set; }
        double ExposureTime { get; set; }
        int Gain { get; set; }
        short Binning { get; set; }
        FilterInfo Filter { get; set; }
        PlateSolverEnum PlateSolverType { get; set; }
        string PS2Location { get; set; }
        int Regions { get; set; }
        double SearchRadius { get; set; }
        double Threshold { get; set; }
        double RotationTolerance { get; set; }
        double ReattemptDelay { get; set; }
        int NumberOfAttempts { get; set; }
        string AspsLocation { get; set; }
        string ASTAPLocation { get; set; }
        int DownSampleFactor { get; set; }
        int MaxObjects { get; set; }
        bool Sync { get; set; }
        bool SlewToTarget { get; set; }
    }
}
