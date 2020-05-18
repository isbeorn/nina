#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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

        public string Category { get; } = string.Empty;

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
