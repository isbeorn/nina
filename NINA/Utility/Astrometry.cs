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

        /// <summary>
        /// Convert degree to radians
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static double ToRadians(double val) {
            return (Math.PI / 180) * val;
        }

        /// <summary>
        /// Convert radians to degree
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double ToDegree(double angle) {
            return angle * (180.0 / Math.PI);
        }
        

        public static double DegreeToArcmin(double degree) {
            return degree * 60;
        }

        public static double DegreeToArcsec(double degree) {
            return degree * 60 * 60;
        }

        public static double ArcminToArcsec(double arcmin) {
            return arcmin * 60;
        }

        public static double ArcminToDegree(double arcmin) {
            return arcmin / 60;
        }

        public static double ArcsecToArcmin(double arcsec) {
            return arcsec / 60;
        }

        public static double ArcsecToDegree(double arcsec) {
            return arcsec / 60 / 60;
        }
    }

    public class Coordinates {

        public enum RAType {
            Degrees,
            Hours
        }

        /// <summary>
        /// Right Ascension in hours
        /// </summary>
        public double RA { get; private set; }

        public string RAString {
            get {
                return Utility.AscomUtil.DegreesToHMS(RADegrees);
            }
        }

        /// <summary>
        /// Right Ascension in degrees
        /// </summary>
        public double RADegrees {
            get {
                return RA * 360 / 24;
            }
        }

        /// <summary>
        /// Declination in Degrees
        /// </summary>
        public double Dec { get; private set; }

        public string DecString {
            get {
                return Utility.AscomUtil.DegreesToDMS(Dec);
            }
        }

        /// <summary>
        /// Epoch the coordinates are stored in. Either J2000 or JNOW
        /// </summary>
        public Epoch Epoch { get; private set; }

        /// <summary>
        /// Creates new coordinates
        /// </summary>
        /// <param name="ra">Right Ascension in degrees or hours. RAType has to be set accordingly</param>
        /// <param name="dec">Declination in degrees</param>
        /// <param name="epoch">J2000|JNOW</param>
        /// <param name="ratype">Degrees|Hours</param>
        public Coordinates(double ra, double dec, Epoch epoch, RAType ratype) {
            this.RA = ra;
            this.Dec = dec;
            this.Epoch = epoch;

            if (ratype == RAType.Degrees) {                
                this.RA = (this.RA * 24) / 360;
            }
        }

        /// <summary>
        /// Converts from one Epoch into another.
        /// </summary>
        /// <param name="targetEpoch"></param>
        /// <returns></returns>
        public Coordinates Transform(Epoch targetEpoch) {
            if (Epoch == targetEpoch) {
                return this;
            }

            if(targetEpoch == Epoch.JNOW) {
                return TransformToJNOW();
            } else if (targetEpoch == Epoch.J2000) {
                return TransformToJ2000();
            } else {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Transforms coordinates from J2000 to JNOW
        /// </summary>
        /// <returns></returns>
        private Coordinates TransformToJNOW() {
            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetJ2000(RA, Dec);
            return new Coordinates(transform.RAApparent, transform.DECApparent, Epoch.JNOW, RAType.Hours);
        }

        /// <summary>
        /// Transforms coordinates from JNOW to J2000
        /// </summary>
        /// <returns></returns>
        private Coordinates TransformToJ2000() {
            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetApparent(RA, Dec);
            return new Coordinates(transform.RAJ2000, transform.DecJ2000, Epoch.J2000, RAType.Hours);
        }

    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Epoch {
        [Description("LblJ2000")]
        J2000,
        [Description("LblJNOW")]
        JNOW
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Hemisphere {
        [Description("LblNorthern")]
        NORTHERN,
        [Description("LblSouthern")]
        SOUTHERN
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Direction {
        [Description("LblAltitude")]
        ALTITUDE,
        [Description("LblAzimuth")]
        AZIMUTH
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum AltitudeSite {
        [Description("LblEast")]
        EAST,
        [Description("LblWest")]
        WEST
    }

}
