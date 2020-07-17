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
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class ApplicationSettings : Settings, IApplicationSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (Culture == "es-US") {
                Culture = "es-ES";
            }
        }

        protected override void SetDefaultValues() {
            language = new CultureInfo("en-GB");
            logLevel = LogLevelEnum.INFO;
            devicePollingInterval = 2;
            skyAtlasImageRepository = string.Empty;
            skySurveyCacheDirectory = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "FramingAssistantCache");
        }

        [DataMember]
        public string Culture {
            get {
                return Language.Name;
            }
            set {
                Language = new CultureInfo(value);
                RaisePropertyChanged();
            }
        }

        private CultureInfo language;

        public CultureInfo Language {
            get {
                return language;
            }
            set {
                if (language != value) {
                    language = value;
                    RaisePropertyChanged();
                }
            }
        }

        private LogLevelEnum logLevel;

        [DataMember]
        public LogLevelEnum LogLevel {
            get {
                return logLevel;
            }
            set {
                if (logLevel != value) {
                    logLevel = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double devicePollingInterval;

        [DataMember]
        public double DevicePollingInterval {
            get {
                return devicePollingInterval;
            }
            set {
                if (devicePollingInterval != value) {
                    devicePollingInterval = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string skyAtlasImageRepository;

        [DataMember]
        public string SkyAtlasImageRepository {
            get {
                return skyAtlasImageRepository;
            }
            set {
                if (skyAtlasImageRepository != value) {
                    skyAtlasImageRepository = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string skySurveyCacheDirectory;

        [DataMember]
        public string SkySurveyCacheDirectory {
            get {
                return skySurveyCacheDirectory;
            }
            set {
                if (skySurveyCacheDirectory != value) {
                    skySurveyCacheDirectory = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}