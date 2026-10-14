#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.Hubs;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace NINA.WPF.Base.ViewModel.Equipment {

    public sealed partial class HubConfigurationVM : BaseINPC, IDisposable {
        private readonly IProfileService profileService;
        private readonly IDeviceHubSettings settings;
        private readonly IList<IDevice> availableDevices;

        public HubConfigurationVM(IProfileService profileService, IEquipmentHub hub,
                IDeviceHubSettings settings, IList<IDevice> availableDevices) {
            this.profileService = profileService;
            this.settings = settings;
            this.availableDevices = availableDevices.Where(device => device is not IEquipmentHub).ToList();
            Hub = hub;

            foreach (var selection in settings.Devices) {
                Rows.Add(CreateRow(selection));
            }

            if (settings is IWeatherHubSettings weatherSettings) {
                foreach (var field in Enum.GetValues<WeatherDataField>()) {
                    Fields.Add(new WeatherHubFieldVM(field, weatherSettings, UpdateConfiguration));
                }
            }

            RefreshSources();
            UpdateConfiguration();
            Hub.PropertyChanged += HubChanged;
        }

        public IEquipmentHub Hub { get; }

        public ObservableCollection<HubDeviceRowVM> Rows { get; } = new();

        public ObservableCollection<WeatherHubFieldVM> Fields { get; } = new();

        public ObservableCollection<HubSourceOption> Sources { get; } = new();

        public bool HasWeatherFields => Fields.Count > 0;

        public bool HasSafetyStatus => Hub is ISafetyMonitor;

        public bool IsSetupDialogOpen => Rows.Any(row => row.Chooser.SetupDialogOpen);

        public bool IsEditable => !Hub.IsSessionActive && !IsSetupDialogOpen;

        private HubDeviceRowVM CreateRow(HubDeviceSettings selection) {
            return new HubDeviceRowVM(profileService, selection, availableDevices, Hub, RowChanged);
        }

        [RelayCommand(CanExecute = nameof(IsEditable))]
        private void AddDevice() {
            var selection = new HubDeviceSettings {
                Id = "No_Device",
                LastDeviceName = string.Empty,
                SourceNumber = settings.Devices.Select(device => device.SourceNumber).DefaultIfEmpty(0).Max() + 1
            };
            settings.Devices.Add(selection);
            Rows.Add(CreateRow(selection));
            RefreshSources();
            UpdateConfiguration();
        }

        private bool CanRemoveDevice(HubDeviceRowVM row) => IsEditable && row != null;

        [RelayCommand(CanExecute = nameof(CanRemoveDevice))]
        private void RemoveDevice(HubDeviceRowVM row) {
            if (row == null) {
                return;
            }

            Rows.Remove(row);
            var selection = settings.Devices.FirstOrDefault(device => ReferenceEquals(device, row.Settings));
            settings.Devices.Remove(selection);
            row.Dispose();
            RefreshSources();
            ClearMissingSources();
            UpdateConfiguration();
        }

        private void RowChanged() {
            RunOnUiThread(() => {
                RaisePropertyChanged(nameof(IsSetupDialogOpen));
                RaisePropertyChanged(nameof(IsEditable));
                AddDeviceCommand.NotifyCanExecuteChanged();
                RemoveDeviceCommand.NotifyCanExecuteChanged();

                if (!Hub.IsSessionActive) {
                    RefreshSources();
                    ClearMissingSources();
                    UpdateConfiguration();
                }
            });
        }

        private void RefreshSources() {
            if (Sources.Count == 0) {
                Sources.Add(new HubSourceOption(string.Empty, Loc.Instance["LblNone"]));
            }

            var ids = Rows.Select(row => row.Settings.Id).ToHashSet();
            foreach (var source in Sources.Where(source => source.Id != string.Empty && !ids.Contains(source.Id)).ToList()) {
                Sources.Remove(source);
            }

            foreach (var row in Rows.Where(row => row.Settings.Id != "No_Device" && !string.IsNullOrEmpty(row.Settings.Id))
                    .DistinctBy(row => row.Settings.Id)) {
                var source = Sources.FirstOrDefault(source => source.Id == row.Settings.Id);
                if (source == null) {
                    Sources.Add(new HubSourceOption(row.Settings.Id, row.DisplayName));
                } else {
                    source.Name = row.DisplayName;
                }
            }
        }

        private void ClearMissingSources() {
            foreach (var field in Fields.Where(field => !Sources.Any(source => source.Id == field.SourceId))) {
                field.SourceId = string.Empty;
            }
        }

        private void UpdateConfiguration() {
            if (Hub.IsSessionActive) {
                return;
            }

            var devices = Rows.Select(row => new HubDevice(row.Settings.SourceNumber, row.Chooser.SelectedDevice)).ToList();
            foreach (var row in Rows) {
                row.IsUsed = settings is not IWeatherHubSettings weatherSettings
                    || weatherSettings.Sources.Any(source => Enum.IsDefined(source.Key) && source.Value == row.Settings.Id);
            }

            switch (Hub) {
                case SafetyMonitorHub safety:
                    safety.Configure(devices);
                    break;
                case CompositeSwitchHub switches:
                    switches.Configure(devices);
                    break;
                case WeatherDataHub weather:
                    weather.Configure(devices, ((IWeatherHubSettings)settings).Sources);
                    break;
            }
        }

        private void HubChanged(object sender, PropertyChangedEventArgs e) {
            RunOnUiThread(() => {
                RaisePropertyChanged(nameof(IsEditable));
                AddDeviceCommand.NotifyCanExecuteChanged();
                RemoveDeviceCommand.NotifyCanExecuteChanged();
                foreach (var row in Rows) {
                    row.RefreshStatus();
                }
            });
        }

        private static void RunOnUiThread(Action action) {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess()) {
                dispatcher.BeginInvoke(action);
                return;
            }

            action();
        }

        public void Dispose() {
            Hub.PropertyChanged -= HubChanged;
            foreach (var row in Rows) {
                row.Dispose();
            }
        }
    }

    public sealed partial class HubDeviceRowVM : BaseINPC, IDisposable {
        private readonly IEquipmentHub hub;
        private readonly Action changed;

        internal HubDeviceRowVM(IProfileService profileService, IHubDeviceSettings settings,
                IList<IDevice> availableDevices, IEquipmentHub hub, Action changed) {
            Settings = settings;
            IsUsed = true;
            this.hub = hub;
            this.changed = changed;
            Chooser = new HubChildChooserVM(profileService, settings, availableDevices);
            Chooser.PropertyChanged += ChooserChanged;
        }

        public IHubDeviceSettings Settings { get; }

        public HubChildChooserVM Chooser { get; }

        [ObservableProperty]
        public partial bool IsUsed { get; internal set; }

        public string DisplayName => $"{Settings.SourceNumber}: {Chooser.SelectedDevice?.DisplayName}";

        public HubDeviceStatus Status => hub.DeviceStatuses.FirstOrDefault(status => status.SourceNumber == Settings.SourceNumber);

        private void ChooserChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(Chooser.SelectedDevice)) {
                Settings.Id = Chooser.SelectedDevice?.Id ?? "No_Device";
                Settings.LastDeviceName = Chooser.SelectedDevice?.DisplayName ?? string.Empty;
                RaisePropertyChanged(nameof(DisplayName));
                changed();
            } else if (e.PropertyName == nameof(Chooser.SetupDialogOpen)) {
                changed();
            }
        }

        internal void RefreshStatus() {
            RaisePropertyChanged(nameof(Status));
        }

        public void Dispose() {
            Chooser.PropertyChanged -= ChooserChanged;
        }
    }

    public sealed class HubChildChooserVM : DeviceChooserVM<IDevice> {
        private readonly IHubDeviceSettings settings;
        private readonly IList<IDevice> availableDevices;

        internal HubChildChooserVM(IProfileService profileService, IHubDeviceSettings settings,
                IList<IDevice> availableDevices) : base(profileService, null) {
            this.settings = settings;
            this.availableDevices = availableDevices;
            GetEquipment().GetAwaiter().GetResult();
        }

        public override Task GetEquipment() {
            var devices = availableDevices.Where(device => device is not IEquipmentHub).ToList();
            if (!devices.Any(device => device.Id == "No_Device")) {
                devices.Insert(0, new DummyDevice(Loc.Instance["LblNone"]));
            }

            DetermineSelectedDevice(devices, string.IsNullOrEmpty(settings.Id) ? "No_Device" : settings.Id, settings.LastDeviceName);
            return Task.CompletedTask;
        }
    }

    public sealed partial class HubSourceOption : BaseINPC {

        internal HubSourceOption(string id, string name) {
            Id = id;
            Name = name;
        }

        public string Id { get; }

        [ObservableProperty]
        public partial string Name { get; set; }
    }

    public sealed class WeatherHubFieldVM : BaseINPC {
        private readonly IWeatherHubSettings settings;
        private readonly Action changed;

        internal WeatherHubFieldVM(WeatherDataField field, IWeatherHubSettings settings, Action changed) {
            Field = field;
            this.settings = settings;
            this.changed = changed;
        }

        public WeatherDataField Field { get; }

        public string Name => Loc.Instance[Field switch {
            WeatherDataField.AveragePeriod => "LblHubAveragePeriod",
            WeatherDataField.DewPoint => "LblDewpoint",
            _ => $"Lbl{Field}"
        }];

        public string SourceId {
            get => settings.Sources.TryGetValue(Field, out var id) ? id : string.Empty;
            set {
                if (value == null) {
                    return;
                }

                if (SourceId == value) {
                    return;
                }

                settings.SetSource(Field, value);
                RaisePropertyChanged();
                changed();
            }
        }
    }
}
