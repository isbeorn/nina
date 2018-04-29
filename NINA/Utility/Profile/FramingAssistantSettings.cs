using NINA.Utility.Mediator;
using System;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(WeatherDataSettings))]
    public class FramingAssistantSettings {
        private int cameraHeight = 3500;

        [XmlElement(nameof(CameraHeight))]
        public int CameraHeight {
            get {
                return cameraHeight;
            }
            set {
                cameraHeight = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}