using ASCOM.Astrometry;
using NINA.Utility.Profile;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Astrometry {

    public class Astrometry {

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
            return GetLocalSiderealTime(DateTime.Now, longitude);
        }

        public static double GetJulianDate(DateTime date) {
            var utcdate = date.ToUniversalTime();
            return NOVAS.JulianDate((short)utcdate.Year, (short)utcdate.Month, (short)utcdate.Day, utcdate.Hour + utcdate.Minute / 60.0 + utcdate.Second / 60.0 / 60.0);
        }

        /// <summary>
        /// Calculates the value of DeltaT using SOFA
        /// Published DeltaT information: https://www.usno.navy.mil/USNO/earth-orientation/eo-products/long-term
        /// </summary>
        /// <param name="date">Date to retrieve DeltaT for</param>
        /// <returns>DeltaT at given date</returns>
        public static double DeltaT(DateTime date) {
            var utcDate = date.ToUniversalTime();
            double utc1 = 0, utc2 = 0, tai1 = 0, tai2 = 0, tt1 = 0, tt2 = 0;
            SOFA.Dtf2d("UTC", utcDate.Year, utcDate.Month, utcDate.Day, utcDate.Hour, utcDate.Minute, (double)utcDate.Second + (double)utcDate.Millisecond / 1000.0, ref utc1, ref utc2);
            SOFA.UtcTai(utc1, utc2, ref tai1, ref tai2);
            SOFA.TaiTt(tai1, tai2, ref tt1, ref tt2);

            var utc = utc1 + utc2;
            var tt = tt1 + tt2;
            var deltaT = Math.Abs(utc - tt) * 60 * 60 * 24;
            return deltaT;
        }

        /// <summary>
        /// </summary>
        /// <param name="date">     </param>
        /// <param name="longitude"></param>
        /// <returns>Sidereal Time in hours</returns>
        public static double GetLocalSiderealTime(DateTime date, double longitude) {
            var jd = GetJulianDate(date);

            /*
            var jd = AstroUtils.JulianDateUtc;
            var d = (jd - 2451545.0);
            var UT = DateTime.UtcNow.ToUniversalTime();
            var lst2 = 100.46 + 0.985647 * d + longitude + 15 * (UT.Hour + UT.Minute / 60.0 + UT.Second / 60.0 / 60.0);
            lst2 = (lst2 % 360) / 15;*/

            long jd_high = (long)jd;
            double jd_low = jd - jd_high;

            double lst = 0;
            NOVAS.SiderealTime(jd_high, jd_low, DeltaT(date), NOVAS.GstType.GreenwichApparentSiderealTime, NOVAS.Method.EquinoxBased, NOVAS.Accuracy.Full, ref lst);
            lst = lst + DegreesToHours(longitude);
            return lst;
        }

        /// <summary>
        /// </summary>
        /// <param name="siderealTime">  </param>
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
        /// <param name="hourAngle">  in degrees</param>
        /// <param name="latitude">   in degrees</param>
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
            return altitude;
        }

        /// <summary>
        /// Calculates Azimuth based on given input
        /// </summary>
        /// <param name="hourAngle">  in degrees</param>
        /// <param name="altitude">   in degrees</param>
        /// <param name="latitude">   in degrees</param>
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

        public static double AUToKilometer(double au) {
            const double conversionFactor = 149597870.7; // https://de.wikipedia.org/wiki/Astronomische_Einheit
            return au * conversionFactor;
        }

        public static RiseAndSetEvent GetNightTimes(DateTime date, double latitude, double longitude) {
            var riseAndSet = new AstronomicalTwilightRiseAndSet(date, latitude, longitude);
            var t = riseAndSet.Calculate().Result;

            return riseAndSet;
        }

        public static RiseAndSetEvent GetMoonRiseAndSet(DateTime date, double latitude, double longitude) {
            var riseAndSet = new MoonRiseAndSet(date, latitude, longitude);
            var t = riseAndSet.Calculate().Result;

            return riseAndSet;
        }

        public static RiseAndSetEvent GetSunRiseAndSet(DateTime date, double latitude, double longitude) {
            var riseAndSet = new SunRiseAndSet(date, latitude, longitude);
            var t = riseAndSet.Calculate().Result;

            return riseAndSet;
        }

        /// <summary>
        /// Formats a given hours value into format "DD° MM' SS"
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DegreesToDMS(double value) {
            return DegreesToDMS(value, "{0:00}° {1:00}' {2:00}\"");
        }

        private static string DegreesToDMS(double value, string pattern) {
            bool negative = false;
            if (value < 0) {
                negative = true;
                value = -value;
            }
            if (negative) {
                pattern = "-" + pattern;
            }

            var degree = Math.Floor(value);
            var arcmin = Math.Floor(DegreeToArcmin(value - degree));
            var arcminDeg = ArcminToDegree(arcmin);

            var arcsec = Math.Round(DegreeToArcsec(value - degree - arcminDeg), 2);
            if (arcsec == 60) {
                /* If arcsec got rounded to 60 add to arcmin instead */
                arcsec = 0;
                arcmin += 1;

                if (arcmin == 60) {
                    /* If arcmin got rounded to 60 add to degree instead */
                    arcmin = 0;
                    degree += 1;
                }
            }

            return string.Format(pattern, degree, arcmin, arcsec);
        }

        /// <summary>
        /// Formats a given degree value into format "DD MM SS"
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static string DegreesToFitsDMS(double deg) {
            return DegreesToDMS(deg).Replace("°", "").Replace("'", "").Replace("\"", ""); ;
        }

        /// <summary>
        /// Formats a given degree value into format "DD:MM:SS"
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static string DegreesToHMS(double deg) {
            return DegreesToDMS(DegreesToHours(deg), "{0:00}:{1:00}:{2:00}");
        }

        /// <summary>
        /// Formats a given hours value into format "HH:MM:SS"
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static string HoursToHMS(double hours) {
            return DegreesToDMS(hours, "{0:00}:{1:00}:{2:00}");
        }

        /// <summary>
        /// Formats a given hours value into format "HH MM SS"
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static string HoursToFitsHMS(double hours) {
            return HoursToHMS(hours).Replace(':', ' ');
        }

        private static Tuple<NOVAS.SkyPosition, NOVAS.SkyPosition> GetMoonAndSunPosition(DateTime date, double jd) {
            var deltaT = DeltaT(date);

            var obs = new NOVAS.Observer() {
                OnSurf = new NOVAS.OnSurface() { },
                Where = (short)NOVAS.ObserverLocation.EarthGeoCenter
            };

            var moon = new NOVAS.CelestialObject() {
                Name = "Moon",
                Number = (short)NOVAS.Body.Moon,
                Star = new NOVAS.CatalogueEntry(),
                Type = (short)NOVAS.ObjectType.MajorPlanetSunOrMoon
            };

            var moonPosition = new NOVAS.SkyPosition();

            var jdTt = jd + SecondsToDays(deltaT);

            var err = NOVAS.Place(jdTt, moon, obs, deltaT, NOVAS.CoordinateSystem.EquinoxOfDate, NOVAS.Accuracy.Full, ref moonPosition);

            var sun = new NOVAS.CelestialObject() {
                Name = "Sun",
                Number = (short)NOVAS.Body.Sun,
                Star = new NOVAS.CatalogueEntry(),
                Type = (short)NOVAS.ObjectType.MajorPlanetSunOrMoon
            };

            var sunPosition = new NOVAS.SkyPosition();

            NOVAS.Place(jdTt, sun, obs, deltaT, NOVAS.CoordinateSystem.EquinoxOfDate, NOVAS.Accuracy.Full, ref sunPosition);

            return new Tuple<NOVAS.SkyPosition, NOVAS.SkyPosition>(moonPosition, sunPosition);
        }

        private static double GetMoonPositionAngle(DateTime date) {
            var jd = GetJulianDate(date);
            var tuple = GetMoonAndSunPosition(date, jd);
            var moonPosition = tuple.Item1;
            var sunPosition = tuple.Item2;

            var diff = HoursToDegrees(moonPosition.RA - sunPosition.RA);
            var positionAngle = (diff) % 180;

            return positionAngle;
        }

        private static double CalculateMoonIllumination(DateTime date) {
            var jd = GetJulianDate(date);
            var tuple = GetMoonAndSunPosition(date, jd);
            var moonPosition = tuple.Item1;
            var sunPosition = tuple.Item2;

            var raDiffRad = ToRadians(HoursToDegrees(sunPosition.RA - moonPosition.RA));
            var moonDecRad = ToRadians(moonPosition.Dec);
            var sunDecRad = ToRadians(sunPosition.Dec);

            var phi = Math.Acos(
                        Math.Sin(sunDecRad) * Math.Sin(moonDecRad) +
                        Math.Cos(sunDecRad) * Math.Cos(moonDecRad) * Math.Cos(raDiffRad)
                      );

            var phaseAngle = Math.Atan2(sunPosition.Dis * Math.Sin(phi), moonPosition.Dis - sunPosition.Dis * Math.Cos(phi));

            var illuminatedFraction = (1.0 + Math.Cos(phaseAngle)) / 2.0;

            return illuminatedFraction;
        }

        public static double SecondsToDays(double seconds) {
            return seconds * (1.0 / (60.0 * 60.0 * 24.0));
        }

        public static MoonPhase GetMoonPhase(DateTime date) {
            var angle = GetMoonPositionAngle(date);

            if ((angle >= -180.0 && angle < -135.0) || angle == 180.0) {
                return MoonPhase.FullMoon;
            } else if (angle >= -135.0 && angle < -90.0) {
                return MoonPhase.WaningGibbous;
            } else if (angle >= -90.0 && angle < -45.0) {
                return MoonPhase.LastQuarter;
            } else if (angle >= -45 && angle < 0.0) {
                return MoonPhase.WaningCrescent;
            } else if (angle >= 0.0 && angle < 45.0) {
                return MoonPhase.NewMoon;
            } else if (angle >= 45.0 && angle < 90.0) {
                return MoonPhase.WaxingCrescent;
            } else if (angle >= 90.0 && angle < 135.0) {
                return MoonPhase.FirstQuarter;
            } else if (angle >= 135.0 && angle < 180.0) {
                return MoonPhase.WaxingGibbous;
            } else {
                return MoonPhase.Unknown;
            }
        }

        public static double GetMoonIllumination(DateTime date) {
            return CalculateMoonIllumination(date);
        }

        /// <summary>
        /// Calculates arcseconds per pixel for given pixelsize and focallength
        /// </summary>
        /// <param name="pixelSize">Pixel size in microns</param>
        /// <param name="focalLength">Focallength in mm</param>
        /// <returns></returns>
        public static double ArcsecPerPixel(double pixelSize, double focalLength) {
            // arcseconds inside one radian and compensated by the difference of microns in pixels and mm in focal length
            var factor = DegreeToArcsec(ToDegree(1)) / 1000d;
            return (pixelSize / focalLength) * factor;
        }

        public static double MaxFieldOfView(double arcsecPerPixel, double width, double height) {
            return Astrometry.ArcsecToArcmin(arcsecPerPixel * Math.Max(width, height));
        }

        public static double FieldOfView(double arcsecPerPixel, double width) {
            return Astrometry.ArcsecToArcmin(arcsecPerPixel * width);
        }

        public enum MoonPhase {
            Unknown,
            FullMoon,
            WaningGibbous,
            LastQuarter,
            WaningCrescent,
            NewMoon,
            WaxingCrescent,
            FirstQuarter,
            WaxingGibbous
        }
    }

    [Serializable()]
    [XmlRoot(nameof(Coordinates))]
    public class Coordinates {

        private Coordinates() {
        }

        public enum RAType {
            Degrees,
            Hours
        }

        /// <summary>
        /// Right Ascension in hours
        /// </summary>
        [XmlElement(nameof(RA))]
        public double RA { get; set; }

        [XmlIgnore]
        public string RAString {
            get {
                return Astrometry.DegreesToHMS(RADegrees);
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
        [XmlElement(nameof(Dec))]
        public double Dec { get; set; }

        [XmlIgnore]
        public string DecString {
            get {
                return Astrometry.DegreesToDMS(Dec);
            }
        }

        /// <summary>
        /// Epoch the coordinates are stored in. Either J2000 or JNOW
        /// </summary>
        [XmlElement(nameof(Epoch))]
        public Epoch Epoch { get; set; }

        /// <summary>
        /// Creates new coordinates
        /// </summary>
        /// <param name="ra">    Right Ascension in degrees or hours. RAType has to be set accordingly</param>
        /// <param name="dec">   Declination in degrees</param>
        /// <param name="epoch"> J2000|JNOW</param>
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
                return new Coordinates(this.RA, this.Dec, this.Epoch, RAType.Hours);
            }

            if (targetEpoch == Epoch.JNOW) {
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
            double jdTT = GetJdTTNow();

            double ri = 0, di = 0, eo = 0;
            SOFA.CelestialToIntermediate(Astrometry.ToRadians(RADegrees), Astrometry.ToRadians(Dec), 0.0, 0.0, 0.0, 0.0, jdTT, 0.0, ref ri, ref di, ref eo);

            double raApparent = Astrometry.ToDegree(SOFA.Anp(ri - eo));
            double decApparent = Astrometry.ToDegree(di);

            return new Coordinates(raApparent, decApparent, Epoch.JNOW, RAType.Degrees);
        }

        private double GetJdTTNow() {
            var utcNow = DateTime.UtcNow;
            double utc1 = 0, utc2 = 0, tai1 = 0, tai2 = 0, tt1 = 0, tt2 = 0;
            GetJdUTCNow(ref utc1, ref utc2);
            SOFA.UtcTai(utc1, utc2, ref tai1, ref tai2);
            SOFA.TaiTt(tai1, tai2, ref tt1, ref tt2);

            return tt1 + tt2;
        }

        private void GetJdUTCNow(ref double utc1, ref double utc2) {
            var utcNow = DateTime.UtcNow;
            SOFA.Dtf2d("UTC", utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, (double)utcNow.Second + (double)utcNow.Millisecond / 1000.0, ref utc1, ref utc2);
        }

        private double GetJdUTCNow() {
            var utcNow = DateTime.UtcNow;
            double utc1 = 0, utc2 = 0;
            GetJdUTCNow(ref utc1, ref utc2);
            return utc1 + utc2;
        }

        /// <summary>
        /// Transforms coordinates from JNOW to J2000
        /// </summary>
        /// <returns></returns>
        private Coordinates TransformToJ2000() {
            var jdTT = GetJdTTNow();
            var jdUTC = GetJdUTCNow();
            double rc = 0, dc = 0, eo = 0;
            SOFA.IntermediateToCelestial(SOFA.Anp(Astrometry.ToRadians(RADegrees) + SOFA.Eo06a(jdUTC, 0.0)), Astrometry.ToRadians(Dec), jdTT, 0.0, ref rc, ref dc, ref eo);

            var raCelestial = Astrometry.ToDegree(rc);
            var decCelestial = Astrometry.ToDegree(dc);

            return new Coordinates(raCelestial, decCelestial, Epoch.J2000, RAType.Degrees);
        }

        /// <summary>
        /// Shift coordinates by a delta in degree
        /// </summary>
        /// <param name="deltaX">delta x in degree</param>
        /// <param name="deltaY">delta y in degree</param>
        /// <param name="rotation">rotation relative to delta values</param>
        /// <returns></returns>
        public Coordinates Shift(double deltaX, double deltaY, double rotation) {
            var deltaXDeg = -deltaX;
            var deltaYDeg = -deltaY;
            var rotationRad = Astrometry.ToRadians(rotation);

            if (rotation != 0) {
                //Recalculate delta based on rotation
                //No spherical or other aberrations are assumed
                var originalDeltaX = deltaXDeg;
                deltaXDeg = deltaXDeg * Math.Cos(rotationRad) - deltaYDeg * Math.Sin(rotationRad);
                deltaYDeg = deltaYDeg * Math.Cos(rotationRad) + originalDeltaX * Math.Sin(rotationRad);
            }

            var originRARad = Astrometry.ToRadians(this.RADegrees);
            var originDecRad = Astrometry.ToRadians(this.Dec);

            var deltaXRad = Astrometry.ToRadians(deltaXDeg);
            var deltaYRad = Astrometry.ToRadians(deltaYDeg);

            // refer to http://faculty.wcas.northwestern.edu/nchapman/coding/worldpos.py

            var targetRARad = originRARad + Math.Atan2(deltaXRad, Math.Cos(originDecRad) - deltaYRad * Math.Sin(originDecRad));
            var targetDecRad =
                Math.Atan(
                    Math.Cos(targetRARad - originRARad)
                    * (deltaYRad * Math.Cos(originDecRad) + Math.Sin(originDecRad))
                    / (Math.Cos(originDecRad) - deltaYRad * Math.Sin(originDecRad))
                );

            var targetRA = Astrometry.ToDegree(targetRARad);
            if (targetRA < 0) { targetRA += 360; }
            if (targetRA >= 360) { targetRA -= 360; }

            var targetDec = Astrometry.ToDegree(targetDecRad);

            return new Coordinates(
                targetRA,
                targetDec,
                Epoch.J2000,
                Coordinates.RAType.Degrees
            );
        }

        /// <summary>
        /// Shift coordinates by a delta in pixel
        /// </summary>
        /// <param name="origin">Coordinates to shift from</param>
        /// <param name="deltaX">delta x</param>
        /// <param name="deltaY">delta y</param>
        /// <param name="rotation">rotation relative to delta values</param>
        /// <param name="scaleX">scale relative to deltaX in arcsecs</param>
        /// <param name="scaleY">scale raltive to deltaY in arcsecs</param>
        /// <returns></returns>
        public Coordinates Shift(
                double deltaX,
                double deltaY,
                double rotation,
                double scaleX,
                double scaleY
        ) {
            var deltaXDeg = deltaX * Astrometry.ArcsecToDegree(scaleX);
            var deltaYDeg = deltaY * Astrometry.ArcsecToDegree(scaleY);
            return this.Shift(deltaXDeg, deltaYDeg, rotation);
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

    public abstract class RiseAndSetEvent {

        public RiseAndSetEvent(DateTime date, double latitude, double longitude) {
            this.Date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public DateTime Date { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public DateTime? Rise { get; private set; }
        public DateTime? Set { get; private set; }

        protected abstract double AdjustAltitude(Body body);

        protected abstract Body GetBody(DateTime date);

        /// <summary>
        /// Calculates rise and set time for the sun
        /// Caveat: does not consider more than two rises
        /// </summary>
        /// <returns></returns>
        public Task<bool> Calculate() {
            return Task.Run(async () => {
                // Check rise and set events in two hour periods
                var offset = 0;

                do {
                    // Shift date by offset
                    var offsetDate = Date.AddHours(offset);

                    // Get three sun locations for date, date + 1 hour and date + 2 hours
                    var bodyAt0 = GetBody(offsetDate);
                    var bodyAt1 = GetBody(offsetDate.AddHours(1));
                    var bodyAt2 = GetBody(offsetDate.AddHours(2));

                    await Task.WhenAll(bodyAt0.Calculate(), bodyAt1.Calculate(), bodyAt2.Calculate());

                    var location = new NOVAS.OnSurface() {
                        Latitude = Latitude,
                        Longitude = Longitude
                    };

                    // Adjust altitude for the three sunrise event
                    var altitude0 = AdjustAltitude(bodyAt0);
                    var altitude1 = AdjustAltitude(bodyAt1);
                    var altitude2 = AdjustAltitude(bodyAt2);

                    // Normalized quadratic equation parameters
                    var a = 0.5 * (altitude2 + altitude0) - altitude1;
                    var b = 0.5 * (altitude2 - altitude0);
                    var c = altitude0;

                    // x = -b +- Sqrt(b² - 4ac) / 2a   --- https://de.khanacademy.org/math/algebra/quadratics/solving-quadratics-using-the-quadratic-formula/a/discriminant-review

                    var xAxisSymmetry = -b / (2.0 * a);
                    var discriminant = (Math.Pow(b, 2)) - (4.0 * a * c);

                    var zeroPoint1 = double.NaN;
                    var zeroPoint2 = double.NaN;
                    var events = 0;

                    if (discriminant > 0) {
                        // Zero points detected
                        var delta = 0.5 * Math.Sqrt(discriminant) / Math.Abs(a);
                        zeroPoint1 = xAxisSymmetry - delta;
                        zeroPoint2 = xAxisSymmetry + delta;

                        if (Math.Abs(zeroPoint1) <= 1) {
                            events++;
                        }
                        if (Math.Abs(zeroPoint2) <= 1) {
                            events++;
                        }
                        if (zeroPoint1 < -1.0) {
                            zeroPoint1 = zeroPoint2;
                        }
                    }

                    var gradient = 2 * a * zeroPoint1 + b;

                    if (events == 1) {
                        if (gradient > 0) {
                            // rise
                            this.Rise = offsetDate.AddHours(zeroPoint1);
                        } else {
                            // set
                            this.Set = offsetDate.AddHours(zeroPoint1);
                        }
                    } else if (events == 2) {
                        if (gradient > 0) {
                            // rise and set
                            this.Rise = offsetDate.AddHours(zeroPoint1);
                            this.Set = offsetDate.AddHours(zeroPoint2);
                        } else {
                            // set and rise
                            this.Rise = offsetDate.AddHours(zeroPoint2);
                            this.Set = offsetDate.AddHours(zeroPoint1);
                        }
                    }
                    offset += 2;
                } while (!((this.Rise != null && this.Set != null) || offset > 24));

                return true;
            });
        }
    }

    public class SunRiseAndSet : RiseAndSetEvent {

        public SunRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        private double SunRiseDegree {
            get {
                //http://aa.usno.navy.mil/faq/docs/RST_defs.php #Paragraph Sunrise and sunset
                return Astrometry.ArcminToDegree(-50);
            }
        }

        protected override double AdjustAltitude(Body body) {
            return body.Altitude - SunRiseDegree;
        }

        protected override Body GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude);
        }
    }

    public class AstronomicalTwilightRiseAndSet : RiseAndSetEvent {

        public AstronomicalTwilightRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        private double AstronomicalTwilightDegree {
            get {
                //http://aa.usno.navy.mil/faq/docs/RST_defs.php #Paragraph Astronomical twilight
                return -18;
            }
        }

        protected override double AdjustAltitude(Body body) {
            return body.Altitude - AstronomicalTwilightDegree;
        }

        protected override Body GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude);
        }
    }

    public class MoonRiseAndSet : RiseAndSetEvent {

        public MoonRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        protected override double AdjustAltitude(Body body) {
            /* Due to the moon being close and orbit not being circular enough, altitude is adjusted accordingly */
            var horizon = 90.0;
            var location = new NOVAS.OnSurface() {
                Latitude = Latitude,
                Longitude = Longitude
            };
            var refraction = NOVAS.Refract(ref location, NOVAS.RefractionOption.StandardRefraction, horizon);

            var altitude = body.Altitude - Astrometry.ToDegree(Earth.Radius) / body.Distance + Astrometry.ToDegree(body.Radius) / body.Distance + refraction;
            return altitude;
        }

        protected override Body GetBody(DateTime date) {
            return new Moon(date, Latitude, Longitude);
        }
    }

    public class Earth {

        public static double Radius {
            get {
                return 6371; // https://de.wikipedia.org/wiki/Erdradius
            }
        }
    }

    public abstract class Body {

        public Body(DateTime date, double latitude, double longitude) {
            this.Date = date;
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public DateTime Date { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Distance { get; protected set; }
        public double Altitude { get; protected set; }

        public abstract double Radius { get; }
        protected abstract string Name { get; }
        protected abstract NOVAS.Body BodyNumber { get; }

        public Task Calculate() {
            return Task.Run(() => {
                var jd = Astrometry.GetJulianDate(Date);
                var deltaT = Astrometry.DeltaT(Date);

                var location = new NOVAS.OnSurface() {
                    Latitude = Latitude,
                    Longitude = Longitude
                };

                var observer = new NOVAS.Observer() {
                    OnSurf = location,
                    Where = (short)NOVAS.ObserverLocation.EarthGeoCenter
                };

                var obj = new NOVAS.CelestialObject() {
                    Name = Name,
                    Number = (short)BodyNumber,
                    Star = new NOVAS.CatalogueEntry(),
                    Type = (short)NOVAS.ObjectType.MajorPlanetSunOrMoon
                };

                var objPosition = new NOVAS.SkyPosition();

                NOVAS.Place(jd + Astrometry.SecondsToDays(deltaT), obj, observer, deltaT, NOVAS.CoordinateSystem.EquinoxOfDate, NOVAS.Accuracy.Full, ref objPosition);

                this.Distance = Astrometry.AUToKilometer(objPosition.Dis);

                var siderealTime = Astrometry.GetLocalSiderealTime(Date, Longitude);
                var hourAngle = Astrometry.HoursToDegrees(Astrometry.GetHourAngle(siderealTime, objPosition.RA));
                this.Altitude = Astrometry.GetAltitude(hourAngle, Latitude, objPosition.Dec);
            });
        }
    }

    public class Sun : Body {

        public Sun(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        public override double Radius {
            get {
                return 696342; // https://de.wikipedia.org/wiki/Sonnenradius
            }
        }

        protected override string Name {
            get {
                return "Sun";
            }
        }

        protected override NOVAS.Body BodyNumber {
            get {
                return NOVAS.Body.Sun;
            }
        }
    }

    public class Moon : Body {

        public Moon(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        public override double Radius {
            get {
                return 1738; // https://de.wikipedia.org/wiki/Monddurchmesser
            }
        }

        protected override string Name {
            get {
                return "Moon";
            }
        }

        protected override NOVAS.Body BodyNumber {
            get {
                return NOVAS.Body.Moon;
            }
        }
    }
}