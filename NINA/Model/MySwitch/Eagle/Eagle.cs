#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
            get => "Eagle";
        }

        public string Name {
            get => "Eagle";
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
            get => "Eagle";
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