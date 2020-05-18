#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using System.Collections.Generic;

namespace NINA.Profile {

    public interface IFlatDeviceSettings : ISettings {
        string Id { get; set; }
        string Name { get; set; }
        string PortName { get; set; }
        bool OpenForDarkFlats { get; set; }
        bool CloseAtSequenceEnd { get; set; }
        bool UseWizardTrainedValues { get; set; }

        Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> FilterSettings { get; set; }

        void AddBrightnessInfo(FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value);

        FlatDeviceFilterSettingsValue GetBrightnessInfo(FlatDeviceFilterSettingsKey key);

        IEnumerable<BinningMode> GetBrightnessInfoBinnings();

        IEnumerable<int> GetBrightnessInfoGains();

        void ClearBrightnessInfo();
    }
}
