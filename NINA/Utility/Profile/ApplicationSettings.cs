using NINA.Utility.Enum;
using NINA.Utility.Mediator;
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
            databaseLocation = @"%localappdata%\NINA\NINA.sqlite";
            devicePollingInterval = 0.5;
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

        private string databaseLocation;

        [DataMember]
        public string DatabaseLocation {
            get {
                return Environment.ExpandEnvironmentVariables(databaseLocation);
            }
            set {
                databaseLocation = value;
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