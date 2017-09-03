using ASCOM;
using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {
    class AscomFocuser : BaseINPC, IFocuser, IDisposable {

        public AscomFocuser(string focuserwheel, string name) {
            Id = focuserwheel;
            Name = name;
        }

        private Focuser _focuser;

        private string _id;
        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;
        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }
        
        public bool IsMoving {
            get {
                if(Connected) {
                    return _focuser.IsMoving;
                } else {
                    return false;
                }                
            }
        }

        public int MaxIncrement {
            get {
                if(Connected) {
                    return _focuser.MaxIncrement;
                } else {
                    return -1;
                }                
            }
        }

        public int MaxStep {
            get {
                if(Connected) {
                    return _focuser.MaxStep;
                } else {
                    return -1;
                }                
            }
        }

        private bool _canGetPosition;
        public int Position {
            get {
                int pos = -1;
                try {
                    if (Connected && _canGetPosition) {
                        pos = _focuser.Position;
                    }
                } catch(PropertyNotImplementedException) {
                    _canGetPosition = false;
                }
                return pos;
            }
        }

        private bool _canGetStepSize;
        public double StepSize {
            get {
                double stepSize = double.NaN;
                try {
                    if (Connected && _canGetStepSize) {
                        stepSize = _focuser.StepSize;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetStepSize = false;
                }
                return stepSize;
            }
        }

        public bool TempCompAvailable {
            get {
                if(Connected) {
                    return _focuser.TempCompAvailable;
                } else {
                    return false;
                }                
            }
        }

        public bool TempComp {
            get {
                if(Connected) {
                    return _focuser.TempComp;
                } else {
                    return false;
                }                
            }
            set {
                if(Connected && _focuser.TempCompAvailable) {
                    _focuser.TempComp = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _hasTempearture;
        public double Temperature {
            get {
                double temperature = double.NaN;
                try {
                    if (Connected && _hasTempearture) {
                        temperature = _focuser.Temperature;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasTempearture = false;
                }
                return temperature;
            }
        }

        private bool _connected;
        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _focuser.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning("Filter wheel connection lost! Please reconnect focuser!");
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
                    _focuser.Connected = value;
                    _connected = value;                    

                } catch (Exception ex) {
                    Notification.ShowError(ex.Message + "\n Please reconnect focuser!");
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }

        public void Move(int position) {
            if(Connected && !TempComp) {                
                _focuser.Move(position);
            }
        }

        private bool _canHalt;
        public void Halt() {
            if (Connected && _canHalt) {
                try {
                    _focuser.Halt();
                } catch(MethodNotImplementedException) {
                    _canHalt = false;
                } catch(Exception ex) {
                    Logger.Error(ex.Message);
                }                
            }
        }


        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Description {
            get {
                if(Connected) {
                    return _focuser.Description;
                } else {
                    return string.Empty;
                }
            }
        }

        public void UpdateValues() {
            RaisePropertyChanged(nameof(Position));
            RaisePropertyChanged(nameof(Temperature));
            RaisePropertyChanged(nameof(IsMoving));
            RaisePropertyChanged(nameof(TempComp));
        }

        public void Dispose() {
            _focuser?.Dispose();
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (_focuser == null) {
                        _focuser = new Focuser(Id);
                    }
                    _focuser.SetupDialog();
                    if (dispose) {
                        _focuser.Dispose();
                        _focuser = null;
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public bool Connect() {
            try {
                _focuser = new Focuser(Id);
                Connected = true;
                if (Connected) {
                    init();
                    RaiseAllPropertiesChanged();
                    Notification.ShowSuccess("Focuser connected");
                }
            } catch (ASCOM.DriverAccessCOMException ex) {
                Notification.ShowError(ex.Message);
            } catch (Exception ex) {
                Notification.ShowError("Unable to connect to focuser " + ex.Message);
            }
            return Connected;
        }

        private void init() {
            _canGetPosition = true;
            _canGetStepSize = true;
            _hasTempearture = true;
            _canHalt = true;
        }

        public void Disconnect() {
            Connected = false;           
            _focuser?.Dispose();
            _focuser = null;
        }
    }
}
