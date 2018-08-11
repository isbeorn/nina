using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class MeridianFlipSettings : Settings, IMeridianFlipSettings {
        private bool enabled = false;

        [DataMember]
        public bool Enabled {
            get {
                return enabled;
            }
            set {
                enabled = value;
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
            }
        }
    }
}