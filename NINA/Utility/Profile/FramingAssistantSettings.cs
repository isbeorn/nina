using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class FramingAssistantSettings : IFramingAssistantSettings {
        private int cameraHeight = 3500;

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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