#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.Body;
using System;
using System.Threading.Tasks;

namespace NINA.Astrometry.RiseAndSet {

    public abstract class RiseAndSetEvent {

        [Obsolete("Use method with elevation parameter instead")]
        public RiseAndSetEvent(DateTime date, double latitude, double longitude) : this(date, latitude, longitude, elevation: 0) { }
        public RiseAndSetEvent(DateTime date, double latitude, double longitude, double elevation) {
            this.Date = date;
            this.Latitude = latitude;
            this.Longitude = longitude;
            Elevation = elevation;
        }

        public DateTime Date { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Elevation { get; private set; }
        public virtual DateTime? Rise { get; private set; }
        public virtual DateTime? Set { get; private set; }

        protected abstract double AdjustAltitude(BasicBody body);

        protected abstract BasicBody GetBody(DateTime date);

        [Obsolete("Use Compute instead")]
        public virtual Task<bool> Calculate() {
            return Task.FromResult(Compute());
        }

        private SolarEventContext solarContext;

        // Only the built-in solar searches share a context. Public Compute and plugin hooks
        // keep their standalone behavior, including when a returned event is recomputed.
        internal bool Compute(SolarEventContext context) {
            solarContext = context;
            try {
                return Compute();
            } finally {
                solarContext = null;
            }
        }

        private const long EventTimeToleranceTicks = TimeSpan.TicksPerSecond / 10;
        private const long ExtremumTimeToleranceTicks = TimeSpan.TicksPerSecond / 100;
        private const int MaximumRefinementIterations = 32;

        /// <summary>
        /// Calculates the first rise and set in the historical 26-hour search interval.
        /// Actual crossings are refined to a time bracket no wider than 0.1 seconds.
        /// </summary>
        public virtual bool Compute() {
            Rise = null;
            Set = null;
            var startUtc = Date.ToUniversalTime();
            var previousAltitude = EvaluateAltitude(startUtc);

            // Preserve the historical search through Date + 26 hours, using elapsed UTC time.
            for (var offset = 0; offset <= 24 && (Rise == null || Set == null); offset += 2) {
                var start = startUtc.AddHours(offset);
                var middle = start.AddHours(1);
                var end = start.AddHours(2);
                var altitude0 = previousAltitude;
                var altitude1 = EvaluateAltitude(middle);
                var altitude2 = EvaluateAltitude(end);
                previousAltitude = altitude2;

                if (altitude0 == 0 && altitude1 == 0 && altitude2 == 0) {
                    continue;
                }

                // The quadratic is only an extremum locator. Its roots need not be real crossings.
                var a = 0.5 * (altitude2 + altitude0) - altitude1;
                var b = 2 * altitude1 - 0.5 * altitude2 - 1.5 * altitude0;
                var extremumHours = -b / (2 * a);
                // Refine only extrema that could conceal crossings between same-sign samples.
                // Crossings already bracketed by the samples can use those narrower intervals.
                var needsExtremum = altitude0 == 0 || altitude1 == 0 || altitude2 == 0 ||
                    (a > 0
                        ? altitude0 > 0 && altitude1 > 0 && altitude2 > 0
                        : altitude0 < 0 && altitude1 < 0 && altitude2 < 0);
                if (needsExtremum && double.IsFinite(extremumHours) && extremumHours > 0 && extremumHours < 2) {
                    var extremum = RefineExtremum(start, end, a > 0);
                    FindCrossing(start, altitude0, extremum.Time, extremum.Altitude);
                    FindCrossing(extremum.Time, extremum.Altitude, end, altitude2);
                } else {
                    FindCrossing(start, altitude0, middle, altitude1);
                    FindCrossing(middle, altitude1, end, altitude2);
                }
            }

            return Rise != null || Set != null;
        }

        private double EvaluateAltitude(DateTime time, bool findingExtremum = false) {
            var body = solarContext?.GetBody(time) ?? GetBody(time);
            if (solarContext == null) {
                body.Calculate();
            }
            var altitude = findingExtremum && solarContext != null ? body.Altitude : AdjustAltitude(body);
            if (!double.IsFinite(altitude)) {
                throw new InvalidOperationException($"Cannot calculate rise/set: nonfinite altitude at {time:O}.");
            }
            return altitude;
        }

        private (DateTime Time, double Altitude) RefineExtremum(DateTime left, DateTime right, bool minimum) {
            var key = (left, right, minimum);
            if (solarContext != null && solarContext.Extrema.TryGetValue(key, out var cachedTime)) {
                return (cachedTime, EvaluateAltitude(cachedTime));
            }
            // Constant solar thresholds share the same center-altitude extrema. Compare the
            // unadjusted altitude so rounding after threshold subtraction cannot change the search.
            // Golden-section search retains a bounded interval around the actual body extremum.
            const double fraction = 0.6180339887498949;
            var first = right.AddTicks(-(long)((right - left).Ticks * fraction));
            var second = left.AddTicks((long)((right - left).Ticks * fraction));
            var firstAltitude = EvaluateAltitude(first, findingExtremum: true);
            var secondAltitude = EvaluateAltitude(second, findingExtremum: true);
            for (var iteration = 0; iteration < MaximumRefinementIterations && (right - left).Ticks > ExtremumTimeToleranceTicks; iteration++) {
                if (minimum ? firstAltitude < secondAltitude : firstAltitude > secondAltitude) {
                    right = second;
                    second = first;
                    secondAltitude = firstAltitude;
                    first = right.AddTicks(-(long)((right - left).Ticks * fraction));
                    firstAltitude = EvaluateAltitude(first, findingExtremum: true);
                } else {
                    left = first;
                    first = second;
                    firstAltitude = secondAltitude;
                    second = left.AddTicks((long)((right - left).Ticks * fraction));
                    secondAltitude = EvaluateAltitude(second, findingExtremum: true);
                }
            }
            var time = left.AddTicks((right - left).Ticks / 2);
            if (solarContext != null) {
                solarContext.Extrema[key] = time;
            }
            return (time, EvaluateAltitude(time));
        }

        private void FindCrossing(DateTime left, double leftAltitude, DateTime right, double rightAltitude) {
            // A zero at a shared boundary counts once and only when the body changes sides.
            if (leftAltitude == 0) {
                AssignBoundaryEvent(left);
            }
            if (rightAltitude == 0) {
                AssignBoundaryEvent(right);
            }
            if (leftAltitude == 0 || rightAltitude == 0 || (leftAltitude > 0) == (rightAltitude > 0)) {
                return;
            }

            var rising = rightAltitude > leftAltitude;
            // Interpolation proposes a short bracket; actual altitude signs must verify it.
            // Limit the proposals so a stalled secant always falls back to bounded bisection.
            for (var trial = 0; trial < 4 && (right - left).Ticks > EventTimeToleranceTicks; trial++) {
                var fraction = leftAltitude / (leftAltitude - rightAltitude);
                if (!double.IsFinite(fraction) || fraction <= 0 || fraction >= 1) {
                    break;
                }
                var proposal = left.AddTicks((long)((right - left).Ticks * fraction));
                if (proposal <= left || proposal >= right) {
                    break;
                }
                var first = proposal.AddTicks(-Math.Min(EventTimeToleranceTicks / 2, (proposal - left).Ticks));
                var second = proposal.AddTicks(Math.Min(EventTimeToleranceTicks / 2, (right - proposal).Ticks));
                var firstAltitude = first == left ? leftAltitude : EvaluateAltitude(first);
                var secondAltitude = second == right ? rightAltitude : EvaluateAltitude(second);
                if (firstAltitude == 0 || secondAltitude == 0) {
                    break;
                }
                if ((firstAltitude > 0) != (secondAltitude > 0)) {
                    AssignEvent(first.AddTicks((second - first).Ticks / 2), rising);
                    return;
                }
                if ((firstAltitude > 0) == (leftAltitude > 0)) {
                    left = second;
                    leftAltitude = secondAltitude;
                } else {
                    right = first;
                    rightAltitude = firstAltitude;
                }
            }
            for (var iteration = 0; iteration < MaximumRefinementIterations && (right - left).Ticks > EventTimeToleranceTicks; iteration++) {
                var middle = left.AddTicks((right - left).Ticks / 2);
                var altitude = EvaluateAltitude(middle);
                if (altitude == 0) {
                    AssignEvent(middle, rising);
                    return;
                }
                if ((altitude > 0) == (leftAltitude > 0)) {
                    left = middle;
                    leftAltitude = altitude;
                } else {
                    right = middle;
                }
            }
            AssignEvent(left.AddTicks((right - left).Ticks / 2), rising);
        }

        private void AssignBoundaryEvent(DateTime time) {
            var before = EvaluateAltitude(time.AddTicks(-EventTimeToleranceTicks));
            var after = EvaluateAltitude(time.AddTicks(EventTimeToleranceTicks));
            if ((before < 0 && after > 0) || (before > 0 && after < 0)) {
                AssignEvent(time, after > before);
            }
        }

        private void AssignEvent(DateTime utcTime, bool rising) {
            var eventTime = Date.Kind == DateTimeKind.Utc ? utcTime : utcTime.ToLocalTime();
            if (Date.Kind == DateTimeKind.Unspecified) {
                eventTime = DateTime.SpecifyKind(eventTime, DateTimeKind.Unspecified);
            }

            // Windows and refined subintervals are visited chronologically, preserving the first
            // crossing and ignoring duplicates at their shared boundaries, including DST folds.
            if (rising) {
                if (Rise == null) {
                    Rise = eventTime;
                }
            } else if (Set == null) {
                Set = eventTime;
            }
        }
    }
}