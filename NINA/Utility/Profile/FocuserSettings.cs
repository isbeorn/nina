using NINA.Utility.Mediator;
using System;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(FocuserSettings))]
    public class FocuserSettings {
        private string id = "No_Device";

        [XmlElement(nameof(Id))]
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

        [XmlElement(nameof(UseFilterWheelOffsets))]
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

        [XmlElement(nameof(AutoFocusStepSize))]
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

        [XmlElement(nameof(AutoFocusInitialOffsetSteps))]
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

        [XmlElement(nameof(AutoFocusExposureTime))]
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