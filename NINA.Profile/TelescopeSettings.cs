#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Profile.Interfaces;
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
            TelescopeLocationSyncDirection = TelescopeLocationSyncDirection.PROMPT;
        }

        private string id;

        [DataMember]
        public string Id {
            get => id;
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
            get => name;
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
            get => focalLength;
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
            get => focalRatio;
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
            get => snapPortStart;
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
            get => snapPortStop;
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
            get => settleTime;
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
            get => noSync;
            set {
                if (noSync != value) {
                    noSync = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool primaryReversed;

        [DataMember]
        public bool PrimaryReversed {
            get => primaryReversed;
            set {
                if (primaryReversed != value) {
                    primaryReversed = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool secondaryReversed;

        [DataMember]
        public bool SecondaryReversed {
            get => secondaryReversed;
            set {
                if (secondaryReversed != value) {
                    secondaryReversed = value;
                    RaisePropertyChanged();
                }
            }
        }

        private TelescopeLocationSyncDirection telescopeLocationSyncDirection;
        [DataMember]
        public TelescopeLocationSyncDirection TelescopeLocationSyncDirection {
            get => telescopeLocationSyncDirection;
            set {
                if(telescopeLocationSyncDirection != value) {
                    telescopeLocationSyncDirection = value;
                    RaisePropertyChanged();
                }                
            }
        }
    }
}