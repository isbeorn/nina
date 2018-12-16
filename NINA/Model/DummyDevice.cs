#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model {

    public class DummyDevice : IDevice {

        public DummyDevice(string name) {
            Name = name;
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public string Id {
            get {
                return "No_Device";
            }
        }

        private string _name;

        public string Name {
            get {
                return _name;
            }
            private set {
                _name = value;
            }
        }

        public bool Connected {
            get {
                return false;
            }
        }

        public string Description {
            get {
                return string.Empty;
            }
        }

        public string DriverInfo {
            get {
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                return string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => false);
        }

        public void Disconnect() {
            return;
        }

        public void SetupDialog() {
        }
    }
}