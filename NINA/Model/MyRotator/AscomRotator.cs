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

using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyRotator {

    internal class AscomRotator : BaseINPC, IRotator, IDisposable {

        public AscomRotator(string id, string name) {
            this.Id = id;
            this.Name = name;
        }

        private Rotator rotator;

        public bool IsMoving {
            get {
                if (Connected) {
                    return rotator.IsMoving;
                } else {
                    return false;
                }
            }
        }

        public float Position {
            get {
                if (Connected) {
                    return rotator.Position;
                } else {
                    return float.NaN;
                }
            }
        }

        public float StepSize {
            get {
                if (Connected) {
                    return rotator.StepSize;
                } else {
                    return float.NaN;
                }
            }
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Id { get; }

        public string Name { get; }

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = rotator.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblRotatorConnectionLost"]);
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
                    rotator.Connected = value;
                    _connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    _connected = false;
                }
            }
        }

        public string Description {
            get {
                return rotator.Description;
            }
        }

        public string DriverInfo {
            get {
                return rotator.DriverInfo;
            }
        }

        public string DriverVersion {
            get {
                return rotator.DriverVersion;
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                try {
                    rotator = new Rotator(Id);
                    Connected = true;
                    if (Connected) {
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("Unable to connect to rotator " + ex.Message);
                }
                return Connected;
            });
        }

        public void Disconnect() {
            Connected = false;
            Dispose();
        }

        public void Dispose() {
            rotator?.Dispose();
            rotator = null;
        }

        public void Halt() {
            if (IsMoving) {
                rotator?.Halt();
            }
        }

        public void Move(float position) {
            if (Connected) {
                rotator?.Move(position);
            }
        }

        public void MoveAbsolute(float position) {
            if (Connected) {
                rotator?.MoveAbsolute(position);
            }
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (rotator == null) {
                        rotator = new Rotator(Id);
                    }
                    rotator.SetupDialog();
                    if (dispose) {
                        rotator.Dispose();
                        rotator = null;
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }
    }
}