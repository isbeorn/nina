using ASCOM.DriverAccess;
using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {
    class AscomFilterWheel : BaseINPC, IFilterWheel  {

        public AscomFilterWheel(string filterWheelId) {
            _filterwheel = new FilterWheel(filterWheelId);
            var l = new AsyncObservableCollection<FilterInfo>();
            for (int i = 0; i < Names.Length; i++) {
                l.Add(new FilterInfo(Names[i], FocusOffsets[i], (short)i));
            }
            Filters = l;            
        }

        private FilterWheel _filterwheel;
        
        public bool Connect() {
            try {            
                Connected = true;      
                if(Connected) {
                    RaiseAllPropertiesChanged();
                    Notification.ShowSuccess("Filter wheel connected");                    
                }
            } catch (ASCOM.DriverAccessCOMException ex) {
                Notification.ShowError(ex.Message);
            } catch(Exception ex) {
                Notification.ShowError("Unable to connect to filter wheel " + ex.Message);
            }
            return Connected;
        }

        private bool _connected;
        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _filterwheel.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning("Filter wheel connection lost! Please reconnect filter wheel!");
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
                    _connected = value;
                    _filterwheel.Connected = value;
                    
                } catch(Exception ex) {
                    Notification.ShowError(ex.Message + "\n Please reconnect filter wheel!");
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }

        public string Description {
            get {
                return _filterwheel.Description;
            }
        }

        public string Name {
            get {
                return _filterwheel.Name;
            }
        }

        public string DriverInfo {
            get {
                return _filterwheel.DriverInfo;
            }
        }

        public string DriverVersion {
            get {
                return _filterwheel.DriverVersion;
            }
        }

        public short InterfaceVersion {
            get {
                return _filterwheel.InterfaceVersion;
            }
        }

        public int[] FocusOffsets {
            get {
                return _filterwheel.FocusOffsets;
            }
        }

        public string[] Names {
            get {
                return _filterwheel.Names;
            }
        }

        public short Position {
            get {
                if(Connected) {
                    return _filterwheel.Position;
                } else {
                    return -1;
                }
            }
            set {
                if(Connected) {
                    try {
                        _filterwheel.Position = value;
                    } catch(ASCOM.DriverAccessCOMException ex) {
                        Notification.ShowWarning(ex.Message);
                    }                    
                }
                
                RaisePropertyChanged();
            }
        }

        public ArrayList SupportedActions {
            get {
                return _filterwheel.SupportedActions;
            }
        }



        public void Disconnect() {
            Connected = false;
            Filters.Clear();
            _filterwheel.Dispose();            
        }

        private AsyncObservableCollection<FilterInfo> _filters;
        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                return _filters;
            }
            private set {
                _filters = value;
                RaisePropertyChanged();
            }
        }
        

    }


    
}
