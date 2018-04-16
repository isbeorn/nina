using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(ApplicationSettings))]
    public class ApplicationSettings {


        private string culture;
        [XmlElement(nameof(Culture))]
        public string Culture {
            get {
                return culture;
            }
            set {
                culture = value;
                Language = new CultureInfo(value);
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
                culture = value.Name;
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
            }
        }

    }
}
