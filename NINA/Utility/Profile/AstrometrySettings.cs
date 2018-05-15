using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class AstrometrySettings : IAstrometrySettings {
        private Epoch epochType = Epoch.JNOW;

        [DataMember]
        public Epoch EpochType {
            get {
                return epochType;
            }
            set {
                epochType = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private Hemisphere hemisphereType = Hemisphere.NORTHERN;

        [DataMember]
        public Hemisphere HemisphereType {
            get {
                return hemisphereType;
            }
            set {
                hemisphereType = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double latitude = 0;

        [DataMember]
        public double Latitude {
            get {
                return latitude;
            }
            set {
                latitude = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double longitude = 0;

        [DataMember]
        public double Longitude {
            get {
                return longitude;
            }
            set {
                longitude = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}