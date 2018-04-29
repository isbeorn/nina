using NINA.Utility.Mediator;
using System;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(Profile))]
    public class MeridianFlipSettings {
        private bool enabled = false;

        [XmlElement(nameof(Enabled))]
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

        [XmlElement(nameof(Recenter))]
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

        [XmlElement(nameof(MinutesAfterMeridian))]
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

        [XmlElement(nameof(SettleTime))]
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

        [XmlElement(nameof(PauseTimeBeforeMeridian))]
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