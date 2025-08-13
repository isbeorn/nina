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

    public interface ITelescopeSettings : ISettings {
        string Name { get; set; }
        string MountName { get; set; }
        double FocalLength { get; set; }
        double FocalRatio { get; set; }
        string Id { get; set; }
        string LastDeviceName { get; set; }
        int SettleTime { get; set; }
        string SnapPortStart { get; set; }
        string SnapPortStop { get; set; }
        bool NoSync { get; set; }
        bool TimeSync { get; set; }
        bool PrimaryReversed { get; set; }
        bool SecondaryReversed { get; set; }

        TelescopeLocationSyncDirection TelescopeLocationSyncDirection { get; set; }
    }
}