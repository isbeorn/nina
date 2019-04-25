#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility;
using NINA.Profile;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {

    internal class ManualFilterWheel : BaseINPC, IFilterWheel {

        public ManualFilterWheel(IProfileService profileService) {
            this.profileService = profileService;
            this.profileService.LocaleChanged += ProfileService_LocaleChanged;
        }

        private void ProfileService_LocaleChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(Description));
        }

        private bool connected;
        private IProfileService profileService;

        public bool Connected {
            get {
                return connected;
            }
            set {
                connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description {
            get {
                return Locale.Loc.Instance["LblManualFilterWheelDescription"];
            }
        }

        public string DriverInfo {
            get {
                return "n.A.";
            }
        }

        public string DriverVersion {
            get {
                return "1.0";
            }
        }

        public short InterfaceVersion {
            get {
                return 1;
            }
        }

        public int[] FocusOffsets {
            get {
                return this.Filters.Select((x) => x.FocusOffset).ToArray();
            }
        }

        public string[] Names {
            get {
                return this.Filters.Select((x) => x.Name).ToArray();
            }
        }

        private short position;

        public short Position {
            get {
                return position;
            }

            set {
                MyMessageBox.MyMessageBox.Show(
                    string.Format(Locale.Loc.Instance["LblPleaseChangeToFilter"], this.Filters[value].Name),
                    Locale.Loc.Instance["LblFilterChangeRequired"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxResult.OK);
                position = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                return this.profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            }
        }

        public ArrayList SupportedActions {
            get {
                return new ArrayList();
            }
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public string Id {
            get {
                return "Manual Filter Wheel";
            }
        }

        public string Name {
            get {
                return Locale.Loc.Instance["LblManualFilterWheel"];
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Connected = false;
        }

        public void SetupDialog() {
        }
    }
}