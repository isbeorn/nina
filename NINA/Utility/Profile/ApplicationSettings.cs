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
    class ApplicationSettings {


        private CultureInfo language = new CultureInfo("en-GB");
        [XmlElement(nameof(Language))]
        public CultureInfo Language {
            get {
                return language;
            }
            set {
                var culture = (CultureInfo)value;
                language = culture;

                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                Locale.Loc.Instance.ReloadLocale();
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
                return databaseLocation;
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
