using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Astrometry {
    class Astrometry {       

        private static ASCOM.Astrometry.AstroUtils.AstroUtils _astroUtils;
        public static ASCOM.Astrometry.AstroUtils.AstroUtils AstroUtils {
            get {
                if (_astroUtils == null) {
                    _astroUtils = new ASCOM.Astrometry.AstroUtils.AstroUtils();
                }
                return _astroUtils;

            }
        }

        private static ASCOM.Astrometry.NOVAS.NOVAS31 _nOVAS31;
        public static ASCOM.Astrometry.NOVAS.NOVAS31 NOVAS31 {
            get {
                if (_nOVAS31 == null) {
                    _nOVAS31 = new ASCOM.Astrometry.NOVAS.NOVAS31(); ;
                }
                return _nOVAS31;

            }
        }        
    }

    public class Coordinates {

        public enum RAType {
            Degrees,
            Hours
        }

        public double RA;
        public double Dec;
        public string RAString {
            get {
                return Utility.AscomUtil.DegreesToHMS(RA);
            }
        }
        public string DecString {
            get {
                return Utility.AscomUtil.DegreesToDMS(Dec);
            }
        }
        public Epoch Epoch;

        public Coordinates(double ra, double dec, Epoch epoch, RAType ratype) {
            this.RA = ra;
            this.Dec = dec;
            this.Epoch = epoch;

            if (ratype == RAType.Degrees) {                
                this.RA = (this.RA * 24) / 360;
            }
        }

        /// <summary>
        /// Converts from one Epoch into another. Currently only from J2000 -> JNOW
        /// </summary>
        /// <param name="targetEpoch"></param>
        /// <returns></returns>
        public Coordinates transform(Epoch targetEpoch) {
            if (Epoch == targetEpoch) {
                return this;
            }
            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetJ2000(RA, Dec);

            return new Coordinates(transform.RAApparent, transform.DECApparent, Epoch.JNOW, RAType.Hours);
        }

    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Epoch {
        [Description("J2000")]
        J2000,
        [Description("JNOW")]
        JNOW
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Hemisphere {
        [Description("Northern")]
        NORTHERN,
        [Description("Southern")]
        SOUTHERN
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Direction {
        [Description("Altitude")]
        ALTITUDE,
        [Description("Azimuth")]
        AZIMUTH
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum AltitudeSite {
        [Description("East")]
        EAST,
        [Description("West")]
        WEST
    }

}
