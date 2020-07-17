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
using System.Linq;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal abstract class EquipmentChooserVM : BaseVM {

        public EquipmentChooserVM(IProfileService profileService) : base(profileService) {
            this.profileService = profileService;
            SetupDialogCommand = new RelayCommand(OpenSetupDialog);
        }

        private AsyncObservableCollection<Model.IDevice> _devices;

        public AsyncObservableCollection<Model.IDevice> Devices {
            get {
                if (_devices == null) {
                    _devices = new AsyncObservableCollection<Model.IDevice>();
                }
                return _devices;
            }
            set {
                _devices = value;
            }
        }

        public abstract void GetEquipment();

        private Model.IDevice _selectedDevice;

        public Model.IDevice SelectedDevice {
            get {
                return _selectedDevice;
            }
            set {
                _selectedDevice = value;
                RaisePropertyChanged();
            }
        }

        public ICommand SetupDialogCommand { get; private set; }

        private void OpenSetupDialog(object o) {
            if (SelectedDevice?.HasSetupDialog == true) {
                SelectedDevice.SetupDialog();
            }
        }

        public void DetermineSelectedDevice(string id) {
            if (Devices.Count > 0) {
                var items = (from device in Devices where device.Id == id select device);
                if (items.Count() > 0) {
                    SelectedDevice = items.First();
                } else {
                    SelectedDevice = Devices.First();
                }
            }
        }
    }
}