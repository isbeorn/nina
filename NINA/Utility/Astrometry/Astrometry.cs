using System;
using System.ComponentModel;
using System.Linq;

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

            var arcsec = Math.Round(DegreeToArcsec(value - degree - arcminDeg), 0);
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

        /// <summary>
        /// Approximate dew point using Magnus Formula
        /// </summary>
        /// <param name="temperatrue">Temperature in Celsius</param>
        /// <param name="humidity">Humidity</param>
        /// <returns></returns>
        /// <remarks>
        /// https://en.wikipedia.org/wiki/Dew_point#Calculating_the_dew_point
        /// Using b and c values from Journal of Applied Meteorology and Climatology
        /// </remarks>
        public static double ApproximateDewPoint(double temperatrue, double humidity) {
            // 0°C <= T <= 50°C ==> Error <= 0.05%
            double b = 17.368;
            double c = 233.88;
            if (temperatrue < 0) {
                // -40°C <= T < 0°C ==> Error <= 0.06%
                b = 17.966;
                c = 247.15;
            }

            var γTRH = Math.Log(humidity / 100d) + ((b * temperatrue) / (c + temperatrue));

            var dP = (c * γTRH) / (b - γTRH);
            return dP;
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