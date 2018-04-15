using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(AstrometrySettings))]
    class PolarAlignmentSettings {

        private double altitudeDeclination = 0;
        [XmlElement(nameof(AltitudeDeclination))]
        public double AltitudeDeclination {
            get {
                return altitudeDeclination;
            }
            set {
                altitudeDeclination = value;
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
            }
        }
    }
}
