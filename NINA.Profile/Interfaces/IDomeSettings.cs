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

namespace NINA.Profile.Interfaces {

    public interface IDomeSettings : ISettings {
        string Id { get; set; }
        double ScopePositionEastWest_mm { get; set; }
        double ScopePositionNorthSouth_mm { get; set; }
        double ScopePositionUpDown_mm { get; set; }
        double DomeRadius_mm { get; set; }
        double GemAxis_mm { get; set; }
        double LateralAxis_mm { get; set; }
        double AzimuthTolerance_degrees { get; set; }
        bool FindHomeBeforePark { get; set; }
        int DomeSyncTimeoutSeconds { get; set; }
        bool SynchronizeDuringMountSlew { get; set; }
        bool SyncSlewDomeWhenMountSlews { get; set; }
        double RotateDegrees { get; set; }
        bool CloseOnUnsafe { get; set; }
        bool ParkMountBeforeShutterMove { get; set; }
        bool RefuseUnsafeShutterMove { get; set; }
        bool RefuseUnsafeShutterOpenSansSafetyDevice { get; set; }
        bool ParkDomeBeforeShutterMove { get; set; }
        MountTypeEnum MountType { get; set; }
        double DecOffsetHorizontal_mm { get; set; }
        int SettleTimeSeconds { get; set; }
    }
}