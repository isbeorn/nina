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

namespace NINA.Model {

    public class DeviceInfo : BaseINPC {
        private bool connected;
        public bool Connected { get { return connected; } set { connected = value; RaisePropertyChanged(); } }
        private string name;
        public string Name { get { return name; } set { name = value; RaisePropertyChanged(); } }

        private string description;

        public string Description {
            get { return description; }
            set { description = value; RaisePropertyChanged(); }
        }

        private string driverInfo;

        public string DriverInfo {
            get { return driverInfo; }
            set { driverInfo = value; RaisePropertyChanged(); }
        }

        private string driverVersion;

        public string DriverVersion {
            get { return driverVersion; }
            set { driverVersion = value; RaisePropertyChanged(); }
        }

        public static T CreateDefaultInstance<T>() where T : DeviceInfo, new() {
            return new T() {
                Connected = false
            };
        }
    }
}