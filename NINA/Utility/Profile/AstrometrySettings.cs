using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using System;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(AstrometrySettings))]
    public class AstrometrySettings {
        private Epoch epochType = Epoch.JNOW;

        [XmlElement(nameof(EpochType))]
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

        [XmlElement(nameof(HemisphereType))]
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

        [XmlElement(nameof(Latitude))]
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

        [XmlElement(nameof(Longitude))]
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