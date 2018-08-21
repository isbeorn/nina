using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    Notification.ShowError(ex.Message);
                } catch (Exception ex) {
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