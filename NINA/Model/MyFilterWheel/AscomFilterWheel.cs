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
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NINA.Model.MyFilterWheel {

    internal class AscomFilterWheel : BaseINPC, IFilterWheel, IDisposable {

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

        public string Category { get; } = "ASCOM";

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
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
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
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }
    }
}