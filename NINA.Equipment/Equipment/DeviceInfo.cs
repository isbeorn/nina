#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Core.Utility;
using System;
using System.Linq;
using System.Reflection;

namespace NINA.Equipment.Equipment {

    public partial class DeviceInfo : BaseINPC {
        [ObservableProperty]
        private bool connected;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string displayName;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string driverInfo;

        [ObservableProperty]
        private string driverVersion;

        [ObservableProperty]
        private string deviceId;

        public static T CreateDefaultInstance<T>() where T : DeviceInfo, new() {
            return new T() {
                Connected = false
            };
        }

        public void Reset() {
            var defaultInstance = Activator.CreateInstance(this.GetType()) as DeviceInfo;
            defaultInstance.Connected = false;
            this.CopyFrom(defaultInstance);
        }

        public void CopyFrom(DeviceInfo other) {
            foreach (PropertyInfo property in this.GetType().GetProperties().Where(p => p.CanWrite)) {
                property.SetValue(this, property.GetValue(other, null), null);
            }
        }
    }
}