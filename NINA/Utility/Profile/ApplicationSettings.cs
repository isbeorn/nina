using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Globalization;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(ApplicationSettings))]
    public class ApplicationSettings {

        [XmlElement(nameof(Culture))]
        public string Culture {
            get {
                return Language.Name;
            }
            set {
                Language = new CultureInfo(value);
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private CultureInfo language = new CultureInfo("en-GB");

        [XmlIgnore()]
        public CultureInfo Language {
            get {
                return language;
            }
            set {
                language = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private LogLevelEnum logLevel = LogLevelEnum.ERROR;

        [XmlElement(nameof(LogLevel))]
        public LogLevelEnum LogLevel {
            get {
                return logLevel;
            }
            set {
                logLevel = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private string databaseLocation = @"%localappdata%\NINA\NINA.sqlite";

        [XmlElement(nameof(DatabaseLocation))]
        public string DatabaseLocation {
            get {
                return Environment.ExpandEnvironmentVariables(databaseLocation);
            }
            set {
                databaseLocation = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double devicePollingInterval = 0.5;

        [XmlElement(nameof(DevicePollingInterval))]
        public double DevicePollingInterval {
            get {
                return devicePollingInterval;
            }
            set {
                devicePollingInterval = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private string skyAtlasImageRepository = string.Empty;

        [XmlElement(nameof(SkyAtlasImageRepository))]
        public string SkyAtlasImageRepository {
            get {
                return skyAtlasImageRepository;
            }
            set {
                skyAtlasImageRepository = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}