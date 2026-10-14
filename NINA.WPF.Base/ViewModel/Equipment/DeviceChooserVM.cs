#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.ComponentModel;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Core.Locale;
using System.Windows;
using NINA.Profile.Interfaces;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;

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

        private IProfile hubProfile;
        private bool hubSetupDialogOpen;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HubSelected), nameof(HasSetupDialog))]
        public partial HubConfigurationVM HubConfiguration { get; private set; }

        partial void OnHubConfigurationChanging(HubConfigurationVM oldValue, HubConfigurationVM newValue) {
            if (oldValue != null) {
                oldValue.PropertyChanged -= HubConfigurationChanged;
                oldValue.Hub.PropertyChanged -= HubSessionChanged;
                oldValue.Dispose();
            }
        }

        partial void OnHubConfigurationChanged(HubConfigurationVM value) {
            hubProfile = value == null ? null : ActiveProfile;
            if (value != null) {
                value.PropertyChanged += HubConfigurationChanged;
                value.Hub.PropertyChanged += HubSessionChanged;
            }
        }

        public bool HubSelected => HubConfiguration != null && ReferenceEquals(SelectedDevice, HubConfiguration.Hub);

        public bool HasSetupDialog => HubSelected || SelectedDevice?.HasSetupDialog == true;

        protected void AddHub(IList<IDevice> devices, IEquipmentHub hub, object settings) {
            HubConfiguration = settings is IHasDeviceHubSettings hubSettings
                ? new HubConfigurationVM(profileService, hub, hubSettings.HubSettings, devices)
                : null;
            if (HubConfiguration != null) {
                devices.Add(hub);
            }
        }

        private void HubSessionChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != nameof(IEquipmentHub.IsSessionActive) || HubConfiguration?.Hub.IsSessionActive != false
                || ReferenceEquals(hubProfile, ActiveProfile)) {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess()) {
                // Finish rebinding before Disconnect returns and an immediate reconnect can start.
                dispatcher.Invoke(RefreshHubProfile);
            } else {
                RefreshHubProfile();
            }
        }

        private void RefreshHubProfile() {
            if (HubConfiguration?.Hub.IsSessionActive != false || ReferenceEquals(hubProfile, ActiveProfile) || ActiveProfile == null) {
                return;
            }

            var hub = HubConfiguration.Hub;
            object settings;
            string id;
            string name;
            if (hub is SafetyMonitorHub) {
                var safety = ActiveProfile.SafetyMonitorSettings;
                settings = safety;
                id = safety.Id;
                name = safety.LastDeviceName;
                hub = new SafetyMonitorHub(Loc.Instance["LblSafetyMonitorHub"]);
            } else if (hub is CompositeSwitchHub) {
                var switches = ActiveProfile.SwitchSettings;
                settings = switches;
                id = switches.Id;
                name = switches.LastDeviceName;
                hub = new CompositeSwitchHub(Loc.Instance["LblSwitchHub"]);
            } else if (hub is WeatherDataHub) {
                var weather = ActiveProfile.WeatherDataSettings;
                settings = weather;
                id = weather.Id;
                name = weather.LastDeviceName;
                hub = new WeatherDataHub(Loc.Instance["LblWeatherDataHub"]);
            } else {
                return;
            }

            var devices = Devices.Where(device => device is not IEquipmentHub).ToList();
            AddHub(devices, hub, settings);
            DetermineSelectedDevice(devices, id, name);
        }

        private void HubConfigurationChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(HubConfiguration.IsSetupDialogOpen) && !hubSetupDialogOpen) {
                SetupDialogOpen = HubConfiguration.IsSetupDialogOpen;
            }
        }

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
                    RaisePropertyChanged(nameof(HubSelected));
                    RaisePropertyChanged(nameof(HasSetupDialog));
                }
            }
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SetupDialogCommand))]
        [NotifyPropertyChangedFor(nameof(SetupDialogNotOpen))]
        private bool setupDialogOpen;
        
        public bool SetupDialogNotOpen => !SetupDialogOpen;

        [RelayCommand(CanExecute = nameof(SetupDialogNotOpen))]
        private async Task SetupDialog() {
            if (HubSelected) {
                var configuration = HubConfiguration;
                if (configuration.Hub.IsSessionActive) {
                    return;
                }

                hubSetupDialogOpen = true;
                SetupDialogOpen = true;
                try {
                    await new WindowService().ShowDialog(configuration,
                        $"{configuration.Hub.Name} - {Loc.Instance["LblSettings"]}",
                        ResizeMode.NoResize, WindowStyle.ToolWindow);
                } finally {
                    hubSetupDialogOpen = false;
                    SetupDialogOpen = configuration.IsSetupDialogOpen;
                }

                return;
            }

            if (SelectedDevice?.HasSetupDialog == true) {
                SetupDialogOpen = true;
                try {
                    await Task.Run(() => {
                        Thread thread = new Thread(() => {
                            SelectedDevice.SetupDialog();
                        });

                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        thread.Join();
                    });
                } finally {
                    SetupDialogOpen = false;
                }                
            }
        }

        protected void DetermineSelectedDevice(IList<IDevice> d, string id, string name) {
            if (d.Count > 0) {
                var items = (from device in d where device.Id == id select device);
                if (items.Any()) {
                    SelectedDevice = items.First();
                } else {
                    var offlineDevice = new OfflineDevice(id, name);
                    d.Insert(0, offlineDevice);
                    SelectedDevice = offlineDevice;
                }
            }
            Devices = d;
        }
    }
}