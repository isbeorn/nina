#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.RiseAndSet;
using NINA.Core.Database;
using NINA.Core.Utility;
using NINA.Profile;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace NINA.Astrometry {

    public class AstroUtil {

        private const double DegreeToRadiansFactor = Math.PI / 180d;
        private const double RadiansToDegreeFactor = 180d / Math.PI;
        private const double RadianstoHourFactor = 12d / Math.PI;
        private const double DaysToSecondsFactor = 60d * 60d * 24d;
        private const double SecondsToDaysFactor = 1.0 / (60d * 60d * 24d);
        public const double SIDEREAL_RATE_ARCSECONDS_PER_SECOND = 15.041;
        private const double ArcSecPerPixConversionFactor = RadiansToDegreeFactor * 60d * 60d / 1000d;

        public const string HMSPattern = @"(([0-9]{1,2})([h|:| ]|[?]{2}|[h|r]{2})\s*([0-9]{1,2})([m|'|′|:| ]|[?]{2})?\s*([0-9]{1,2}(?:\.[0-9]+){0,1})?([s|""|″|:| ]|[?]{2})?\s*)";
        public const string DMSPattern = @"([\+|-]?([0-9]{1,2})([d|°|º|:| ]|[?]{2})\s*([0-9]{1,2})([m|'|′|:| ]|[?]{2})\s*([0-9]{1,2}(?:\.[0-9]+)?)?([s|""|″|:| ]|[?]{2})?\s*)";

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
        public static double DeltaT(DateTime date, DatabaseInteraction db = null) {
            var utcDate = date.ToUniversalTime();
            double utc1 = 0, utc2 = 0, tai1 = 0, tai2 = 0;
            SOFA.Dtf2d("UTC", utcDate.Year, utcDate.Month, utcDate.Day, utcDate.Hour, utcDate.Minute, (double)utcDate.Second + (double)utcDate.Millisecond / 1000.0, ref utc1, ref utc2);
            SOFA.UtcTai(utc1, utc2, ref tai1, ref tai2);

            var utc = utc1 + utc2;
            var tai = tai1 + tai2;
            var deltaT = 32.184 + DaysToSeconds((tai - utc)) - DeltaUT(utcDate, db);
            return deltaT;
        }

        private static double DeltaUTToday = 0.0;
        private static double DeltaUTYesterday = 0.0;
        private static double DeltaUTTomorrow = 0.0;
        private static ConcurrentDictionary<DateTime, double> DeltaUTCache = new ConcurrentDictionary<DateTime, double>();
        private static DateTime DeltaUTReference;

        /// <summary>
        /// Retrieve UT1 - UTC approximation to adjust DeltaT
        /// </summary>
        /// <param name="date"></param>
        /// <returns>UT1 - UTC in seconds</returns>
        /// <remarks>https://www.iers.org/IERS/EN/DataProducts/EarthOrientationData/eop.html</remarks>
        public static double DeltaUT(DateTime date, DatabaseInteraction db = null) {
            if (DeltaUTReference != DateTime.UtcNow.Date) {
                // Clear the cache when a app is open longer than a day
                DeltaUTReference = DateTime.UtcNow.Date;
                DeltaUTYesterday = 0d;
                DeltaUTToday = 0d;
                DeltaUTTomorrow = 0d;
            }

            var utcDate = date.ToUniversalTime();

            if (utcDate.Date == DateTime.UtcNow.Date) {
                if (DeltaUTToday != 0) {
                    return DeltaUTToday;
                }
            }

            if (utcDate.Date == DateTime.UtcNow.Date - TimeSpan.FromDays(1)) {
                if (DeltaUTYesterday != 0) {
                    return DeltaUTYesterday;
                }
            }

            if (utcDate.Date == DateTime.UtcNow.Date + TimeSpan.FromDays(1)) {
                if (DeltaUTTomorrow != 0) {
                    return DeltaUTTomorrow;
                }
            }

            var deltaUT = 0d;
            if (DeltaUTCache.TryGetValue(utcDate.Date, out deltaUT)) {
                if(deltaUT != 0) {
                    return deltaUT;
                }
            }

            db = db ?? new DatabaseInteraction();
            try {
                deltaUT = AsyncContext.Run(() => db.GetUT1_UTC(utcDate, default));
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            if (utcDate.Date == DateTime.UtcNow.Date) {
                DeltaUTToday = deltaUT;
            }

            if (utcDate.Date == DateTime.UtcNow.Date - TimeSpan.FromDays(1)) {
                DeltaUTYesterday = deltaUT;
            }

            if (utcDate.Date == DateTime.UtcNow.Date + TimeSpan.FromDays(1)) {
                DeltaUTTomorrow = deltaUT;
            }

            try {
                if (!DeltaUTCache.ContainsKey(utcDate.Date)) {
                    DeltaUTCache.AddOrUpdate(utcDate.Date, deltaUT, (a, b) => b);
                }                
            } catch(Exception) { }
            

            return deltaUT;
        }

        /// <summary>
        /// </summary>
        /// <param name="date">     </param>
        /// <param name="longitude"></param>
        /// <returns>Sidereal Time in hours</returns>
        public static double GetLocalSiderealTime(DateTime date, double longitude, DatabaseInteraction db = null) {
            var jd = GetJulianDate(date);

            long jd_high = (long)jd;
            double jd_low = jd - jd_high;

            double lst = 0;
            NOVAS.SiderealTime(jd_high, jd_low, DeltaT(date, db), NOVAS.GstType.GreenwichApparentSiderealTime, NOVAS.Method.EquinoxBased, NOVAS.Accuracy.Full, ref lst);
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

        public static RiseAndSetEvent GetNauticalNightTimes(DateTime date, double latitude, double longitude) {
            var riseAndSet = new NauticalTwilightRiseAndSet(date, latitude, longitude);
            var t = riseAndSet.Calculate().Result;

            return riseAndSet;
        }

        public static RiseAndSetEvent GetCivilNightTimes(DateTime date, double latitude, double longitude) {
            var riseAndSet = new CivilTwilightRiseAndSet(date, latitude, longitude);
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

            // Prevent "-0" when using ToString
            if(arcsec == 0) { arcsec = 0; }
            if(arcmin == 0) { arcmin = 0; }
            if(degree == 0) { degree = 0; }

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
            if (hours == double.MaxValue) return string.Empty;

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

        /// <summary>
        /// Attempts to determine if a string is a recognizable DMS format
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true if string matches a recognizable DMS format</returns>
        /// <example>
        /// -05 24 44.3 => True
        /// 055° 3' 45" => True
        /// 16 06 01.335 => True
        /// 124.3435463 => False
        /// </example>
        public static bool IsDMS(string value) {
            const string pattern = @"^[-+]?\d{1,3}(\s|°)\d{1,2}(\s|')\d{1,2}(\.\d+)?";
            return Regex.IsMatch(value, pattern);
        }

        /// <summary>
        /// Attempts to determine if a string is a recognizable HMS format
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true if string matches a recognizable HMS format</returns>
        /// <example>
        /// 05 24 44.3 => True
        /// 12:3:45 => True
        /// 16 06 01.335 => True
        /// 15.3435463 => False
        /// </example>
        public static bool IsHMS(string value) {
            const string pattern = @"^\d{1,2}(\s|:)\d{1,2}(\s|:)\d{1,2}(\.\d+)?";
            return Regex.IsMatch(value, pattern);
        }

        public static NOVAS.SkyPosition GetMoonPosition(DateTime date, double jd, ObserverInfo oberverInfo) {
            var deltaT = DeltaT(date);

            var onSurface = new NOVAS.OnSurface() {
                Latitude = oberverInfo.Latitude,
                Longitude = oberverInfo.Longitude,
                Height = oberverInfo.Elevation,
                Temperature = oberverInfo.Temperature,
                Pressure = oberverInfo.Pressure
            };

            var obs = new NOVAS.Observer() {
                OnSurf = onSurface,
                Where = (short)NOVAS.ObserverLocation.EarthSurface
            };

            var celestialObject = new NOVAS.CelestialObject() {
                Name = "Moon",
                Number = (short)NOVAS.Body.Moon,
                Star = new NOVAS.CatalogueEntry(),
                Type = (short)NOVAS.ObjectType.MajorPlanetSunOrMoon
            };

            var skyPosition = new NOVAS.SkyPosition();

            var jdTt = jd + SecondsToDays(deltaT);
            _ = NOVAS.Place(jdTt, celestialObject, obs, deltaT, NOVAS.CoordinateSystem.EquinoxOfDate, NOVAS.Accuracy.Full, ref skyPosition);

            return skyPosition;
        }

        public static NOVAS.SkyPosition GetSunPosition(DateTime date, double jd, ObserverInfo oberverInfo) {
            var deltaT = DeltaT(date);

            var onSurface = new NOVAS.OnSurface() {
                Latitude = oberverInfo.Latitude,
                Longitude = oberverInfo.Longitude,
                Height = oberverInfo.Elevation,
                Temperature = oberverInfo.Temperature,
                Pressure = oberverInfo.Pressure
            };

            var obs = new NOVAS.Observer() {
                OnSurf = onSurface,
                Where = (short)NOVAS.ObserverLocation.EarthSurface
            };

            var celestialObject = new NOVAS.CelestialObject() {
                Name = "Sun",
                Number = (short)NOVAS.Body.Sun,
                Star = new NOVAS.CatalogueEntry(),
                Type = (short)NOVAS.ObjectType.MajorPlanetSunOrMoon
            };

            var skyPosition = new NOVAS.SkyPosition();

            var jdTt = jd + SecondsToDays(deltaT);
            _ = NOVAS.Place(jdTt, celestialObject, obs, deltaT, NOVAS.CoordinateSystem.EquinoxOfDate, NOVAS.Accuracy.Full, ref skyPosition);

            return skyPosition;
        }

        public static Tuple<NOVAS.SkyPosition, NOVAS.SkyPosition> GetMoonAndSunPosition(DateTime date, double jd, ObserverInfo observerInfo = null) {
            if (observerInfo == null) { observerInfo = new ObserverInfo(); }
            return new Tuple<NOVAS.SkyPosition, NOVAS.SkyPosition>(GetMoonPosition(date, jd, observerInfo), GetSunPosition(date, jd, observerInfo));
        }

        public static double GetMoonPositionAngle(DateTime date) {
            var jd = GetJulianDate(date);
            var tuple = GetMoonAndSunPosition(date, jd);
            var moonPosition = tuple.Item1;
            var sunPosition = tuple.Item2;

            var diff = HoursToDegrees(moonPosition.RA - sunPosition.RA);
            if (diff > 180) {
                return diff - 360;
            } else if (diff < -180) {
                return diff + 360;
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

        public static double GetMoonAltitude(DateTime date, ObserverInfo observerInfo) {
            var jd = GetJulianDate(date);
            var tuple = GetMoonAndSunPosition(date, jd);

            var siderealTime = GetLocalSiderealTime(date, observerInfo.Longitude);
            var hourAngle = HoursToDegrees(GetHourAngle(siderealTime, tuple.Item1.RA));

            return GetAltitude(hourAngle, observerInfo.Latitude, tuple.Item1.Dec);
        }

        public static double GetSunAltitude(DateTime date, ObserverInfo observerInfo) {
            var jd = GetJulianDate(date);
            var tuple = GetMoonAndSunPosition(date, jd);

            var siderealTime = GetLocalSiderealTime(date, observerInfo.Longitude);
            var hourAngle = HoursToDegrees(GetHourAngle(siderealTime, tuple.Item2.RA));

            return GetAltitude(hourAngle, observerInfo.Latitude, tuple.Item2.Dec);
        }

        public static double CalculateAltitudeForStandardRefraction(double currentAltitude, double latitude, double longitude) {
            var zenithDistance = 90d - currentAltitude;
            var location = new NOVAS.OnSurface() {
                Latitude = latitude,
                Longitude = longitude
            };
            var refraction = NOVAS.Refract(ref location, NOVAS.RefractionOption.StandardRefraction, zenithDistance);
            return currentAltitude + refraction;
        }

        [Obsolete("Use NINA.Astrometry.ObserverInfo object instead of latitude and longitude arguments")]
        public static double GetMoonAltitude(DateTime date, double latitude, double longitude) {
            var observerInfo = new ObserverInfo() {
                Latitude = latitude,
                Longitude = longitude,
            };

            return GetMoonAltitude(date, observerInfo);
        }

        [Obsolete("Use NINA.Astrometry.ObserverInfo object instead of latitude and longitude arguments")]
        public static double GetSunAltitude(DateTime date, double latitude, double longitude) {
            var observerInfo = new ObserverInfo() {
                Latitude = latitude,
                Longitude = longitude,
            };

            return GetSunAltitude(date, observerInfo);
        }

        /// <summary>
        /// Calculates arcseconds per pixel for given pixelsize and focallength
        /// </summary>
        /// <param name="pixelSize">Pixel size in microns</param>
        /// <param name="focalLength">Focallength in mm</param>
        /// <returns></returns>
        public static double ArcsecPerPixel(double pixelSize, double focalLength) {
            // arcseconds inside one radian and compensated by the difference of microns in pixels and mm in focal length
            //var factor = DegreeToArcsec(ToDegree(1)) / 1000d;
            return (pixelSize / focalLength) * ArcSecPerPixConversionFactor;
        }

        public static double MaxFieldOfView(double arcsecPerPixel, double width, double height) {
            return AstroUtil.ArcsecToArcmin(arcsecPerPixel * Math.Max(width, height));
        }

        public static double FieldOfView(double arcsecPerPixel, double width) {
            return AstroUtil.ArcsecToArcmin(arcsecPerPixel * width);
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
        /// Airmass calculated using Gueymard 1993
        /// </summary>
        /// <param name="altitude">Altitude in degrees</param>
        /// <returns>Airmass or NaN if an invalid altitude is supplied</returns>
        public static double Airmass(double altitude) {
            if (altitude < 0d || altitude > 90d || double.IsNaN(altitude) || double.IsInfinity(altitude)) {
                return double.NaN;
            } else {
                double Z = 90 - altitude;
                double cosZ = Math.Cos(ToRadians(Z));

                return 1.0 / (cosZ + 0.00176759 * Z * Math.Pow(94.37515 - Z, -1.21563));
            }
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
        /// Converts the air pressure for the given elevation from the given mean sea level air pressure using the ISO 2533:1975 Standard Atmospheric Model
        /// </summary>
        /// <param name="mslPressure"></param>
        /// <param name="elevation"></param>
        /// <returns></returns>
        public static double MslToLocalPressure(double mslPressure, double elevation) {
            return mslPressure * Math.Pow(1.0d - 2.25577e-5 * elevation, 5.25588d);
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

        public static Vector3D Polar3DToCartesian(double radius, double phi, double theta) {
            double x = radius * Math.Cos(phi);
            double radiusProjection = radius * Math.Sin(phi);
            double z = radiusProjection * Math.Cos(theta);
            double y = -radiusProjection * Math.Sin(theta);
            return new Vector3D(x, y, z);
        }

        /// <summary>
        /// Calculates position angle between two coordinates
        /// </summary>
        /// <param name="a1">Right Ascension in degrees</param>
        /// <param name="a2">Right Ascension in degrees</param>
        /// <param name="d1">Declination in degrees</param>
        /// <param name="d2">Declination in degrees</param>
        /// <returns>Position Angle in degrees</returns>
        public static double CalculatePositionAngle(double a1deg, double a2deg, double d1deg, double d2deg) {
            var a1 = ToRadians(a1deg);
            var a2 = ToRadians(a2deg);
            var d1 = ToRadians(d1deg);
            var d2 = ToRadians(d2deg);

            var numerator = Math.Sin(a1 - a2);
            var denominator = Math.Cos(d2) * Math.Tan(d1) - Math.Sin(d2) * Math.Cos(a1 - a2);
            var result = AstroUtil.ToDegree(Math.Atan(numerator / denominator));
            return result;
        }

        /// <summary>
        /// Calculates the refraction adjusted (observed) altitude for a given topocentric (in vacuum) altitude
        /// The Method works for Altitudes down to about 5°. For lower altitudes the method will produce unreliable results.
        /// </summary>
        /// <param name="altitude">Altitude to calculate the refracted altitude from</param>
        /// <param name="pressurehPa">Pressure in hecto pascals (hPa) at the observer (not at sea level)</param>
        /// <param name="tempCelcius">Ambient temperature in Celcius</param>
        /// <param name="relativeHumidity">Relative humidity at the ambient temperature</param>
        /// <param name="wavelength">Wavelength of light in micrometers. 0.54 is approximately the center of a typical luminance bandpass and would be a reasonable default value to use</param>
        /// <param name="iterationIncrementInArcsec">[Optional] Iteration Increment Size. Decreasing increment will need more iterations</param>
        /// <param name="maxIterations">[Optional] Maximum Iterations - When exceeded NaN is returned</param>
        /// <returns></returns>
        public static double CalculateRefractedAltitude(double altitude, double pressurehPa, double tempCelcius, double relativeHumidity, double wavelength, double iterationIncrementInArcsec = 1, double maxIterations = 1000) {
            if(altitude < 0) { throw new ArgumentException("Altitude must be greater than or equals 0"); }

            double refa = 0d;
            double refb = 0d;
            double Z = AstroUtil.ToRadians(90 - altitude);

            SOFA.RefractionConstants(pressurehPa, tempCelcius, relativeHumidity, wavelength, ref refa, ref refb);
            
            var increment = AstroUtil.ToRadians(AstroUtil.ArcsecToDegree(iterationIncrementInArcsec));
            var roller = increment;
            var iterations = 0;
            do {
                double refractedZenithDistanceRadian = Z - roller;
                // dZ = A tan Z + B tan^3 Z.  
                var dZ2 = refa * Math.Tan(refractedZenithDistanceRadian) + refb * Math.Pow(Math.Tan(refractedZenithDistanceRadian), 3);
                if(double.IsNaN(dZ2)) {
                    return double.NaN;
                }
                var originalZenithDistanceRadian = refractedZenithDistanceRadian + dZ2;
                if (Math.Abs(originalZenithDistanceRadian - Z) < AstroUtil.ToRadians(AstroUtil.ArcsecToDegree(iterationIncrementInArcsec))) {
                    return 90 - AstroUtil.ToDegree(refractedZenithDistanceRadian);
                }
                roller += increment;
                iterations++;

            } while (iterations < maxIterations);

            return double.NaN;
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
    public enum Direction {

        [Description("LblAltitude")]
        ALTITUDE,

        [Description("LblAzimuth")]
        AZIMUTH
    }
}