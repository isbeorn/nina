#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.Profile;
using System;

namespace NINA.ViewModel.Equipment.Focuser {

    internal class FocuserChooserVM : EquipmentChooserVM {

        public FocuserChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoFocuser"]));

            try {
                foreach (IFocuser focuser in ASCOMInteraction.GetFocusers(profileService)) {
                    Devices.Add(focuser);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            Devices.Add(new UltimatePowerboxV2(profileService));

            DetermineSelectedDevice(profileService.ActiveProfile.FocuserSettings.Id);
        }
    }
}