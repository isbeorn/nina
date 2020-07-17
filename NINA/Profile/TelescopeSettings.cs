#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
            noSync = false;
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

        private bool noSync;

        [DataMember]
        public bool NoSync {
            get {
                return noSync;
            }
            set {
                if (noSync != value) {
                    noSync = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}