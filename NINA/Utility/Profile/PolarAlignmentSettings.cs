using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class PolarAlignmentSettings : IPolarAlignmentSettings {
        private double altitudeDeclination = 0;

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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