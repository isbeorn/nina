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
using System.Linq;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal abstract class EquipmentChooserVM : BaseVM {

        public EquipmentChooserVM(Type equipmentType, IProfileService profileService) : base(profileService) {
            this.profileService = profileService;
            SetupDialogCommand = new RelayCommand(OpenSetupDialog);
            GetEquipment();
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