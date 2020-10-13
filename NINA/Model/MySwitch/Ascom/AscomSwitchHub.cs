#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowWarning(Locale.Loc.Instance["LblSwitchConnectionLost"]);
                        try {
                            Disconnect();
                        } catch (Exception disconnectEx) {
                            Logger.Error(disconnectEx);
                        }
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
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }
    }
}