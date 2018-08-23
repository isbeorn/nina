using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class SequenceSettings : Settings, ISequenceSettings {
        private string templatePath = string.Empty;

        [DataMember]
        public string TemplatePath {
            get {
                return templatePath;
            }
            set {
                templatePath = value;
                RaisePropertyChanged();
            }
        }

        private TimeSpan estimatedDownloadTime = TimeSpan.FromSeconds(0);

        public TimeSpan EstimatedDownloadTime {
            get {
                return estimatedDownloadTime;
            }
            set {
                estimatedDownloadTime = value;
                RaisePropertyChanged();
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