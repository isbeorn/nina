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

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class PlanetariumSettings : Settings, IPlanetariumSettings {

        public PlanetariumSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDeserialization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            stellariumTimeout = 500;
            stellariumPort = 8090;
            stellariumHost = "localhost";
            cdCTimeout = 300;
            cdCPort = 3292;
            cdCHost = "localhost";
            TSXTimeout = 300;
            TSXPort = 3040;
            TSXHost = "localhost";
            HNSKYTimeout = 300;
            HNSKYPort = 7700;
            HNSKYHost = "localhost";
            preferredPlanetarium = PlanetariumEnum.CDC;
        }

        private string stellariumHost;

        [DataMember]
        public string StellariumHost {
            get {
                return stellariumHost;
            }
            set {
                stellariumHost = value;
                RaisePropertyChanged();
            }
        }

        private int stellariumPort;

        [DataMember]
        public int StellariumPort {
            get {
                return stellariumPort;
            }
            set {
                stellariumPort = value;
                RaisePropertyChanged();
            }
        }

        private int stellariumTimeout;

        [DataMember]
        public int StellariumTimeout {
            get {
                return stellariumTimeout;
            }
            set {
                stellariumTimeout = value;
                RaisePropertyChanged();
            }
        }

        private string cdCHost;

        [DataMember]
        public string CdCHost {
            get {
                return cdCHost;
            }
            set {
                cdCHost = value;
                RaisePropertyChanged();
            }
        }

        private int cdCPort;

        [DataMember]
        public int CdCPort {
            get {
                return cdCPort;
            }
            set {
                cdCPort = value;
                RaisePropertyChanged();
            }
        }

        private int cdCTimeout;

        [DataMember]
        public int CdCTimeout {
            get {
                return cdCTimeout;
            }
            set {
                cdCTimeout = value;
                RaisePropertyChanged();
            }
        }

        private string tsxHost;

        [DataMember]
        public string TSXHost {
            get {
                return tsxHost;
            }
            set {
                tsxHost = value;
                RaisePropertyChanged();
            }
        }

        private int _TSXPort;

        [DataMember]
        public int TSXPort {
            get {
                return _TSXPort;
            }
            set {
                _TSXPort = value;
                RaisePropertyChanged();
            }
        }

        private int _TSXTimeout;

        [DataMember]
        public int TSXTimeout {
            get {
                return _TSXTimeout;
            }
            set {
                _TSXTimeout = value;
                RaisePropertyChanged();
            }
        }

        private string hnskyHost;

        [DataMember]
        public string HNSKYHost {
            get {
                return hnskyHost;
            }
            set {
                hnskyHost = value;
                RaisePropertyChanged();
            }
        }

        private int hnskyPort;

        [DataMember]
        public int HNSKYPort {
            get {
                return hnskyPort;
            }
            set {
                hnskyPort = value;
                RaisePropertyChanged();
            }
        }

        private int hnskyTimeout;

        [DataMember]
        public int HNSKYTimeout {
            get {
                return hnskyTimeout;
            }
            set {
                hnskyTimeout = value;
                RaisePropertyChanged();
            }
        }

        private PlanetariumEnum preferredPlanetarium;

        [DataMember]
        public PlanetariumEnum PreferredPlanetarium {
            get {
                return preferredPlanetarium;
            }
            set {
                preferredPlanetarium = value;
                RaisePropertyChanged();
            }
        }
    }
}