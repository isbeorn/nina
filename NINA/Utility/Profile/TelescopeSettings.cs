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

using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class TelescopeSettings : Settings, ITelescopeSettings {
        private string id = "No_Device";

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private int focalLength = 800;

        [DataMember]
        public int FocalLength {
            get {
                return focalLength;
            }
            set {
                focalLength = value;
                RaisePropertyChanged();
            }
        }

        private string snapPortStart = ":SNAP1,1#";

        [DataMember]
        public string SnapPortStart {
            get {
                return snapPortStart;
            }
            set {
                snapPortStart = value;
                RaisePropertyChanged();
            }
        }

        private string snapPortStop = "SNAP1,0#";

        [DataMember]
        public string SnapPortStop {
            get {
                return snapPortStop;
            }
            set {
                snapPortStop = value;
                RaisePropertyChanged();
            }
        }

        private int settleTime = 5;

        [DataMember]
        public int SettleTime {
            get {
                return settleTime;
            }
            set {
                settleTime = value;
                RaisePropertyChanged();
            }
        }
    }
}