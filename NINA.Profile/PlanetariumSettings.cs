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
    public class PlanetariumSettings : Settings, IPlanetariumSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            stellariumPort = 8090;
            stellariumHost = "localhost";
            cdCPort = 3292;
            cdCHost = "localhost";
            tsxPort = 3040;
            tsxHost = "localhost";
            tsxUseSelectedObject = false;
            hnskyPort = 7700;
            hnskyHost = "localhost";
            c2aPort = 5876;
            c2aHost = "localhost";
            skytechxPort = 2055;
            skytechxHost = "localhost";
            preferredPlanetarium = PlanetariumEnum.CDC;
        }

        private string stellariumHost;

        [DataMember]
        public string StellariumHost {
            get => stellariumHost;
            set {
                if (stellariumHost != value) {
                    stellariumHost = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int stellariumPort;

        [DataMember]
        public int StellariumPort {
            get => stellariumPort;
            set {
                if (stellariumPort != value) {
                    stellariumPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string cdCHost;

        [DataMember]
        public string CdCHost {
            get => cdCHost;
            set {
                if (cdCHost != value) {
                    cdCHost = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int cdCPort;

        [DataMember]
        public int CdCPort {
            get => cdCPort;
            set {
                if (cdCPort != value) {
                    cdCPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string tsxHost;

        [DataMember]
        public string TSXHost {
            get => tsxHost;
            set {
                if (tsxHost != value) {
                    tsxHost = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int tsxPort;

        [DataMember]
        public int TSXPort {
            get => tsxPort;
            set {
                if (tsxPort != value) {
                    tsxPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool tsxUseSelectedObject;

        [DataMember]
        public bool TSXUseSelectedObject {
            get => tsxUseSelectedObject;
            set {
                if (tsxUseSelectedObject != value) {
                    tsxUseSelectedObject = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string hnskyHost;

        [DataMember]
        public string HNSKYHost {
            get => hnskyHost;
            set {
                if (hnskyHost != value) {
                    hnskyHost = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int hnskyPort;

        [DataMember]
        public int HNSKYPort {
            get => hnskyPort;
            set {
                if (hnskyPort != value) {
                    hnskyPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string c2aHost;

        [DataMember]
        public string C2AHost {
            get => c2aHost;
            set {
                if (c2aHost != value) {
                    c2aHost = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int c2aPort;

        [DataMember]
        public int C2APort {
            get => c2aPort;
            set {
                if (c2aPort != value) {
                    c2aPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string skytechxHost;

        [DataMember]
        public string SkytechXHost {
            get => skytechxHost;
            set {
                if (skytechxHost != value) {
                    skytechxHost = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int skytechxPort;

        [DataMember]
        public int SkytechXPort {
            get => skytechxPort;
            set {
                if (skytechxPort != value) {
                    skytechxPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private PlanetariumEnum preferredPlanetarium;

        [DataMember]
        public PlanetariumEnum PreferredPlanetarium {
            get => preferredPlanetarium;
            set {
                if (preferredPlanetarium != value) {
                    preferredPlanetarium = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}