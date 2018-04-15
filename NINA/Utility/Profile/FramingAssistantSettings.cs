using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(WeatherDataSettings))]
    class FramingAssistantSettings {
        private int cameraHeight = 3500;
        [XmlElement(nameof(CameraHeight))]
        public int CameraHeight {
            get {
                return cameraHeight;
            }
            set {
                cameraHeight = value;
            }
        }

        private int cameraWidth = 4500;
        [XmlElement(nameof(CameraWidth))]
        public int CameraWidth {
            get {
                return cameraWidth;
            }
            set {
                cameraWidth = value;
            }
        }

        private double fieldOfView = 3;
        [XmlElement(nameof(FieldOfView))]
        public double FieldOfView {
            get {
                return fieldOfView;
            }
            set {
                fieldOfView = value;
            }
        }
    }
}
