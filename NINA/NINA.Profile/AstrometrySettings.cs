#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class AstrometrySettings : Settings, IAstrometrySettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            try {
                if (!string.IsNullOrWhiteSpace(HorizonFilePath)) {
                    Horizon = CustomHorizon.FromFilePath(HorizonFilePath);
                }
            } catch (Exception e) {
                Logger.Error($"Failed to parse custom horizon file {HorizonFilePath}", e);
                HorizonFilePath = "";
            }
        }

        protected override void SetDefaultValues() {
            latitude = 0;
            longitude = 0;
            elevation = 0;
            horizonFilePath = string.Empty;
            observer = string.Empty;
            observatory = string.Empty;
            site = string.Empty;
        }

        private double latitude;

        [DataMember]
        public double Latitude {
            get => latitude;
            set {
                if (latitude != value) {
                    latitude = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double longitude;

        [DataMember]
        public double Longitude {
            get => longitude;
            set {
                if (longitude != value) {
                    longitude = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double elevation;

        [DataMember]
        public double Elevation {
            get => elevation;
            set {
                if (elevation != value) {
                    elevation = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string horizonFilePath;

        [DataMember]
        public string HorizonFilePath {
            get => horizonFilePath;
            set {
                if (horizonFilePath != value) {
                    horizonFilePath = value;
                    RaisePropertyChanged();
                }
            }
        }

        private CustomHorizon horizon;

        public CustomHorizon Horizon {
            get => horizon;
            set {
                horizon = value;
                RaisePropertyChanged();
            }
        }

        private string observer;

        [DataMember]
        public string Observer {
            get => observer;
            set {
                if (observer != value) {
                    observer = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string observatory;

        [DataMember]
        public string Observatory {
            get => observatory;
            set {
                if (observatory != value) {
                    observatory = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string site;

        [DataMember]
        public string Site {
            get => site;
            set {
                if (site != value) {
                    site = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}