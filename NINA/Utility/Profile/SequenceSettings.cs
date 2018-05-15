using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class SequenceSettings : ISequenceSettings {
        private string templatePath = string.Empty;

        [DataMember]
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

        public TimeSpan EstimatedDownloadTime {
            get {
                return estimatedDownloadTime;
            }
            set {
                estimatedDownloadTime = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        [DataMember]
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