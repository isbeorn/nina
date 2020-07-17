#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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

        public string Category { get; } = "N.I.N.A.";

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