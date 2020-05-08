#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Database;
using Nito.AsyncEx;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace NINA.Utility.Astrometry {

    public class Astrometry {

        static Astrometry() {
            _ = new EarthRotationParameterUpdater().Update();
        }

        private static double DegreeToRadiansFactor = Math.PI / 180d;
        private static double RadiansToDegreeFactor = 180d / Math.PI;
        private static double RadianstoHourFactor = 12d / Math.PI;
        private static double DaysToSecondsFactor = 60d * 60d * 24d;
        private static double SecondsToDaysFactor = 1.0 / (60d * 60d * 24d);

        /// <summary>
        /// Convert degree to radians
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static double ToRadians(double val) {
            return DegreeToRadiansFactor * val;
        }

        /// <summary>
        /// Convert radians to degree
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double ToDegree(double angle) {
            return angle * RadiansToDegreeFactor;
        }

        /// <summary>
        /// Convert radians to hour
        /// </summary>
        /// <param name="radian"></param>
        /// <returns></returns>
        public static double RadianToHour(double radian) {
            return radian * RadianstoHourFactor;
        }

        public static double DegreeToArcmin(double degree) {
            return degree * 60d;
        }

        public static double DegreeToArcsec(double degree) {
            return degree * 60d * 60d;
        }

        public static double ArcminToArcsec(double arcmin) {
            return arcmin * 60d;
        }

        public static double ArcminToDegree(double arcmin) {
            return arcmin / 60d;
        }

        public static double ArcsecToArcmin(double arcsec) {
            return arcsec / 60d;
        }

        public static double ArcsecToDegree(double arcsec) {
            return arcsec / 60d / 60d;
        }

        public static double HoursToDegrees(double hours) {
            return hours * 15d;
        }

        public static double DegreesToHours(double deg) {
            return deg / 15d;
        }

        public static float EuclidianModulus(float x, float y) {
            return (float)EuclidianModulus((double)x, (double)y);
        }

        public static double EuclidianModulus(double x, double y) {
            if (y > 0) {
                double r = x % y;
                if (r < 0) {
                    return r + y;
                } else {
                    return r;
                }
            } else if (y < 0) {
                return -1 * EuclidianModulus(-1 * x, -1 * y);
            } else {
                return double.NaN;
            }
        }

        public static double GetLocalSiderealTimeNow(double longitude) {
            return GetLocalSiderealTime(DateTime.Now, longitude);
        }

        public static double GetJulianDate(DateTime date) {
            var utcdate = date.ToUniversalTime();
            return NOVAS.JulianDate((short)utcdate.Year, (short)utcdate.Month, (short)utcdate.Day, utcdate.Hour + utcdate.Minute / 60.0 + utcdate.Second / 60.0 / 60.0 + utcdate.Millisecond / 60.0 / 60.0 / 1000.0);
        }

        public static double MathMod(double a, double b) {
            return EuclidianModulus(a, b);
        }

        /// <summary>
        /// Calculates the value of DeltaT using SOFA
        /// Published DeltaT information: https://www.usno.navy.mil/USNO/earth-orientation/eo-products/long-term
        /// Formula: deltaT = 32.184 + (TAI - UTC) - (UT1 - UTC) https://de.wikipedia.org/wiki/Delta_T
        /// </summary>
        /// <param name="date">Date to retrieve DeltaT for</param>
        /// <returns>DeltaT at given date</returns>
        public static double DeltaT(DateTime date) {
            var utcDate = date.ToUniversalTime();
            double utc1 = 0, utc2 = 0, tai1 = 0, tai2 = 0;
            SOFA.Dtf2d("UTC", utcDate.Year, utcDate.Month, utcDate.Day, utcDate.Hour, utcDate.Minute, (double)utcDate.Second + (double)utcDate.Millisecond / 1000.0, ref utc1, ref utc2);
            SOFA.UtcTai(utc1, utc2, ref tai1, ref tai2);

            var utc = utc1 + utc2;
            var tai = tai1 + tai2;
            var deltaT = 32.184 + DaysToSeconds((tai - utc)) - DeltaUT(utcDate);
            return deltaT;
        }

        private static double DeltaUTToday = 0.0;
        private static double DeltaUTYesterday = 0.0;

        /// <summary>
        /// Retrieve UT1 - UTC approximation to adjust DeltaT
        /// </summary>
        /// <param name="date"></param>
        /// <returns>UT1 - UTC in seconds</returns>
        /// <remarks>https://www.iers.org/IERS/EN/DataProducts/EarthOrientationData/eop.html</remarks>
        public static double DeltaUT(DateTime date) {
            if (date.Date == DateTime.UtcNow.Date) {
                if (DeltaUTToday != 0) {
                    return DeltaUTToday;
                }
            }

            if (date.Date == DateTime.UtcNow.Date - TimeSpan.FromDays(1)) {
                if (DeltaUTYesterday != 0) {
                    return DeltaUTYesterday;
                }
            }

            var utcDate = date.ToUniversalTime();

            var db = new DatabaseInteraction();
            var deltaUT = 0d;
            try {
                deltaUT = AsyncContext.Run(() => db.GetUT1_UTC(utcDate, default));
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            if (date.Date == DateTime.UtcNow.Date) {
                DeltaUTToday = deltaUT;
            }

            if (date.Date == DateTime.UtcNow.Date - TimeSpan.FromDays(1)) {
                DeltaUTYesterday = deltaUT;
            }

            return deltaUT;
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
            return GetHourAngle(Angle.ByHours(siderealTime), Angle.ByHours(rightAscension)).Hours;
        }

        public static Angle GetHourAngle(Angle siderealTime, Angle rightAscension) {
            var hourAngle = siderealTime - rightAscension;
            if (hourAngle.Hours < 0) { hourAngle = Angle.ByHours(hourAngle.Hours + 24); }
            return hourAngle;
        }

        public static Angle GetRightAscensionFromHourAngle(Angle hourAngle, Angle siderealTime) {
            var ra = siderealTime - hourAngle;
            return ra;
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
            return GetAltitude(Angle.ByDegree(hourAngle), Angle.ByDegree(latitude), Angle.ByDegree(declination)).Degree;
        }

        /// <summary>
        /// Calculates altitude for given location and time
        /// </summary>
        /// <param name="hourAngle"></param>
        /// <param name="latitude"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static Angle GetAltitude(Angle hourAngle, Angle latitude, Angle declination) {
            return (declination.Sin() * latitude.Sin() + declination.Cos() * latitude.Cos() * hourAngle.Cos()).Asin();
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
            return GetAzimuth(Angle.ByDegree(hourAngle), Angle.ByDegree(altitude), Angle.ByDegree(latitude), Angle.ByDegree(declination)).Degree;
        }

        /// <summary>
        /// Calculates azimuth for given location and time
        /// </summary>
        /// <param name="hourAngle"></param>
        /// <param name="altitude"></param>
        /// <param name="latitude"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static Angle GetAzimuth(Angle hourAngle, Angle altitude, Angle latitude, Angle declination) {
            var cosAz = (declination.Sin() - altitude.Sin() * latitude.Sin()) / (altitude.Cos() * latitude.Cos());

            //fix double precision issues
            if (cosAz.Radians < -1) { cosAz = Angle.ByRadians(-1); }
            if (cosAz.Radians > 1) { cosAz = Angle.ByRadians(1); }

            if (hourAngle.Sin().Radians < 0) {
                return cosAz.Acos();
            } else {
                return Angle.ByDegree(360 - cosAz.Acos().Degree);
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
            if (deg >= 0) {
                return String.Concat("+", DegreesToDMS(deg).Replace("°", "").Replace("'", "").Replace("\"", ""));
            } else {
                return DegreesToDMS(deg).Replace("°", "").Replace("'", "").Replace("\"", "");
            }
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

        /// <summary>
        /// Restores double degree value out of dms string
        /// </summary>
        /// <param name="hms">hms string</param>
        /// <returns>value in degree</returns>
        public static double HMSToDegrees(string hms) {
            return HoursToDegrees(DMSToDegrees(hms));
        }

        /// <summary>
        /// Restores double degree value out of dms string
        /// </summary>
        /// <param name="dms">dms string</param>
        /// <returns>value in degree</returns>
        public static double DMSToDegrees(string dms) {
            dms = dms.Trim();

            double signFactor = 1d;
            if (dms.Contains('-')) {
                signFactor = -1d;
            }

            var pattern = "[0-9\\.]+";
            if (dms.Contains(",")) {
                pattern = "[0-9\\,]+";
            }
            var regex = new Regex(pattern);

            var matches = regex.Matches(dms);

            double degree = 0, minutes = 0, seconds = 0;

            if (matches.Count > 0) {
                degree = double.Parse(matches[0].Value, CultureInfo.InvariantCulture);

                if (matches.Count > 1) {
                    minutes = ArcminToDegree(double.Parse(matches[1].Value, CultureInfo.InvariantCulture));
                }

                if (matches.Count > 2) {
                    seconds = ArcsecToDegree(double.Parse(matches[2].Value, CultureInfo.InvariantCulture));
                }
            }

            return signFactor * (degree + minutes + seconds);
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
            if (diff > 180) {
                return diff - 360;
            } else {
                return diff;
            }
        }

        private static double CalculateMoonIllumination(DateTime date) {
            var jd = GetJulianDate(date);
            var tuple = GetMoonAndSunPosition(date, jd);
            var moonPosition = tuple.Item1;
            var sunPosition = tuple.Item2;

            var sunRAAngle = Angle.ByHours(sunPosition.RA);
            var sunDecAngle = Angle.ByDegree(sunPosition.Dec);
            var moonRAAngle = Angle.ByHours(moonPosition.RA);
            var moonDecAngle = Angle.ByDegree(moonPosition.Dec);

            var phi = (
                sunDecAngle.Sin() * moonDecAngle.Sin()
                + sunDecAngle.Cos() * moonDecAngle.Cos() * (sunRAAngle - moonRAAngle).Cos()
                ).Acos();

            var phaseAngle = Angle.Atan2(
                sunPosition.Dis * phi.Sin(),
                moonPosition.Dis - sunPosition.Dis * phi.Cos()
            );

            var illuminatedFraction = (1.0 + phaseAngle.Cos().Radians) / 2.0;

            return illuminatedFraction;
        }

        public static double SecondsToDays(double seconds) {
            return seconds * SecondsToDaysFactor;
        }

        public static double DaysToSeconds(double days) {
            return days * DaysToSecondsFactor;
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

        /// <summary>
        /// Returns the polar alignment error during a drift in degree for the given measurements
        /// </summary>
        /// <param name="startDeclination">Starting position of drift alignment in degrees</param>
        /// <param name="driftRate">drift rate in degrees</param>
        /// <param name="declinationError">Determined delta between start declination and end declination in degrees</param>
        /// <returns></returns>
        /// <remarks>
        /// Hook's equation => δ(err) = (t * cos(δ) * θ(r) / 4) * 60 * 60 ===> δ(err) = 900 * t * cos(δ) * θ(r)
        /// ==> t = time to drift in minutes
        /// ==> δ = Declination of drift target
        /// ==> θ(r) = alignment error in radians
        /// ==> δ(err) = declination drift in arcseconds
        ///
        /// Solving for alignError = δ(err) / (900 * t * cos(δ)) ==> yields align error in radians
        /// Express error in degree ==> δ(err) / (900 * t * cos(δ)) * (180/π)
        /// Factor 4 is simplified for drift rate in degrees converted to minutes</remarks>
        /// <see cref="http://celestialwonders.com/articles/polaralignment/PolarAlignmentAccuracy.pdf"/>
        public static double DetermineDriftAlignError(double startDeclination, double driftRate, double declinationError) {
            double δ = ToRadians(startDeclination);
            double δerr = DegreeToArcsec(declinationError);
            double t = driftRate * 4;

            return ToDegree(δerr / (900 * t * Math.Cos(δ)));
        }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Epoch {

        [Description("LblJNOW")]
        JNOW,

        [Description("LblB1950")]
        B1950,

        [Description("LblJ2000")]
        J2000,

        [Description("LblJ2050")]
        J2050
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
}