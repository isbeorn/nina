#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment {

    public class DummyDevice : IDevice {

        public DummyDevice(string name) {
            Name = name;
        }

        public bool HasSetupDialog => false;

        public string Category { get; } = string.Empty;

        public string Id => "No_Device";

        public string Name { get; private set; }
        public string DisplayName => Name;

        public bool Connected => false;

        public string Description => string.Empty;

        public string DriverInfo => string.Empty;

        public string DriverVersion => string.Empty;

        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => false, token);
        }

        public void Disconnect() {
        }

        public void SetupDialog() {
        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}