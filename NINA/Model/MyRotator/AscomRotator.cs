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
using NINA.Utility.Astrometry;
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

        public string Category { get; } = "ASCOM";

        public bool CanReverse {
            get {
                if (Connected) {
                    return rotator.CanReverse;
                } else {
                    return false;
                }
            }
        }

        public bool Reverse {
            get {
                if (Connected) {
                    return rotator.Reverse;
                } else {
                    return false;
                }
            }
            set {
                if (CanReverse) {
                    rotator.Reverse = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsMoving {
            get {
                if (Connected) {
                    return rotator.IsMoving;
                } else {
                    return false;
                }
            }
        }

        private float position = 0;

        public float Position {
            get => position;
            private set {
                position = Astrometry.EuclidianModulus(value, 360);
                RaisePropertyChanged();
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
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowWarning(Locale.Loc.Instance["LblRotatorConnectionLost"]);
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
                    Position = 0;
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

        public void Sync(float skyAngle) {
            Position = skyAngle;
        }

        public void Move(float position) {
            if (Connected) {
                rotator?.Move(position);
                Position += position;
            }
        }

        public void MoveAbsolute(float targetPosition) {
            if (Connected) {
                Move(targetPosition - Position);
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
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }
    }
}