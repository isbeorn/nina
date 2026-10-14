#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace NINA.Profile {
    [Serializable]
    [DataContract]
    [KnownType(typeof(WeatherHubSettings))]
    public class DeviceHubSettings : Settings, IDeviceHubSettings {
        private ObservableCollection<HubDeviceSettings> devices;
        [NonSerialized]
        private HashSet<HubDeviceSettings> subscribedDevices;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            Devices = new ObservableCollection<HubDeviceSettings>();
        }

        [DataMember]
        public ObservableCollection<HubDeviceSettings> Devices {
            get => devices;
            set {
                if (ReferenceEquals(devices, value)) {
                    return;
                }

                if (devices != null) {
                    devices.CollectionChanged -= DevicesChanged;
                }
                if (subscribedDevices != null) {
                    foreach (var device in subscribedDevices) {
                        device.PropertyChanged -= DeviceChanged;
                    }
                }

                devices = value ?? new ObservableCollection<HubDeviceSettings>();
                subscribedDevices = new HashSet<HubDeviceSettings>();
                devices.CollectionChanged += DevicesChanged;
                UpdateSubscriptions();
                RaisePropertyChanged();
            }
        }

        private void DevicesChanged(object sender, NotifyCollectionChangedEventArgs e) {
            UpdateSubscriptions();
            RaisePropertyChanged(nameof(Devices));
        }

        private void UpdateSubscriptions() {
            var current = new HashSet<HubDeviceSettings>(devices);
            foreach (var device in subscribedDevices) {
                if (!current.Contains(device)) {
                    device.PropertyChanged -= DeviceChanged;
                }
            }
            foreach (var device in current) {
                if (!subscribedDevices.Contains(device)) {
                    device.PropertyChanged += DeviceChanged;
                }
            }

            subscribedDevices = current;
        }

        private void DeviceChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged(nameof(Devices));
        }
    }
}
