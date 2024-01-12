#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Container;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Utility;

namespace NINA.Sequencer.Utility {

    public class ItemUtility {

        public static ContextCoordinates RetrieveContextCoordinates(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null && container.Target != null && container.Target.InputCoordinates != null && container.Target.DeepSkyObject != null) {
                    return new ContextCoordinates(
                        container.Target.InputCoordinates.Coordinates, 
                        container.Target.PositionAngle,
                        container.Target.DeepSkyObject.ShiftTrackingRate);
                } else {
                    return RetrieveContextCoordinates(parent.Parent);
                }
            } else {
                return null;
            }
        }

        public static bool IsInRootContainer(ISequenceContainer parent) {
            return GetRootContainer(parent) != null;
        }

        public static ISequenceRootContainer GetRootContainer(ISequenceContainer parent) {
            if (parent != null) {
                if (parent is ISequenceRootContainer rootContainer) {
                    return rootContainer;
                }
                return GetRootContainer(parent.Parent);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Checks if the current context or one of its parents contains a meridian flip trigger and returns the estimated time of the flip
        /// </summary>
        /// <param name="context">current context instruction set</param>
        /// <returns></returns>
        public static DateTime GetMeridianFlipTime(ISequenceContainer context) {
            if (context == null) { return DateTime.MinValue; }

            if (context is ITriggerable triggerable) {
                var snapshot = triggerable.GetTriggersSnapshot();
                if (snapshot?.Count > 0) {
                    var item = snapshot.FirstOrDefault(x => x is IMeridianFlipTrigger);
                    if (item != null) {
                        return ((IMeridianFlipTrigger)item).LatestFlipTime;
                    }
                }
            }
            if (context.Parent != null) {
                return GetMeridianFlipTime(context.Parent);
            } else {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Checks if the current context or one of its parents contains a meridian flip trigger and checks if the remaining time is enough to complete prior to the flip
        /// </summary>
        /// <param name="context">current context instruction set</param>
        /// <param name="estimatedDuration">estimated duration of the item to run</param>
        /// <returns>
        /// true: is too close and can't finish before flip
        /// false: can finish before flip or no meridian flip trigger is present
        /// </returns>
        public static bool IsTooCloseToMeridianFlip(ISequenceContainer context, TimeSpan estimatedDuration) {
            var estimatedItemFinishTime = DateTime.Now + TimeSpan.FromSeconds(estimatedDuration.TotalSeconds * 1.5);

            var flipTime = GetMeridianFlipTime(context);

            if (flipTime > DateTime.Now && estimatedItemFinishTime > flipTime) {
                return true;
            }
            return false;
        }

        public static List<IDeepSkyObjectContainer> LookForTargetsDownwards(ISequenceContainer container) {
            var objects = new List<IDeepSkyObjectContainer>();

            var children = (IList<ISequenceItem>)container.GetItemsSnapshot();
            if (children != null) {
                foreach (var child in children) {
                    if (child is IDeepSkyObjectContainer skyObjectContainer) {
                        objects.Add(skyObjectContainer);
                    } else if (child is ISequenceContainer childContainer) {
                        var check = LookForTargetsDownwards(childContainer);
                        if (check != null) {
                            objects.AddRange(check);
                        }
                    }
                }
            }
            return objects;
        }

        public class RiseSetMeridian {
            public DateTime Rise;
            public DateTime Set;
            public DateTime Meridian;
            public double CurrentAltitude;
            public bool IsRising;

            public RiseSetMeridian(DateTime rise, DateTime set, DateTime meridian, double currentAltitude, bool isRising) {
                Rise = rise;
                Set = set;
                Meridian = meridian;
                CurrentAltitude = currentAltitude;
                IsRising = isRising;
            }

            public override string ToString() {
                return $"Altitude: {Math.Round(CurrentAltitude, 2)}, Rise: {Rise.ToString("t")}, Set: {Set.ToString("t")}, Meridian: {Meridian.ToString("t")}";
            }
        }

        // Yes, I know these are elsewhere, but keeping things local for now.  Can clean up later
        private static double Cos(double degrees) => Math.Cos(ToRadians(degrees));
        private static double Sin(double degrees) => Math.Sin(ToRadians(degrees));
        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
        private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
        private static double ToHours(double degrees) => degrees / 15.0;

        public static double Rev(double x) => x - 360.0 * Math.Floor(x * 1.0 / 360.0);

        // Days since January 1, 2000 00:00:00 UTC
        // This is the standard for calculations of sun/moon position/times
        // It is accurate to the year 2100
        public static double ReferenceDays(DateTime n, double longitude) {
            int d = (367 * n.Year) - (7 * (n.Year + ((n.Month + 9) / 12)) / 4) + (275 * n.Month / 9) + n.Day - 730530;
            double days = d + n.TimeOfDay.TotalHours / 24.0;
            return days + longitude / 360.0;
        }

        public static double ReferenceDays(double longitude) {
            return ReferenceDays(DateTime.UtcNow, longitude);
         }

        public static RiseSetMeridian CalculateTimeAtAltitude(Coordinates coord, double latitude, double longitude, double targetAltitude) {
            return CalculateTimeAtAltitude(coord, latitude, longitude, targetAltitude, DateTime.Now);
        }

        private const int LOOP_INTERVAL = 5;  // How many minutes for each loop
        private const int NEAR_TIME = 10;  // How close before we get an exact time
        private const int NEAR_TIME_HORIZON = 60;

        public static void Iterate(WaitLoopData data, RiseSetMeridian rsm, bool greater, bool sense, int allowance, Func<DateTime, ObserverInfo, double> getCurrentAltitude) {
            // We'll iterate (not too much) to get a better time
            data.SetApproximate(true);

            bool ns = (greater && sense) || (!greater && !sense);

            DateTime now = DateTime.Now;
            DateTime startTime, endTime;
            int interval;
            int loops = allowance / LOOP_INTERVAL;
            TimeSpan span = data.ExpectedDateTime - DateTime.Now;
            // If we're within 5 minutes, get a precise time by iterating every minute starting now
            // Otherwise, set a range for iteration and check every five minutes
            DateTime baseTime = DateTime.Now;
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
                        startTime = DateTime.Now;
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
                        targetAltitude = data.GetTargetAltitudeWithHorizon(startTime);
                    }

                    if ((ns && (altitude >= targetAltitude)) || (!ns && (altitude < targetAltitude))) {
                        Console.WriteLine(data.Name + ": " + ++iterations + " iterations: " + (startTime - baseTime).TotalMinutes + " minutes from predicted");
                        data.TargetAltitude = targetAltitude;
                        data.ExpectedDateTime = startTime;
                        return;
                    } else {
                        iterations++;
                    }
                }
                startTime = startTime.AddMinutes(interval);
            }
            
            Logger.Debug(data.Name + ": CalculateExpectedTime failed after " + ++iterations + " iterations, Custom: " + data.UseCustomHorizon);
            data.ExpectedDateTime = startTime;
            data.ExpectedTime = "--";
            // If we fail, we'll just take user's provided value.  Previous usage of NaN was meaningless.
            data.TargetAltitude = data.Offset;
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
        public static void CalculateExpectedTimeCommon(WaitLoopData data, double offset, bool until, int allowance, Func<DateTime, ObserverInfo, double> getCurrentAltitude) {
            // Don't waste time on constructors
            if (data == null) { return; }            
            if (data.Coordinates == null) { return; }

            Coordinates coord = data.Coordinates.Coordinates;
            if (coord.RADegrees == 0 && coord.Dec == 0) { return; }

            data.SetApproximate(false);

            double targetAltitude = data.Offset;
            if (data.UseCustomHorizon) {
                // For computing rise/set time, use minimum horizon altitude
                if (data.Horizon != null) {
                    targetAltitude = data.Horizon.GetMinAltitude();
                }
            }

            RiseSetMeridian rsm = CalculateTimeAtAltitude(coord, data.Latitude, data.Longitude, targetAltitude + offset);
            data.IsRising = rsm.IsRising;

            // Not thrilled with this exception, but don't want a more significant refactor at this point
            // For AltitudeCondition, an additional requirement is that a rising target shouldn't be considered
            // "below" the target altitude, regardless of its current altitude (i.e. we must wait until it's setting)
            bool mustSet = data.Name == "AltitudeCondition";

            if (data.UseCustomHorizon) {
                // For knowing if the condition is met NOW, target altitude must use current horizon
                targetAltitude = data.GetTargetAltitudeWithHorizon(DateTime.Now) + offset;
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

        public static RiseSetMeridian CalculateTimeAtAltitude(Coordinates coord, double latitude, double longitude, double targetAltitude, DateTime time) {
            int tzoHours = new DateTimeOffset(time).Offset.Hours;
            double ra = coord.RADegrees;
            double dec = coord.Dec;
            var altaz = coord.Transform(Angle.ByDegree(latitude), Angle.ByDegree(longitude), time);
            double currentAltitude = altaz.Altitude.Degree;
            bool isRising = altaz.AltitudeSite == Core.Enum.AltitudeSite.EAST;

            // Determine when the star is in the south (meridian)
            double gmst0 = Rev(180.0 + 356.0470 + 282.9404 + (0.9856002585 + 4.70935E-5) * ReferenceDays(longitude));
            double meridian = AstroUtil.DegreesToHours(ra - gmst0 - longitude);
            double meridianLocal = meridian + tzoHours;
            if (meridianLocal < 0) meridianLocal += 24;

            //cos−1(sec(dec)sec(lat)(sin(el)−sin(dec)sin(lat)))
            double hourAngle = Math.Acos(1.0 / Cos(dec) * 1.0 / Cos(latitude) * (Sin(targetAltitude) - (Sin(dec) * Sin(latitude))));
            double hourAngleDegrees = ToDegrees(hourAngle);
            double hourAngleHours = ToHours(hourAngleDegrees);

            // If the target altitude can't be reached, just return
            if (double.IsNaN(hourAngleHours)) return new RiseSetMeridian(DateTime.MinValue, DateTime.MinValue, DateTime.Today.AddHours(meridianLocal), currentAltitude, isRising);

            double riseHours = meridian - hourAngleHours + tzoHours;
            if (riseHours < 0) riseHours += 24;
            double setHours = meridian + hourAngleHours + tzoHours;
            if (setHours < 0) setHours += 24;

            // Time the object is rising to this altitude
            DateTime risingTime = DateTime.Today.AddHours(riseHours);
            // Time the object is setting to this altitude
            DateTime settingTime = DateTime.Today.AddHours(setHours);
            if (settingTime < risingTime) settingTime = settingTime.AddHours(24);
            // Time the object transits the meridian
            DateTime meridianTime = DateTime.Today.AddHours(meridianLocal);

            return new RiseSetMeridian(risingTime, settingTime, meridianTime, currentAltitude, isRising);
        }

        public static Coordinates CalculateSunRADec(ObserverInfo observer) {
            double jd = AstroUtil.GetJulianDate(DateTime.Now);
            NOVAS.SkyPosition skyPos = AstroUtil.GetSunPosition(DateTime.Now, jd, observer);
            return new Coordinates(skyPos.RA, skyPos.Dec, Epoch.JNOW, Coordinates.RAType.Hours);
        }

        public static Coordinates CalculateMoonRADec(ObserverInfo observer) {
            double jd = AstroUtil.GetJulianDate(DateTime.Now);
            NOVAS.SkyPosition skyPos = AstroUtil.GetMoonPosition(DateTime.Now, jd, observer);
            return new Coordinates(skyPos.RA, skyPos.Dec, Epoch.JNOW, Coordinates.RAType.Hours);
        }
    }
}