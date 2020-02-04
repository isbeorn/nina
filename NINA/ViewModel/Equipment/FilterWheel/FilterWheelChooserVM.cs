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