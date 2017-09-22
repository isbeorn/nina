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

        public static double HoursToDegrees(double hours) {
            return hours * 15;
        }

        public static double DegreesToHours(double deg) {
            return deg / 15;
        }

        public static double GetLocalSiderealTimeNow(double longitude) {
            return GetLocalSiderealTime(DateTime.Now,longitude);            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <param name="longitude"></param>
        /// <returns>Sidereal Time in hours</returns>
        public static double GetLocalSiderealTime(DateTime date, double longitude) {
            var utcdate = date.ToUniversalTime();
            var jd = NOVAS31.JulianDate((short)utcdate.Year,(short)utcdate.Month,(short)utcdate.Day,utcdate.Hour + utcdate.Minute / 60.0 + utcdate.Second / 60.0 / 60.0 );

            /*
            var jd = AstroUtils.JulianDateUtc; 
            var d = (jd - 2451545.0);
            var UT = DateTime.UtcNow.ToUniversalTime();
            var lst2 = 100.46 + 0.985647 * d + longitude + 15 * (UT.Hour + UT.Minute / 60.0 + UT.Second / 60.0 / 60.0);
            lst2 = (lst2 % 360) / 15;*/

            long jd_high = (long)jd;
            double jd_low = jd - jd_high;

            double lst = 0;
            NOVAS31.SiderealTime(jd_high,jd_low,NOVAS31.DeltaT(jd),ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime,ASCOM.Astrometry.Method.EquinoxBased,ASCOM.Astrometry.Accuracy.Full,ref lst);
            lst = lst + DegreesToHours(longitude);
            return lst;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="siderealTime"></param>
        /// <param name="rightAscension"></param>
        /// <returns>Hour Angle in hours</returns>
        public static double GetHourAngle(double siderealTime, double rightAscension) {
            double hourAngle = siderealTime - rightAscension;
            if (hourAngle < 0) { hourAngle += 24; }
            return hourAngle;
        }

        /*
         * some handy formulas: http://www.stargazing.net/kepler/altaz.html
         */
        /// <summary>
        /// Calculates Altitude based on given input
        /// </summary>
        /// <param name="hourAngle">in degrees</param>
        /// <param name="latitude">in degrees</param>
        /// <param name="declination">in degrees</param>
        /// <returns></returns>
        public static double GetAltitude(double hourAngle, double latitude, double declination) {
            var radX = ToRadians(hourAngle);
            var sinAlt = Math.Sin(ToRadians(declination))
                         * Math.Sin(ToRadians(latitude))
                         + Math.Cos(ToRadians(declination))
                         * Math.Cos(ToRadians(latitude))
                         * Math.Cos(radX);
            var altitude = Astrometry.ToDegree(Math.Asin(sinAlt));
            if(Settings.HemisphereType == Hemisphere.SOUTHERN) {
                altitude = -altitude;
            }
            return altitude;
        }

        /// <summary>
        /// Calculates Azimuth based on given input
        /// </summary>
        /// <param name="hourAngle">in degrees</param>
        /// <param name="altitude">in degrees</param>
        /// <param name="latitude">in degrees</param>
        /// <param name="declination">in degrees</param>
        /// <returns></returns>
        public static double GetAzimuth(double hourAngle, double altitude, double latitude, double declination) {

            var radHA = ToRadians(hourAngle);
            var radAlt = ToRadians(altitude);
            var radLat = ToRadians(latitude);
            var radDec = ToRadians(declination);

            var cosA = (Math.Sin(radDec) - Math.Sin(radAlt) * Math.Sin(radLat)) /
                        (Math.Cos(radAlt) * Math.Cos(radLat));

            //fix double precision issues
            if (cosA < -1) { cosA = -1; }
            if (cosA > 1) { cosA = 1; }

            if (Math.Sin(radHA) < 0) {
                return ToDegree(Math.Acos(cosA));
            } else {
                return 360 - ToDegree(Math.Acos(cosA));
            }
        }

        public static AstronomicalTwilight GetNightTimes(DateTime date) {            
            var d = date.Day;
            var m = date.Month;
            var y = date.Year;

            /*The returned zero based arraylist has the following values: 
             * Arraylist(0) - Boolean - True if the body is above the event limit at midnight (the beginning of the 24 hour day), false if it is below the event limit
             * Arraylist(1) - Integer - Number of rise events in this 24 hour period
             * Arraylist(2) - Integer - Number of set events in this 24 hour period
             * Arraylist(3) onwards - Double - Values of rise events in hours Arraylist
             * (3 + NumberOfRiseEvents) onwards - Double - Values of set events in hours*/
        var times = AstroUtils.EventTimes(ASCOM.Astrometry.EventType.AstronomicalTwilight,d,m,y,Settings.Latitude,Settings.Longitude,Settings.TimeZone.GetUtcOffset(date).Hours + Settings.TimeZone.GetUtcOffset(date).Minutes / 60.0);

            if(times.Count > 3) {
                int nrOfRiseEvents = (int)times[1];
                int nrOfSetEvents = (int)times[2];

                double[] rises = new double[nrOfRiseEvents];
                double[] sets = new double[nrOfSetEvents];

                for (int i = 0;i < nrOfRiseEvents;i++) {
                    rises[i] = (double)times[i + 3];
                }

                for (int i = 0;i < nrOfSetEvents;i++) {
                    sets[i] = (double)times[i + 3 + nrOfRiseEvents];
                }
                
                if(rises.Count() > 0 && sets.Count() > 0) {
                    var rise = rises[0];
                    var set = sets[0];
                    return new AstronomicalTwilight(date,rise,set);
                } else {
                    return null;
                }

                
            }
            return null;
        }

        public class AstronomicalTwilight {
            public AstronomicalTwilight(DateTime referenceDate, double rise, double set) {
                RiseDate = new DateTime(referenceDate.Year,referenceDate.Month,referenceDate.Day, referenceDate.Hour, referenceDate.Minute, referenceDate.Second);
                if(RiseDate.Hour + RiseDate.Minute / 60.0 + RiseDate.Second / 60.0 / 60.0 > rise) {
                    RiseDate = RiseDate.AddDays(1);
                }
                RiseDate = RiseDate.Date;
                RiseDate = RiseDate.AddHours(rise);

                SetDate = new DateTime(referenceDate.Year,referenceDate.Month,referenceDate.Day,referenceDate.Hour,referenceDate.Minute,referenceDate.Second);
                if (SetDate.Hour + SetDate.Minute / 60.0 + SetDate.Second / 60.0 / 60.0 > set) {
                    SetDate = SetDate.AddDays(1);
                }
                SetDate = SetDate.Date;
                SetDate = SetDate.AddHours(set);
            }
            public DateTime RiseDate { get; private set; }
            public DateTime SetDate { get; private set; }
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
