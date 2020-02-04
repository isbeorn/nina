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

using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal class AscomSwitchHub : BaseINPC, ISwitchHub, IDisposable {
        private Switch ascomSwitchHub;

        public AscomSwitchHub(string id, string name) {
            this.Id = id;
            this.Name = name;
        }

        public string Category { get; } = "ASCOM";

        public bool HasSetupDialog {
            get => true;
        }

        public string Id { get; }

        public string Name { get; }

        private bool connected;

        public bool Connected {
            get {
                if (connected) {
                    bool val = false;
                    try {
                        val = ascomSwitchHub.Connected;
                        if (connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblSwitchConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception) {
                        Disconnect();
                    }
                    return val;
                } else {
                    return false;
                }
            }
            private set {
                try {
                    ascomSwitchHub.Connected = value;
                    connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    connected = false;
                }
            }
        }

        public string Description {
            get {
                return ascomSwitchHub.Description;
            }
        }

        public string DriverInfo {
            get {
                return ascomSwitchHub.DriverInfo;
            }
        }

        public string DriverVersion {
            get {
                return ascomSwitchHub.DriverVersion;
            }
        }

        public ICollection<ISwitch> Switches { get; private set; } = new AsyncObservableCollection<ISwitch>();

        private async Task ScanForSwitches() {
            Logger.Trace("Scanning for Ascom Switches");
            var numberOfSwitches = ascomSwitchHub.MaxSwitch;
            for (short i = 0; i < numberOfSwitches; i++) {
                try {
                    var canWrite = ascomSwitchHub.CanWrite(i);

                    if (canWrite) {
                        Logger.Trace($"Writable Switch found for index {i}");
                        var s = new AscomWritableSwitch(ascomSwitchHub, i);
                        Switches.Add(s);
                    } else {
                        Logger.Trace($"Readable Switch found for index {i}");
                        var s = new AscomSwitch(ascomSwitchHub, i);
                        Switches.Add(s);
                    }
                } catch (ASCOM.MethodNotImplementedException) {
                    //ISwitchV1 Fallbacks
                    try {
                        var s = new AscomWritableV1Switch(ascomSwitchHub, i);
                        s.TargetValue = s.Value;
                        await s.SetValue();
                        Switches.Add(s);
                    } catch (Exception) {
                        var s = new AscomV1Switch(ascomSwitchHub, i);
                        Switches.Add(s);
                    }
                }
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(async () => {
                try {
                    Logger.Trace("Successfully connected to Ascom Switch Hub");
                    ascomSwitchHub = new Switch(Id);
                    Connected = true;

                    await ScanForSwitches();

                    if (Connected) {
                        Logger.Trace("Successfully connected to Ascom Switch Hub");
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Connected = false;
                    Logger.Error(ex);
                }
                return Connected;
            });
        }

        public void Disconnect() {
            Switches.Clear();
            Connected = false;
            Dispose();
        }

        public void Dispose() {
            ascomSwitchHub?.Dispose();
            ascomSwitchHub = null;
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (ascomSwitchHub == null) {
                        ascomSwitchHub = new Switch(Id);
                        dispose = true;
                    }

                    ascomSwitchHub.SetupDialog();
                    if (dispose) {
                        ascomSwitchHub.Dispose();
                        ascomSwitchHub = null;
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }
    }
}