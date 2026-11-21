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
using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NINA.ViewModel {

    /// <summary>
    /// Availability of a target in a sky window that has an upper horizon, for the Sky Atlas above-horizon filter.
    /// A target can be above an upper horizon for only a few minutes (e.g. while passing the zenith), which can fall
    /// entirely between two altitude samples, so the selected window is searched for such excursions in finer steps
    /// instead of relying on the samples alone.
    /// </summary>
    internal sealed class SkyWindowAvailability {

        // The window is searched in one minute steps, at the samples and at the transit, and the start and end of each
        // excursion above the limit that is detected are located to about a second.
        private const double CHECK_STEP_HOURS = 1d / 60d;
        private const int CROSSING_ITERATIONS = 8;

        // Durations are compared as hours, so a run of exactly the required duration must not pass through rounding errors.
        private const double DURATION_TOLERANCE_HOURS = 1e-9;

        private readonly Coordinates coords;
        private readonly double latitude;
        private readonly SkyWindow skyWindow;
        private readonly double hourAngleAtReferenceDate;
        private readonly double sampleHours;

        public SkyWindowAvailability(
            Coordinates coords,
            double latitude,
            SkyWindow skyWindow,
            double hourAngleAtReferenceDate,
            double sampleHours) {
            this.coords = coords;
            this.latitude = latitude;
            this.skyWindow = skyWindow;
            this.hourAngleAtReferenceDate = hourAngleAtReferenceDate;
            this.sampleHours = sampleHours;
        }

        /// <summary>
        /// False without an upper horizon or if the target never gets higher than the lowest point of it, so it cannot exceed it.
        /// </summary>
        public static bool CanExceedUpperHorizon(Coordinates coords, double latitude, SkyWindow skyWindow) {
            return skyWindow != null
                && skyWindow.HasUpper
                && AstroUtil.GetAltitude(0, latitude, coords.Dec) > skyWindow.Upper.GetMinAltitude();
        }

        internal static (double Altitude, double LowerAltitude, double UpperAltitude) GetAltitudeAndLimits(
            Coordinates coords,
            double latitude,
            SkyWindow skyWindow,
            double hourAngle) {
            var hourAngleDegrees = AstroUtil.HoursToDegrees(hourAngle);
            var altitude = AstroUtil.GetAltitude(hourAngleDegrees, latitude, coords.Dec);
            var lowerAltitude = SkyWindow.DefaultLowerAltitude;
            var upperAltitude = SkyWindow.DefaultUpperAltitude;

            if (skyWindow != null && (skyWindow.HasLower || skyWindow.HasUpper)) {
                var azimuth = AstroUtil.GetAzimuth(hourAngleDegrees, altitude, latitude, coords.Dec);
                lowerAltitude = skyWindow.GetLower(azimuth);
                upperAltitude = skyWindow.GetUpper(azimuth);
            }

            return (altitude, lowerAltitude, upperAltitude);
        }

        /// <summary>
        /// Availability inside the exact selected window. The upper horizon is searched for excursions
        /// (see <see cref="FindExcursions"/>) over the whole window, also where it has no sample or extends past the last
        /// sample of the day: every detected excursion blocks the interval. The lower horizon is still evaluated on the
        /// sample grid and, as for every other target, a run is credited only up to the last sample that was available.
        /// In duration mode the longest stretch between blocked parts has to exceed the minimum duration.
        /// </summary>
        public bool IsAvailable(
            DateTime referenceDate,
            DateTime fromDate,
            DateTime throughDate,
            int firstSample,
            int lastSample,
            double minimumDuration,
            CancellationToken token) {
            var fullInterval = minimumDuration == double.MaxValue;
            var windowStart = (fromDate - referenceDate).TotalHours;
            var windowEnd = (throughDate - referenceDate).TotalHours;
            if (windowEnd < windowStart) {
                // An empty window only satisfies the "entire interval" sentinel case.
                return fullInterval;
            }

            if (!fullInterval && firstSample >= lastSample) {
                // A duration requires at least two samples to establish elapsed time.
                return false;
            }

            // Blocked parts in hours after the reference date.
            var blocked = new List<(double Start, double End)>();
            for (var sample = firstSample; sample <= lastSample; sample++) {
                token.ThrowIfCancellationRequested();

                var (altitude, lowerAltitude, _) = GetAltitudeAndLimits(coords, latitude, skyWindow, hourAngleAtReferenceDate + (sample * sampleHours));
                if (fullInterval ? altitude <= lowerAltitude : altitude < lowerAltitude) {
                    if (fullInterval) {
                        return false;
                    }

                    // The run before is credited up to the previous sample and the next run starts at this one.
                    var time = sample * sampleHours;
                    var previousSampleTime = sample > firstSample ? (sample - 1) * sampleHours : time;
                    blocked.Add((previousSampleTime, time));
                }
            }

            foreach (var excursion in FindExcursions(windowStart, windowEnd, token)) {
                if (fullInterval) {
                    return false;
                }

                blocked.Add(excursion);
            }

            if (fullInterval) {
                return true;
            }

            // Like for every other target a run is credited at most up to the last sample, also where an excursion starts after it.
            blocked.Sort();
            var runStart = firstSample * sampleHours;
            var lastSampleTime = lastSample * sampleHours;
            foreach (var (start, end) in blocked) {
                if (Math.Min(start, lastSampleTime) - runStart > minimumDuration + DURATION_TOLERANCE_HOURS) {
                    return true;
                }

                runStart = Math.Max(runStart, end);
            }

            return lastSampleTime - runStart > minimumDuration + DURATION_TOLERANCE_HOURS;
        }

        /// <summary>
        /// The detected stretches (hours after the reference date, clipped to the window) in which the target is above the upper horizon.
        /// The target is checked in one minute steps, at the samples and at the transit, where its altitude peaks, and the
        /// borders of every stretch are located to about a second. A stretch shorter than the step that does not contain
        /// one of these points can still be missed, which can only happen for an upper horizon that varies with azimuth.
        /// </summary>
        private IEnumerable<(double Start, double End)> FindExcursions(double windowStart, double windowEnd, CancellationToken token) {
            var times = new SortedSet<double> { windowStart, windowEnd };
            for (var time = windowStart + CHECK_STEP_HOURS; time < windowEnd; time += CHECK_STEP_HOURS) {
                times.Add(time);
            }

            for (var sample = (int)Math.Ceiling(windowStart / sampleHours); sample * sampleHours <= windowEnd; sample++) {
                times.Add(sample * sampleHours);
            }

            var transit = windowStart + AstroUtil.EuclidianModulus(-(hourAngleAtReferenceDate + windowStart), 24);
            if (transit <= windowEnd) {
                times.Add(transit);
            }

            var first = true;
            var previousTime = 0d;
            var previousAbove = false;
            var excursionStart = 0d;
            foreach (var time in times) {
                token.ThrowIfCancellationRequested();

                var above = IsAboveUpperHorizon(hourAngleAtReferenceDate + time);
                if (first) {
                    excursionStart = time;
                } else if (above && !previousAbove) {
                    excursionStart = FindCrossing(previousTime, time, true);
                } else if (!above && previousAbove) {
                    yield return (excursionStart, FindCrossing(previousTime, time, false));
                }

                first = false;
                previousTime = time;
                previousAbove = above;
            }

            if (previousAbove) {
                yield return (excursionStart, windowEnd);
            }
        }

        /// <summary>
        /// Bisects the time at which the target crosses the upper horizon between two times that are on different sides of it.
        /// </summary>
        private double FindCrossing(double earlier, double later, bool aboveAtLater) {
            for (var i = 0; i < CROSSING_ITERATIONS; i++) {
                var middle = (earlier + later) / 2;
                if (IsAboveUpperHorizon(hourAngleAtReferenceDate + middle) == aboveAtLater) {
                    later = middle;
                } else {
                    earlier = middle;
                }
            }

            return (earlier + later) / 2;
        }

        private bool IsAboveUpperHorizon(double hourAngle) {
            var (altitude, _, upperAltitude) = GetAltitudeAndLimits(coords, latitude, skyWindow, hourAngle);

            return altitude > upperAltitude;
        }
    }
}
