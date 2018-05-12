using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class MeridianFlipSettings : IMeridianFlipSettings {
        private bool enabled = false;

        [DataMember]
        public bool Enabled {
            get {
                return enabled;
            }
            set {
                enabled = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private bool recenter = true;

        [DataMember]
        public bool Recenter {
            get {
                return recenter;
            }
            set {
                recenter = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double minutesAfterMeridian = 1;

        [DataMember]
        public double MinutesAfterMeridian {
            get {
                return minutesAfterMeridian;
            }
            set {
                minutesAfterMeridian = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private int settleTime = 5;

        [DataMember]
        public int SettleTime {
            get {
                return settleTime;
            }
            set {
                settleTime = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double pauseTimeBeforeMeridian = 1;

        [DataMember]
        public double PauseTimeBeforeMeridian {
            get {
                return pauseTimeBeforeMeridian;
            }
            set {
                pauseTimeBeforeMeridian = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}