using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NINA.Model.MyFilterWheel {
    class AscomFilterWheel : BaseINPC, IFilterWheel, IDisposable {

        public AscomFilterWheel(string filterWheelId, string name) {
            Id = filterWheelId;
            Name = name;
        }

        private void init() {
            var l = new AsyncObservableCollection<FilterInfo>();
            for (int i = 0; i < Names.Length; i++) {
                l.Add(new FilterInfo(Names[i], FocusOffsets[i], (short)i));
            }
            Filters = l;
        }

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

        private FilterWheel _filterwheel;

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                try {
                    _filterwheel = new FilterWheel(Id);
                    Connected = true;
                    if (Connected) {
                        init();
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Notification.ShowError(ex.Message);
                } catch (Exception ex) {
                    Notification.ShowError("Unable to connect to filter wheel " + ex.Message);
                }
                return Connected;
            });
        }

        private bool _connected;
        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _filterwheel.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblFilterwheelConnectionLost"]);
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

                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblReconnectFilterwheel"] + Environment.NewLine + ex.Message);
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }

        public string Description {
            get {
                return _filterwheel?.Description ?? string.Empty;
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

        public string DriverInfo {
            get {
                return Connected ? _filterwheel?.DriverInfo ?? string.Empty : string.Empty;
            }
        }

        public string DriverVersion {
            get {
                return Connected ? _filterwheel?.DriverVersion ?? string.Empty : string.Empty;
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

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        public short Position {
            get {
                if (Connected) {
                    return _filterwheel.Position;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    try {
                        _filterwheel.Position = value;
                    } catch (ASCOM.DriverAccessCOMException ex) {
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
            _filterwheel?.Dispose();
            _filterwheel = null;
        }

        public void Dispose() {
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

        public bool HasSetupDialog {
            get {
                return true;
            }
        }
        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (_filterwheel == null) {
                        _filterwheel = new FilterWheel(Id);
                    }
                    _filterwheel.SetupDialog();
                    if (dispose) {
                        _filterwheel.Dispose();
                        _filterwheel = null;
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }
    }



}
