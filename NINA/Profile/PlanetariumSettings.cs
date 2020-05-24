#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Enum;
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
            tsxUseSelectedObject = true;
            hnskyPort = 7700;
            hnskyHost = "localhost";
            preferredPlanetarium = PlanetariumEnum.CDC;
        }

        private string stellariumHost;

        [DataMember]
        public string StellariumHost {
            get {
                return stellariumHost;
            }
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
            get {
                return stellariumPort;
            }
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
            get {
                return cdCHost;
            }
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
            get {
                return cdCPort;
            }
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
            get {
                return tsxHost;
            }
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
            get {
                return tsxPort;
            }
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
            get {
                return tsxUseSelectedObject;
            }
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
            get {
                return hnskyHost;
            }
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
            get {
                return hnskyPort;
            }
            set {
                if (hnskyPort != value) {
                    hnskyPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private PlanetariumEnum preferredPlanetarium;

        [DataMember]
        public PlanetariumEnum PreferredPlanetarium {
            get {
                return preferredPlanetarium;
            }
            set {
                if (preferredPlanetarium != value) {
                    preferredPlanetarium = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}