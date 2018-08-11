using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class FramingAssistantSettings : Settings, IFramingAssistantSettings {
        private int cameraHeight = 3500;

        [DataMember]
        public int CameraHeight {
            get {
                return cameraHeight;
            }
            set {
                cameraHeight = value;
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
            }
        }
    }
}