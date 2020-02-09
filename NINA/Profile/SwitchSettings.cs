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

using System.Resources;
using System.Runtime.Serialization;

namespace NINA.Profile {

    public sealed class SwitchSettings : Settings, ISwitchSettings {

        public SwitchSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            id = "No_Device";
            eagleUrl = "http://localhost:1380/";
        }

        private string id;

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                if (id != value) {
                    id = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string eagleUrl;

        [DataMember]
        public string EagleUrl {
            get {
                return eagleUrl;
            }
            set {
                if (eagleUrl != value) {
                    eagleUrl = value;
                    RaisePropertyChanged();
                }
            }
        }

        #region UltimatePowerboxV2

        private string _upbv2PortName;

        [DataMember]
        public string Upbv2PortName {
            get => _upbv2PortName;
            set {
                if (_upbv2PortName != null && _upbv2PortName.Equals(value)) return;
                _upbv2PortName = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2PowerName1;

        [DataMember]
        public string Upbv2PowerName1 {
            get => _upbv2PowerName1;
            set {
                if (_upbv2PowerName1 != null && _upbv2PowerName1.Equals(value)) return;
                _upbv2PowerName1 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2PowerName2;

        [DataMember]
        public string Upbv2PowerName2 {
            get => _upbv2PowerName2;
            set {
                if (_upbv2PowerName2 != null && _upbv2PowerName2.Equals(value)) return;
                _upbv2PowerName2 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2PowerName3;

        [DataMember]
        public string Upbv2PowerName3 {
            get => _upbv2PowerName3;
            set {
                if (_upbv2PowerName3 != null && _upbv2PowerName3.Equals(value)) return;
                _upbv2PowerName3 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2PowerName4;

        [DataMember]
        public string Upbv2PowerName4 {
            get => _upbv2PowerName4;
            set {
                if (_upbv2PowerName4 != null && _upbv2PowerName4.Equals(value)) return;
                _upbv2PowerName4 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2PowerName5;

        [DataMember]
        public string Upbv2PowerName5 {
            get => _upbv2PowerName5;
            set {
                if (_upbv2PowerName5 != null && _upbv2PowerName5.Equals(value)) return;
                _upbv2PowerName5 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2UsbName1;

        [DataMember]
        public string Upbv2UsbName1 {
            get => _upbv2UsbName1;
            set {
                if (_upbv2UsbName1 != null && _upbv2UsbName1.Equals(value)) return;
                _upbv2UsbName1 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2UsbName2;

        [DataMember]
        public string Upbv2UsbName2 {
            get => _upbv2UsbName2;
            set {
                if (_upbv2UsbName2 != null && _upbv2UsbName2.Equals(value)) return;
                _upbv2UsbName2 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2UsbName3;

        [DataMember]
        public string Upbv2UsbName3 {
            get => _upbv2UsbName3;
            set {
                if (_upbv2UsbName3 != null && _upbv2UsbName3.Equals(value)) return;
                _upbv2UsbName3 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2UsbName4;

        [DataMember]
        public string Upbv2UsbName4 {
            get => _upbv2UsbName4;
            set {
                if (_upbv2UsbName4 != null && _upbv2UsbName4.Equals(value)) return;
                _upbv2UsbName4 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2UsbName5;

        [DataMember]
        public string Upbv2UsbName5 {
            get => _upbv2UsbName5;
            set {
                if (_upbv2UsbName5 != null && _upbv2UsbName5.Equals(value)) return;
                _upbv2UsbName5 = value;
                RaisePropertyChanged();
            }
        }

        private string _upbv2UsbName6;

        [DataMember]
        public string Upbv2UsbName6 {
            get => _upbv2UsbName6;
            set {
                if (_upbv2UsbName6 != null && _upbv2UsbName6.Equals(value)) return;
                _upbv2UsbName6 = value;
                RaisePropertyChanged();
            }
        }

        #endregion UltimatePowerboxV2
    }
}