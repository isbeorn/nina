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

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class TelescopeSettings : Settings, ITelescopeSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            id = "No_Device";
            name = string.Empty;
            focalLength = double.NaN;
            focalRatio = double.NaN;
            snapPortStart = ":SNAP1,1#";
            snapPortStop = "SNAP1,0#";
            settleTime = 5;
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

        private string name;

        [DataMember]
        public string Name {
            get {
                return name;
            }
            set {
                if (name != value) {
                    name = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double focalLength;

        [DataMember]
        public double FocalLength {
            get {
                return focalLength;
            }
            set {
                if (focalLength != value) {
                    focalLength = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double focalRatio;

        [DataMember]
        public double FocalRatio {
            get {
                return focalRatio;
            }
            set {
                if (focalRatio != value) {
                    focalRatio = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string snapPortStart;

        [DataMember]
        public string SnapPortStart {
            get {
                return snapPortStart;
            }
            set {
                if (snapPortStart != value) {
                    snapPortStart = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string snapPortStop;

        [DataMember]
        public string SnapPortStop {
            get {
                return snapPortStop;
            }
            set {
                if (snapPortStop != value) {
                    snapPortStop = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int settleTime;

        [DataMember]
        public int SettleTime {
            get {
                return settleTime;
            }
            set {
                if (settleTime != value) {
                    settleTime = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}