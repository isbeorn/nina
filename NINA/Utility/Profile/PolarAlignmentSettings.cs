using NINA.Utility.Mediator;
using System;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(AstrometrySettings))]
    public class PolarAlignmentSettings {
        private double altitudeDeclination = 0;

        [XmlElement(nameof(AltitudeDeclination))]
        public double AltitudeDeclination {
            get {
                return altitudeDeclination;
            }
            set {
                altitudeDeclination = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double altitudeMeridianOffset = -65;

        [XmlElement(nameof(AltitudeMeridianOffset))]
        public double AltitudeMeridianOffset {
            get {
                return altitudeMeridianOffset;
            }
            set {
                altitudeMeridianOffset = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double azimuthDeclination = 0;

        [XmlElement(nameof(AzimuthDeclination))]
        public double AzimuthDeclination {
            get {
                return azimuthDeclination;
            }
            set {
                azimuthDeclination = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double azimuthMeridianOffset = 90;

        [XmlElement(nameof(AzimuthMeridianOffset))]
        public double AzimuthMeridianOffset {
            get {
                return azimuthMeridianOffset;
            }
            set {
                azimuthMeridianOffset = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}