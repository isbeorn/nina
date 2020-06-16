#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using NINA.Utility.WindowService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal class Eagle : BaseINPC, ISwitchHub {

        public Eagle(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Category { get; } = "PrimaLuceLab";

        public bool HasSetupDialog {
            get => true;
        }

        public string Id {
            get => "EAGLE";
        }

        public string Name {
            get => "EAGLE";
        }

        private bool connected;

        public bool Connected {
            get => connected;
            set {
                connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description {
            get => "EAGLE";
        }

        public string DriverInfo {
            get => string.Empty;
        }

        public string DriverVersion {
            get => string.Empty;
        }

        public ICollection<ISwitch> Switches { get; private set; } = new List<ISwitch>();

        private async Task<bool> AddSwitches() {
            Logger.Trace("Scanning for EAGLE Input Power Switch");
            var inputPower = new EagleInputPower(0, BaseUrl);

            var test = await Task.Run(() => inputPower.Poll());
            if (!test) {
                return false;
            }

            Switches.Add(inputPower);

            Logger.Trace("Scanning for EAGLE USB Switches");

            var tasks = new List<Task<bool>>();
            for (short i = 0; i < 4; i++) {
                var s = new EagleUSBSwitch(i, BaseUrl);
                Switches.Add(s);
                tasks.Add(Task.Run(async () => {
                    var success = await s.Poll();
                    if (success) {
                        s.TargetValue = s.Value;
                    }
                    return success;
                }));
            }

            Logger.Trace("Scanning for EAGLE 12V Power Switches");
            for (short i = 3; i >= 0; i--) {
                var s = new Eagle12VPower(i, BaseUrl);
                Switches.Add(s);
                tasks.Add(Task.Run(async () => {
                    var success = await s.Poll();
                    if (success) {
                        s.TargetValue = s.Value;
                    }
                    return success;
                }));
            }

            Logger.Trace("Scanning for EAGLE Variable Power Switches");
            for (short i = 2; i >= 0; i--) {
                var s = new EagleVariablePower(i, BaseUrl);
                Switches.Add(s);
                tasks.Add(Task.Run(async () => {
                    var success = await s.Poll();
                    if (success) {
                        s.TargetValue = s.Value;
                    }
                    return success;
                }));
            }
            return !(await Task.WhenAll(tasks)).Contains(false);
        }

        public async Task<bool> Connect(CancellationToken token) {
            Logger.Trace("Connecting to EAGLE");
            Connected = await AddSwitches();
            if (!Connected) {
                Switches.Clear();
                Logger.Error("Unable to connect to EAGLE");
            }
            Logger.Trace("Successfully connected to EAGLE");
            return Connected;
        }

        public void Disconnect() {
            Switches.Clear();
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
            set {
                windowService = value;
            }
        }

        private IProfileService profileService;

        public string BaseUrl {
            get => profileService.ActiveProfile.SwitchSettings.EagleUrl;
            set {
                profileService.ActiveProfile.SwitchSettings.EagleUrl = value;
                RaisePropertyChanged();
            }
        }

        public void SetupDialog() {
            WindowService.Close();
            WindowService.Show(this, "EAGLE Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }
    }
}