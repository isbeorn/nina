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