using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class FocuserSettings : Settings, IFocuserSettings {
        private string id = "No_Device";

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
            }
        }
    }
}