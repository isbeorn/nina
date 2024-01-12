#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    public class ManualFilterWheel : BaseINPC, IFilterWheel {

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
            get => connected;
            set {
                connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description => Loc.Instance["LblManualFilterWheelDescription"];

        public string DriverInfo => "n.A.";

        public string DriverVersion => "1.0";

        public short InterfaceVersion => 1;

        public int[] FocusOffsets => this.Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => this.Filters.Select((x) => x.Name).ToArray();

        private short position;

        public short Position {
            get => position;

            set {
                MyMessageBox.Show(
                    string.Format(Loc.Instance["LblPleaseChangeToFilter"], this.Filters[value].Name),
                    Loc.Instance["LblFilterChangeRequired"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxResult.OK);
                position = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<FilterInfo> Filters => this.profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;

        public IList<string> SupportedActions => new List<string>();

        public bool HasSetupDialog => false;

        public string Id => "Manual Filter Wheel";

        public string Name => Loc.Instance["LblManualFilterWheel"];

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            if (Filters.Count == 0) {
                var filter = new FilterInfo(Loc.Instance["LblFilter"] + 1, 0, 0, -1, new BinningMode(1, 1), -1, -1);
                profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Add(filter);
                RaisePropertyChanged(nameof(Filters));
            }
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Connected = false;
        }

        public void SetupDialog() {
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}