using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class ApplicationSettings : Settings, IApplicationSettings {

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

        private CultureInfo language = new CultureInfo("en-GB");

        public CultureInfo Language {
            get {
                return language;
            }
            set {
                language = value;
                RaisePropertyChanged();
            }
        }

        private LogLevelEnum logLevel = LogLevelEnum.ERROR;

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

        private string databaseLocation = @"%localappdata%\NINA\NINA.sqlite";

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

        private double devicePollingInterval = 0.5;

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

        private string skyAtlasImageRepository = string.Empty;

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
    }
}