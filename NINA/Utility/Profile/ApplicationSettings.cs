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
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class ApplicationSettings : Settings, IApplicationSettings {

        public ApplicationSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDesiralization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            language = new CultureInfo("en-GB");
            logLevel = LogLevelEnum.ERROR;
            devicePollingInterval = 2;
            skyAtlasImageRepository = string.Empty;
            skySurveyCacheDirectory = Path.Combine(Utility.APPLICATIONTEMPPATH, "FramingAssistantCache");
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
                language = value;
                RaisePropertyChanged();
            }
        }

        private LogLevelEnum logLevel;

        [DataMember]
        public LogLevelEnum LogLevel {
            get {
                return logLevel;
            }
            set {
                logLevel = value;
                RaisePropertyChanged();
            }
        }

        private double devicePollingInterval;

        [DataMember]
        public double DevicePollingInterval {
            get {
                return devicePollingInterval;
            }
            set {
                devicePollingInterval = value;
                RaisePropertyChanged();
            }
        }

        private string skyAtlasImageRepository;

        [DataMember]
        public string SkyAtlasImageRepository {
            get {
                return skyAtlasImageRepository;
            }
            set {
                skyAtlasImageRepository = value;
                RaisePropertyChanged();
            }
        }

        private string skySurveyCacheDirectory;

        [DataMember]
        public string SkySurveyCacheDirectory {
            get {
                return skySurveyCacheDirectory;
            }
            set {
                skySurveyCacheDirectory = value;
                RaisePropertyChanged();
            }
        }
    }
}