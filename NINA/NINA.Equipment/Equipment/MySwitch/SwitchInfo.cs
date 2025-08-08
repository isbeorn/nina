#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NINA.Equipment.Equipment.MySwitch {

    public class SwitchInfo : DeviceInfo {
        public ReadOnlyCollection<IWritableSwitch> WritableSwitches { get; set; }
        public ReadOnlyCollection<ISwitch> ReadonlySwitches { get; set; }

        private IList<string> supportedActions;

        public IList<string> SupportedActions {
            get => supportedActions;
            set {
                supportedActions = value;
                RaisePropertyChanged();
            }
        }
    }
}