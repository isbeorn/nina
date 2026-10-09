#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;
using RiseSetMeridian = NINA.Sequencer.Utility.ItemUtility.RiseSetMeridian;

namespace NINA.Benchmark {

    // Copied from ItemUtility at 7359a0be18e234ab056c0029c424b6b5364f103a.
    // Changes: injected captured clock, counted actual coordinate transformations and suppressed logging.
    // Rise/meridian bounds, rounding, retained ExpectedDateTime and 1/5/10 minute sampling are unchanged.
    internal sealed class LegacyTargetPrediction {
        private DateTime now;

        internal int Evaluations { get; private set; }

        internal bool Found { get; private set; }

        internal int Predict(WaitLoopData data, DateTime time) {
            now = time.ToLocalTime();
            Evaluations = 0;
            data.CurrentAltitude = GetCurrentAltitude(data, now, data.Observer);
            CalculateExpectedTimeCommon(data, true, 90, (when, observer) => GetCurrentAltitude(data, when, observer));
            Found = !data.ExpectedTime.StartsWith("--", StringComparison.Ordinal);
            return Evaluations;
        }

        private double GetCurrentAltitude(WaitLoopData data, DateTime time, ObserverInfo observer) {
            Evaluations++;
            return data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude),
                Angle.ByDegree(observer.Longitude), observer.Elevation, time).Altitude.Degree;
        }

        private double GetTargetAltitudeWithHorizon(WaitLoopData data, DateTime time) {
            if (data.Coordinates == null) {
                return 0;
            }

            double altitude = 0;
            if (data.Horizon != null) {
                Evaluations++;
                var position = data.Coordinates.Coordinates.Transform(Angle.ByDegree(data.Latitude),
                    Angle.ByDegree(data.Longitude), data.Elevation, time);
                altitude = data.Horizon.GetAltitude(position.Azimuth.Degree);
            }

            return Math.Round(altitude + data.Offset, 2);
        }

        private static double Cos(double degrees) => Math.Cos(ToRadians(degrees));

        private static double Sin(double degrees) => Math.Sin(ToRadians(degrees));

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;

        private static double ToHours(double degrees) => degrees / 15.0;

        private static double Rev(double value) => ItemUtility.Rev(value);

        private const int LOOP_INTERVAL = 5;
        private const int NEAR_TIME = 10;
        private const int NEAR_TIME_HORIZON = 60;

        public void Iterate(WaitLoopData data, RiseSetMeridian rsm, bool greater, bool sense,
            int allowance, Func<DateTime, ObserverInfo, double> getCurrentAltitude) {
            // We'll iterate (not too much) to get a better time
            data.SetApproximate(true);

            bool ns = (greater && sense) || (!greater && !sense);

            DateTime now = this.now;
            DateTime startTime;
            DateTime endTime;
            int interval;
            int loops = allowance / LOOP_INTERVAL;
            TimeSpan span = data.ExpectedDateTime - now;
            // If we're within 5 minutes, get a precise time by iterating every minute starting now
            // Otherwise, set a range for iteration and check every five minutes
            DateTime baseTime = now;
            if (Math.Abs(span.TotalMinutes) < NEAR_TIME) {
                interval = 1;
                startTime = baseTime;
                endTime = startTime.AddMinutes(NEAR_TIME);
                data.SetApproximate(false);
            } else {
                interval = LOOP_INTERVAL;
                baseTime = ns ? rsm.Rise : rsm.Set;
                if (baseTime < now) {
                    baseTime = baseTime.AddDays(1);
                }
                if (data.UseCustomHorizon && data.Horizon != null) {
                    // With custom horizons, we have to do significant iteration so we'll use 10 minutes as
                    // our iteration time

                    if (rsm.Rise == DateTime.MinValue) {
                        // This is the hardest case; the target doesn't fall below our lowest horizon so we
                        // can't determine beforehand a good rise/set time for boundaries
                        startTime = now;
                        endTime = startTime.AddHours(24);
                        baseTime = startTime;
                    } else if (ns) {
                        startTime = rsm.Rise;
                        endTime = rsm.Meridian;
                    } else {
                        startTime = rsm.Meridian;
                        endTime = rsm.Set;
                    }
                    if (startTime < now) {
                        startTime = now;
                    }
                    if (startTime > endTime) {
                        endTime = endTime.AddDays(1);
                    }
                    // We'll be +/0- 10 minutes when > an hour away, otherwise 5 minutes
                    interval = Math.Abs(span.TotalMinutes) < NEAR_TIME_HORIZON ? 5 : 10;
                } else {
                    startTime = baseTime.AddMinutes(-loops / 2 * interval);
                    endTime = baseTime.AddMinutes(loops * interval);
                }
            }

            int iterations = 0;
            while (startTime < endTime) {
                if (startTime >= now) {
                    // Get the "current" altitude at the given time and see if the condition is met at that time
                    double altitude = getCurrentAltitude(startTime, data.Observer);
                    //Console.WriteLine(data.Name + "  #" + iterations + " " + (startTime - baseTime).TotalMinutes + " -> Time: " + startTime.ToString("t") + ", Current: " + Math.Round(altitude, 2) + " Target: " + data.TargetAltitude);

                    double targetAltitude = data.TargetAltitude;
                    if (data.UseCustomHorizon) {
                        targetAltitude = GetTargetAltitudeWithHorizon(data, startTime);
                    }

                    if ((ns && (altitude >= targetAltitude)) || (!ns && (altitude < targetAltitude))) {
                        data.TargetAltitude = targetAltitude;
                        data.ExpectedDateTime = startTime;
                        return;
                    } else {
                        iterations++;
                    }
                }
                startTime = startTime.AddMinutes(interval);
            }
            data.ExpectedDateTime = startTime;
            data.ExpectedTime = "--";
            // If we fail, we'll just take user's provided value.  Previous usage of NaN was meaningless.
            data.TargetAltitude = data.Offset;
        }

        [Obsolete]
        public void CalculateExpectedTimeCommon(WaitLoopData data, double offset, bool until,
            int allowance, Func<DateTime, ObserverInfo, double> getCurrentAltitude) {
            CalculateExpectedTimeCommon(data, until, allowance, getCurrentAltitude);
        }

        /*
         * info: common instruction info
         * offset: is the offset for rise/set for the sun and moon
         * until: true if the instruction is "<instruction> UNTIL <altitude>" as opposed to "<instruction> IF <altitude>"
         * allowance: the amount of slop we allow when confirming time estimates, in minutes.  For DSO's this is minimal, since
         *   their RA/Dec doesn't change (just a matter of getting to the minute.  For the Sun, a bit more is needed, and for
         *   the Moon, a fair bit more since coordinates change rapidly
         * func: a function that returns the sun/moon's altitude at a given time
         * 
         * We either know that the expected time is "Now" (i.e. the condition is already met) or we will iterate to find
         * the actual time
         */
        public void CalculateExpectedTimeCommon(WaitLoopData data, bool until, int allowance,
            Func<DateTime, ObserverInfo, double> getCurrentAltitude) {
            // Don't waste time on constructors
            if (data == null) {
                return;
            }

            if (data.Coordinates == null) {
                return;
            }

            Coordinates coord = data.Coordinates.Coordinates;
            if (coord.RADegrees == 0 && coord.Dec == 0) {
                return;
            }

            data.SetApproximate(false);

            double targetAltitude = data.Offset;
            if (data.UseCustomHorizon) {
                // For computing rise/set time, use minimum horizon altitude
                if (data.Horizon != null) {
                    targetAltitude = data.Horizon.GetMinAltitude();
                }
            }

            RiseSetMeridian rsm = CalculateTimeAtAltitude(coord, data.Latitude, data.Longitude, data.Elevation, targetAltitude);
            data.IsRising = rsm.IsRising;

            // Not thrilled with this exception, but don't want a more significant refactor at this point
            // For AltitudeCondition, an additional requirement is that a rising target shouldn't be considered
            // "below" the target altitude, regardless of its current altitude (i.e. we must wait until it's setting)
            bool mustSet = data.Name == "AltitudeCondition";

            if (data.UseCustomHorizon) {
                // For knowing if the condition is met NOW, target altitude must use current horizon
                targetAltitude = GetTargetAltitudeWithHorizon(data, now);
            }

            switch (data.Comparator) {
                case ComparisonOperatorEnum.GREATER_THAN:
                    if ((until && data.CurrentAltitude > targetAltitude) || (!until && data.CurrentAltitude <= targetAltitude)) {
                        data.TargetAltitude = targetAltitude;
                        data.ExpectedTime = Loc.Instance["LblNow"];
                    } else {
                        Iterate(data, rsm, greater: true, until, allowance, getCurrentAltitude);
                    }
                    return;
                default:
                    if ((until && data.CurrentAltitude <= targetAltitude && (!mustSet || (mustSet && !data.IsRising))) || (!until && data.CurrentAltitude > targetAltitude)) {
                        data.TargetAltitude = targetAltitude;
                        data.ExpectedTime = Loc.Instance["LblNow"];
                    } else {
                        Iterate(data, rsm, greater: false, until, allowance, getCurrentAltitude);
                    }
                    return;
            }
        }

        [Obsolete("Use CalculateTimeAtAltitude with elevation provided")]
        public RiseSetMeridian CalculateTimeAtAltitude(Coordinates coord, double latitude, double longitude, double targetAltitude) {
            return CalculateTimeAtAltitude(coord, latitude, longitude, 0, targetAltitude, now);
        }
        [Obsolete("Use CalculateTimeAtAltitude with elevation provided")]
        public RiseSetMeridian CalculateTimeAtAltitude(Coordinates coord, double latitude, double longitude, double targetAltitude, DateTime time) {
            return CalculateTimeAtAltitude(coord, latitude, longitude, 0, targetAltitude, time);
        }

        public RiseSetMeridian CalculateTimeAtAltitude(Coordinates coord, double latitude, double longitude,
            double elevation, double targetAltitude) {
            return CalculateTimeAtAltitude(coord, latitude, longitude, elevation, targetAltitude, now);
        }

        public RiseSetMeridian CalculateTimeAtAltitude(Coordinates coord, double latitude, double longitude,
            double elevation, double targetAltitude, DateTime time) {
            int tzoHours = new DateTimeOffset(time).Offset.Hours;
            double ra = coord.RADegrees;
            double dec = coord.Dec;
            Evaluations++;
            var altaz = coord.Transform(Angle.ByDegree(latitude), Angle.ByDegree(longitude), elevation, time);
            double currentAltitude = altaz.Altitude.Degree;
            bool isRising = altaz.AltitudeSite == Core.Enum.AltitudeSite.EAST;

            // Determine when the star is in the south (meridian)
            double gmst0 = Rev(180.0 + 356.0470 + 282.9404 + (0.9856002585 + 4.70935E-5) * ItemUtility.ReferenceDays(now.ToUniversalTime(), longitude));
            double meridian = AstroUtil.DegreesToHours(ra - gmst0 - longitude);
            double meridianLocal = meridian + tzoHours;
            if (meridianLocal < 0) {
                meridianLocal += 24;
            }

            //cos−1(sec(dec)sec(lat)(sin(el)−sin(dec)sin(lat)))
            double hourAngle = Math.Acos(1.0 / Cos(dec) * 1.0 / Cos(latitude) * (Sin(targetAltitude) - (Sin(dec) * Sin(latitude))));
            double hourAngleDegrees = ToDegrees(hourAngle);
            double hourAngleHours = ToHours(hourAngleDegrees);

            // If the target altitude can't be reached, just return
            if (double.IsNaN(hourAngleHours)) {
                return new RiseSetMeridian(DateTime.MinValue, DateTime.MinValue,
                    now.Date.AddHours(meridianLocal), currentAltitude, isRising);
            }

            double riseHours = meridian - hourAngleHours + tzoHours;
            if (riseHours < 0) {
                riseHours += 24;
            }
            double setHours = meridian + hourAngleHours + tzoHours;
            if (setHours < 0) {
                setHours += 24;
            }

            // Time the object is rising to this altitude
            DateTime risingTime = now.Date.AddHours(riseHours);
            // Time the object is setting to this altitude
            DateTime settingTime = now.Date.AddHours(setHours);
            if (settingTime < risingTime) {
                settingTime = settingTime.AddHours(24);
            }
            // Time the object transits the meridian
            DateTime meridianTime = now.Date.AddHours(meridianLocal);

            return new RiseSetMeridian(risingTime, settingTime, meridianTime, currentAltitude, isRising);
        }

    }
}
