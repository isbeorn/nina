using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class FocuserSettings : IFocuserSettings {
        private string id = "No_Device";

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private bool useFilterWheelOffsets = false;

        [DataMember]
        public bool UseFilterWheelOffsets {
            get {
                return useFilterWheelOffsets;
            }
            set {
                useFilterWheelOffsets = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private int autoFocusStepSize = 10;

        [DataMember]
        public int AutoFocusStepSize {
            get {
                return autoFocusStepSize;
            }
            set {
                autoFocusStepSize = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private int autoFocusInitialOffsetSteps = 4;

        [DataMember]
        public int AutoFocusInitialOffsetSteps {
            get {
                return autoFocusInitialOffsetSteps;
            }
            set {
                autoFocusInitialOffsetSteps = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private int autoFocusExposureTime = 6;

        [DataMember]
        public int AutoFocusExposureTime {
            get {
                return autoFocusExposureTime;
            }
            set {
                autoFocusExposureTime = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}