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
    class AscomFilterWheel : FilterWheel, INotifyPropertyChanged, IFilterWheel  {

        public AscomFilterWheel(string filterWheelId) : base(filterWheelId) {
            var l = new AsyncObservableCollection<FilterInfo>();
            for (int i = 0; i < Names.Length; i++) {
                l.Add(new FilterInfo(Names[i], FocusOffsets[i], (short)i));
            }
            Filters = l;            
        }
        
        public bool Connect() {
            try {            
                Connected = true;                
                Notification.ShowSuccess("Filter wheel connected");
                RaisePropertyChanged("Position");
            } catch (ASCOM.DriverAccessCOMException ex) {
                Notification.ShowError(ex.Message);
            } catch(Exception ex) {
                Notification.ShowError("Unable to connect to filter wheel " + ex.Message);
            }
            return Connected;
        }

        private bool _connected;
        public new bool Connected {
            get {
                bool con = false;
                try {
                    con = base.Connected;
                    if(_connected != con) {                        
                        Connected = con;
                        Notification.ShowWarning("Filter wheel connection was changed outside application!");
                    }
                } catch (Exception) {
                    Notification.ShowError(ex.Message + "\n Please reconnect filter wheel!");                    
                }
                return con;
            }
            private set {
                try {
                    base.Connected = value;
                    _connected = value;
                } catch(Exception ex) {
                    Notification.ShowError(ex.Message + "\n Please reconnect filter wheel!");                    
                }
                RaisePropertyChanged();


            }
        }

        public new string Description {
            get {
                return base.Description;
            }
        }

        public new string Name {
            get {
                return base.Name;
            }
        }

        public new string DriverInfo {
            get {
                return base.DriverInfo;
            }
        }

        public new string DriverVersion {
            get {
                return base.DriverVersion;
            }
        }

        public new short InterfaceVersion {
            get {
                return base.InterfaceVersion;
            }
        }

        public new int[] FocusOffsets {
            get {
                return base.FocusOffsets;
            }
        }

        public new string[] Names {
            get {
                return base.Names;
            }
        }

        public new short Position {
            get {
                if(Connected) {
                    return base.Position;
                } else {
                    return -1;
                }
            }
            set {
                if(Connected) {
                    try {
                        base.Position = value;
                    } catch(ASCOM.DriverAccessCOMException ex) {
                        Notification.ShowWarning(ex.Message);
                    }                    
                }
                
                RaisePropertyChanged();
            }
        }

        public new ArrayList SupportedActions {
            get {
                return base.SupportedActions;
            }
        }



        public void Disconnect() {
            Connected = false;
            Position = -1;

            Filters.Clear();

            this.Dispose();            
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            var handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        
    }


    
}
