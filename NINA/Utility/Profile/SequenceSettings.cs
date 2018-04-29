using NINA.Utility.Mediator;
using System;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(Profile))]
    public class SequenceSettings {
        private string templatePath = string.Empty;

        [XmlElement(nameof(TemplatePath))]
        public string TemplatePath {
            get {
                return templatePath;
            }
            set {
                templatePath = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private TimeSpan estimatedDownloadTime = TimeSpan.FromSeconds(0);

        [XmlIgnore]
        public TimeSpan EstimatedDownloadTime {
            get {
                return estimatedDownloadTime;
            }
            set {
                estimatedDownloadTime = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        [XmlElement(nameof(EstimatedDownloadTime))]
        public long TimeSpanInTicks {
            get {
                return estimatedDownloadTime.Ticks;
            }
            set {
                estimatedDownloadTime = new TimeSpan(value);
            }
        }
    }
}