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
        public void OnDesiralization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            stellariumTimeout = 500;
            stellariumPort = 8090;
            stellariumHost = "localhost";
            cdCTimeout = 300;
            cdCPort = 3292;
            cdCHost = "localhost";
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

        private PlanetariumEnum preferredPlanetarium;

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