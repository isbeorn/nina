#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.Input;

namespace NINA.WPF.Base.ViewModel.Equipment {

    public abstract partial class DeviceChooserVM<T> : BaseVM, IDeviceChooserVM where T : IDevice {

        public DeviceChooserVM(
                IProfileService profileService,
                IEquipmentProviders<T> equipmentProviders) : base(profileService) {
            this.profileService = profileService;
            this.equipmentProviders = equipmentProviders;
            this.Devices = new List<IDevice>();
        }

        protected SemaphoreSlim lockObj = new SemaphoreSlim(1,1);
        protected readonly IEquipmentProviders<T> equipmentProviders;

        private IList<IDevice> devices;

        public IList<IDevice> Devices {
            get => devices;
            protected set {
                devices = value;
                RaisePropertyChanged();
            }
        }

        public abstract Task GetEquipment();

        private IDevice _selectedDevice;

        public IDevice SelectedDevice {
            get {
                lock (lockObj) {
                    return _selectedDevice;
                }
            }
            set {
                lock (lockObj) {
                    _selectedDevice = value;
                    RaisePropertyChanged();
                }
            }
        }

        
        [RelayCommand]
        private async Task SetupDialog() {
            if (SelectedDevice?.HasSetupDialog == true) {
                await Task.Run(() => {
                    Thread thread = new Thread(() => {
                        SelectedDevice.SetupDialog();
                    });

                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                });
            }
        }

        protected void DetermineSelectedDevice(IList<IDevice> d, string id) {
            if (d.Count > 0) {
                var items = (from device in d where device.Id == id select device);
                if (items.Any()) {
                    SelectedDevice = items.First();
                } else {
                    SelectedDevice = d.First();
                }
            }
            Devices = d;
        }
    }
}