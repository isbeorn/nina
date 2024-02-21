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
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.WPF.Base.Model.Equipment.MySafetyMonitor.Simulator {

    public class SafetyMonitorSimulator : BaseINPC, ISafetyMonitor {
        public bool IsSafe { get; set; }

        public bool HasSetupDialog => true;

        public string Id => "613EC0FF-87D7-4475-9352-F6F6EB1CDE75";

        public string Name => "N.I.N.A. Simulator Safety Device";

        public string DisplayName => Name;

        public string Category => "N.I.N.A.";

        public bool Connected { get; private set; }

        public string Description => string.Empty;

        public string DriverInfo => string.Empty;

        public string DriverVersion => string.Empty;

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Connected = false;
        }

        private IWindowService windowService;

        public IWindowService WindowService {
            get {
                if (windowService == null) {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set => windowService = value;
        }

        public void SetupDialog() {
            WindowService.Show(this, "Simulator Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
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