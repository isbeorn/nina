using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            }
        }

        private TimeSpan estimatedDownloadTime = TimeSpan.FromSeconds(0);
        [XmlElement(nameof(EstimatedDownloadTime))]
        public TimeSpan EstimatedDownloadTime {
            get {
                return estimatedDownloadTime;
            }
            set {
                estimatedDownloadTime = value;
            }
        }
    }
}
