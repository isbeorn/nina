#region "copyright"

/*
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

/*
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyWeatherData;
using NINA.Utility;
using NINA.Profile;
using System;

namespace NINA.ViewModel.Equipment.WeatherData {

    internal class WeatherDataChooserVM : EquipmentChooserVM {

        public WeatherDataChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblWeatherNoSource"]));

            try {
                foreach (IWeatherData obsdev in ASCOMInteraction.GetWeatherDataSources(profileService)) {
                    Devices.Add(obsdev);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            Devices.Add(new OpenWeatherMap(this.profileService));

            DetermineSelectedDevice(profileService.ActiveProfile.WeatherDataSettings.Id);
        }
    }
}