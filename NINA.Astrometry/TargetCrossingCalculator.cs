#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using NINA.Core.Model;

namespace NINA.Astrometry {
    internal enum TargetCrossingComparison {
        AboveInclusive,
        AboveStrict,
        BelowInclusive,
        BelowStrict,
        SettingBelow
    }

    internal enum TargetCrossingStatus {
        AlreadySatisfied,
        Found,
        NoEvent,
        Exhausted
    }

    internal readonly record struct TargetCrossingResult(TargetCrossingStatus Status, DateTime Time, int Evaluations);

    internal readonly record struct TargetPosition(double Altitude, double Azimuth, double HourAngle, double Declination) {
        internal bool IsRising => Azimuth >= 0 && Azimuth < 180;

        internal bool IsFinite => double.IsFinite(Altitude)
            && double.IsFinite(Azimuth)
            && double.IsFinite(HourAngle)
            && double.IsFinite(Declination);
    }

    /// <summary>
    /// Immutable inputs for a fixed catalog target. JNOW retains its original epoch reference date.
    /// </summary>
    internal sealed class TargetCrossingRequest {
        private readonly double rightAscension;
        private readonly double declination;

        internal double Latitude { get; }

        internal double Longitude { get; }

        internal double Elevation { get; }

        internal CustomHorizon Horizon { get; }

        internal double Offset { get; }

        internal TargetCrossingComparison Comparison { get; }

        internal TargetCrossingRequest(
            Coordinates coordinates,
            double latitude,
            double longitude,
            double elevation,
            CustomHorizon horizon,
            double offset,
            TargetCrossingComparison comparison) {
            var normalized = double.IsFinite(coordinates.RADegrees) && double.IsFinite(coordinates.Dec) && Math.Abs(coordinates.Dec) <= 90
                ? coordinates.Transform(Epoch.J2000) : coordinates;
            rightAscension = normalized.RARadians;
            declination = normalized.DecRadians;

            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
            Horizon = horizon;
            Offset = offset;
            Comparison = comparison;
        }

        internal bool SameInputs(TargetCrossingRequest other) {
            return other != null
                && rightAscension == other.rightAscension
                && declination == other.declination
                && Latitude == other.Latitude
                && Longitude == other.Longitude
                && Elevation == other.Elevation
                && ReferenceEquals(Horizon, other.Horizon)
                && Offset == other.Offset
                && Comparison == other.Comparison;
        }

        internal double TargetAltitude(TargetPosition position) => Offset + (Horizon?.GetAltitude(position.Azimuth) ?? 0);

        internal bool Qualifies(TargetPosition position) {
            var difference = position.Altitude - TargetAltitude(position);

            return Comparison switch {
                TargetCrossingComparison.AboveInclusive => difference >= 0,
                TargetCrossingComparison.AboveStrict => difference > 0,
                TargetCrossingComparison.BelowInclusive => difference <= 0,
                TargetCrossingComparison.BelowStrict => difference < 0,
                _ => !position.IsRising && difference < 0
            };
        }

        // The same zero-pressure SOFA transformation as Coordinates.Transform, without renormalizing the epoch
        // or constructing Angle/Coordinates/TopocentricCoordinates objects at each trial timestamp.
        internal TargetPosition Position(DateTime utc) {
            var (utc1, utc2) = AstroUtil.GetJulianDateUTCParts(utc);
            double az = 0;
            double zenith = 0;
            double hourAngle = 0;
            double dec = 0;
            double ra = 0;
            double eo = 0;

            var status = SOFA.CelestialToTopocentric(rightAscension, declination,
                0, 0, 0, 0, utc1, utc2, AstroUtil.DeltaUT(utc), AstroUtil.ToRadians(Longitude),
                AstroUtil.ToRadians(Latitude), Elevation, 0, 0, 0, 0, 0, 0,
                ref az, ref zenith, ref hourAngle, ref dec, ref ra, ref eo);

            if (status < 0) {
                return new TargetPosition(double.NaN, double.NaN, double.NaN, double.NaN);
            }

            return new TargetPosition(
                AstroUtil.ToDegree(AstroUtil.ToRadians(90) - zenith),
                AstroUtil.ToDegree(az),
                AstroUtil.ToDegree(hourAngle),
                AstroUtil.ToDegree(dec));
        }

        internal bool Valid => double.IsFinite(rightAscension)
            && double.IsFinite(declination)
            && Math.Abs(declination) <= Math.PI / 2
            && double.IsFinite(Latitude)
            && Math.Abs(Latitude) <= 90
            && double.IsFinite(Longitude)
            && double.IsFinite(Elevation)
            && double.IsFinite(Offset)
            && (Horizon == null || Horizon.HasFiniteSearchVertices);
    }

    internal sealed class TargetCrossingCalculator {
        internal const int MaximumEvaluations = 4096;

        internal const double ResolutionSeconds = 0.25;

        // IAU 2000 ERA advances 15.041067 degrees/hour. 16 leaves margin for apparent motion of
        // a fixed catalog star over one day. This is a spherical bound, never an azimuth speed bound.
        private const double MotionDegreesPerHour = 16;

        private const double SearchStepSeconds = 5;

        private const double SafePrefixResolutionSeconds = 0.1;

        private const double SiderealDegreesPerSecond = 360 * 1.00273781191135448 / 86400;

        private readonly TargetCrossingRequest request;
        private readonly int budget;
        private int evaluations;

        internal TargetCrossingCalculator(TargetCrossingRequest request, int budget = MaximumEvaluations) {
            this.request = request;
            this.budget = Math.Min(MaximumEvaluations, Math.Max(0, budget));
        }

        internal TargetCrossingResult Find(DateTime start, TargetPosition? current = null) {
            var utc = start.ToUniversalTime();
            if (!request.Valid) {
                return Result(TargetCrossingStatus.Exhausted);
            }

            var first = current ?? Sample(utc);
            if (first == null || !first.Value.IsFinite) {
                return Result(TargetCrossingStatus.Exhausted);
            }

            if (request.Qualifies(first.Value)) {
                return Result(TargetCrossingStatus.AlreadySatisfied, utc);
            }

            var end = utc.AddHours(24);
            if (request.Horizon == null || request.Horizon.FlatSearchAltitude.HasValue) {
                var seeded = FindFlat(utc, end, first.Value);
                if (seeded.HasValue) {
                    return seeded.Value;
                }
            }
            // DeltaUT uses daily EOP values. A motion cap must not straddle the UTC day boundary.
            while (utc < end) {
                var midnight = utc.Date.AddDays(1);
                var boundary = midnight < end ? midnight.AddTicks(-1) : end;
                var safeSeconds = SafePrefixSeconds(first.Value, (boundary - utc).TotalSeconds);
                var next = utc.AddSeconds(safeSeconds + SearchStepSeconds);
                if (next >= boundary) {
                    next = boundary;
                }

                first = Sample(next);
                if (first == null) {
                    return Result(TargetCrossingStatus.Exhausted);
                }

                if (request.Qualifies(first.Value)) {
                    return Result(TargetCrossingStatus.Found, next);
                }

                utc = next;
                if (utc == boundary && boundary != end) {
                    utc = midnight;
                    first = Sample(utc);
                    if (first == null) {
                        return Result(TargetCrossingStatus.Exhausted);
                    }

                    if (request.Qualifies(first.Value)) {
                        return Result(TargetCrossingStatus.Found, utc);
                    }
                }
            }

            return Result(TargetCrossingStatus.NoEvent);
        }

        private TargetPosition? Sample(DateTime utc) {
            if (evaluations >= budget) {
                return null;
            }

            evaluations++;
            var position = request.Position(utc);

            return position.IsFinite ? position : null;
        }

        private TargetCrossingResult Result(TargetCrossingStatus status, DateTime time = default) => new(status, time, evaluations);

        // The USNO spherical altitude formula supplies a seed only. The returned crossing always
        // uses the actual SOFA predicate on the qualifying side of a subsecond bracket.
        private TargetCrossingResult? FindFlat(DateTime start, DateTime end, TargetPosition first) {
            double radians = Math.PI / 180;
            var denominator = Math.Cos(first.Declination * radians) * Math.Cos(request.Latitude * radians);
            if (Math.Abs(denominator) < 1e-9) {
                return null;
            }

            var target = request.Offset + (request.Horizon?.FlatSearchAltitude ?? 0);
            var cosine = (Math.Sin(target * radians) - Math.Sin(first.Declination * radians) * Math.Sin(request.Latitude * radians)) / denominator;
            // Near a tangent, an analytic bracket is unreliable. Retain the bounded chronological search.
            if (Math.Abs(cosine) >= 0.9999) {
                return null;
            }

            var angle = Math.Acos(cosine) / radians;
            bool above = request.Comparison is TargetCrossingComparison.AboveInclusive or TargetCrossingComparison.AboveStrict;
            var desired = above ? -angle : angle;
            var signedDelta = desired - first.HourAngle;
            // Inverting an altitude exactly at the threshold can put the seed a few ulps behind
            // the current hour angle. Try the current crossing before wrapping to tomorrow.
            // The normal endpoint checks below still have to verify the actual SOFA predicate.
            var delta = Math.Abs(signedDelta) < 1e-8 ? 0 : AstroUtil.EuclidianModulus(signedDelta, 360);
            var seed = start.AddSeconds(delta / SiderealDegreesPerSecond);
            // A setting-only cutoff can occur at a transit while already below the requested altitude.
            if (request.Comparison == TargetCrossingComparison.SettingBelow && first.IsRising
                && target > 90 - Math.Abs(request.Latitude - first.Declination)) {
                return null;
            }

            if (seed >= end) {
                return null;
            }

            // Typical seeds are already close to the full-model root. Verify a small bracket first
            // and widen it if needed rather than paying for a two-minute bisection every update.
            for (int attempt = 0; attempt < 3; attempt++) {
                var halfWidthSeconds = attempt switch {
                    0 => 0.25,
                    1 => 1,
                    _ => 120
                };
                var left = seed.AddSeconds(-halfWidthSeconds);
                if (left < start) {
                    left = start;
                }

                var right = seed.AddSeconds(halfWidthSeconds);
                if (right > end) {
                    right = end;
                }

                // Do not use a continuous bracket across a change of daily EOP values.
                if (left.Date != right.Date) {
                    return null;
                }

                var low = left == start ? first : Sample(left);
                var high = Sample(right);
                if (low == null || high == null) {
                    return Result(TargetCrossingStatus.Exhausted);
                }

                if (!request.Qualifies(low.Value) && request.Qualifies(high.Value)) {
                    return Refine(left, right);
                }
            }

            return null;
        }

        private TargetCrossingResult Refine(DateTime left, DateTime right) {
            while ((right - left).TotalSeconds > ResolutionSeconds) {
                var middle = left.AddTicks((right.Ticks - left.Ticks) / 2);
                var sample = Sample(middle);
                if (sample == null) {
                    return Result(TargetCrossingStatus.Exhausted);
                }

                if (request.Qualifies(sample.Value)) {
                    right = middle;
                } else {
                    left = middle;
                }
            }

            return Result(TargetCrossingStatus.Found, right);
        }

        // The lower bound certifies an empty prefix using only spherical and horizon bounds.
        // Advancing at most five seconds beyond it cannot skip a ten-second qualifying window.
        private double SafePrefixSeconds(TargetPosition position, double remainingSeconds) {
            var lower = 0.0;
            var upper = remainingSeconds;
            if (Impossible(position, upper * MotionDegreesPerHour / 3600 + 1e-8)) {
                return upper;
            }

            while (upper - lower > SafePrefixResolutionSeconds) {
                var middle = (lower + upper) / 2;
                var radius = middle * MotionDegreesPerHour / 3600 + 1e-8;
                if (Impossible(position, radius)) {
                    lower = middle;
                } else {
                    upper = middle;
                }
            }

            return lower;
        }

        private bool Impossible(TargetPosition position, double radius) {
            double minHorizon = 0;
            double maxHorizon = 0;
            var reachesPole = radius >= 90 - Math.Abs(position.Altitude);
            var azimuthRadius = reachesPole ? 180 : Math.Asin(Math.Clamp(Math.Sin(radius * Math.PI / 180)
                / Math.Cos(position.Altitude * Math.PI / 180), -1, 1)) * 180 / Math.PI;
            if (request.Horizon != null) {
                request.Horizon.GetSearchAltitudeBounds(position.Azimuth, azimuthRadius, out minHorizon, out maxHorizon);
            }

            var minimum = Math.Max(-90, position.Altitude - radius) - maxHorizon - request.Offset;
            var maximum = Math.Min(90, position.Altitude + radius) - minHorizon - request.Offset;
            return request.Comparison switch {
                TargetCrossingComparison.AboveInclusive => maximum < 0,
                TargetCrossingComparison.AboveStrict => maximum <= 0,
                TargetCrossingComparison.BelowInclusive => minimum > 0,
                TargetCrossingComparison.BelowStrict => minimum >= 0,
                _ => minimum >= 0 || (!reachesPole && position.Azimuth - azimuthRadius >= 0 && position.Azimuth + azimuthRadius < 180)
            };
        }
    }
}
