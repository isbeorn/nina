#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FLI;
using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Profile;
using QHYCCD;
using System;
using System.Collections.Generic;

namespace NINA.ViewModel.Equipment.FilterWheel {

    internal class FilterWheelChooserVM : EquipmentChooserVM {

        public FilterWheelChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoFilterwheel"]));

            /*
             * FLI
             */
            try {
                Logger.Trace("Adding FLI filter wheels");
                List<string> fwheels = FLIFilterWheels.GetFilterWheels();

                if (fwheels.Count > 0) {
                    foreach (var entry in fwheels) {
                        var fwheel = new FLIFilterWheel(entry, profileService);

                        if (!string.IsNullOrEmpty(fwheel.Name)) {
                            Logger.Debug($"Adding FLI Filter Wheel {fwheel.Id} (as {fwheel.Name})");
                            Devices.Add(fwheel);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /*
             * QHY - Integrated or 4-pin connected filter wheels only
             */
            try {
                Logger.Trace("Adding QHY integrated/4-pin filter wheels");
                List<string> fwheels = QHYFilterWheels.GetFilterWheels();

                if (fwheels.Count > 0) {
                    foreach (var entry in fwheels) {
                        var fwheel = new QHYFilterWheel(entry, profileService);

                        if (!string.IsNullOrEmpty(fwheel.Name)) {
                            Logger.Debug($"Adding QHY Filter Wheel {fwheel.Id} (as {fwheel.Name})");
                            Devices.Add(fwheel);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /*
             * ASCOM devices
             */
            try {
                foreach (IFilterWheel fw in ASCOMInteraction.GetFilterWheels(profileService)) {
                    Devices.Add(fw);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            Devices.Add(new ManualFilterWheel(this.profileService));

            DetermineSelectedDevice(profileService.ActiveProfile.FilterWheelSettings.Id);
        }
    }
}