#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NINA.Profile.Interfaces {

    public interface IFlatDeviceSettings : ISettings {
        string Id { get; set; }
        string LastDeviceName { get; set; }
        string Name { get; set; }
        string PortName { get; set; }
        int SettleTime { get; set; }
        ObserveAllCollection<TrainedFlatExposureSetting> TrainedFlatExposureSettings { get; set; }
        bool RemoveFlatExposureSetting(TrainedFlatExposureSetting setting);
        TrainedFlatExposureSetting GetTrainedFlatExposureSetting(short? filterPosition, BinningMode binning, int gain, int offset);
        void AddEmptyTrainedExposureSetting();
        void AddTrainedFlatExposureSetting(short? filterPosition, BinningMode binning, int gain, int offset, int brightness, double exposureTime);
    }
}