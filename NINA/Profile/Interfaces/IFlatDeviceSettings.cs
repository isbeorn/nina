#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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

        IEnumerable<short> GetBrightnessInfoGains();

        void ClearBrightnessInfo();
    }
}