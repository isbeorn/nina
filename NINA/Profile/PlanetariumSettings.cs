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
            stellariumTimeout = 500;
            stellariumPort = 8090;
            stellariumHost = "localhost";
            cdCTimeout = 300;
            cdCPort = 3292;
            cdCHost = "localhost";
            tsxTimeout = 300;
            tsxPort = 3040;
            tsxHost = "localhost";
            hnskyTimeout = 300;
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

        private int stellariumTimeout;

        [DataMember]
        public int StellariumTimeout {
            get {
                return stellariumTimeout;
            }
            set {
                if (stellariumTimeout != value) {
                    stellariumTimeout = value;
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

        private int cdCTimeout;

        [DataMember]
        public int CdCTimeout {
            get {
                return cdCTimeout;
            }
            set {
                if (cdCTimeout != value) {
                    cdCTimeout = value;
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

        private int tsxTimeout;

        [DataMember]
        public int TSXTimeout {
            get {
                return tsxTimeout;
            }
            set {
                if (tsxTimeout != value) {
                    tsxTimeout = value;
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

        private int hnskyTimeout;

        [DataMember]
        public int HNSKYTimeout {
            get {
                return hnskyTimeout;
            }
            set {
                if (hnskyTimeout != value) {
                    hnskyTimeout = value;
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