#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Distributions.Univariate;
using System;

namespace NINA.Equipment.Equipment.MyGuider {

    internal readonly struct DitherOffset {
        internal DitherOffset(double westEastPixels, double northSouthPixels) {
            WestEastPixels = westEastPixels;
            NorthSouthPixels = northSouthPixels;
        }

        internal double WestEastPixels { get; }
        internal double NorthSouthPixels { get; }
    }

    internal sealed class DitherOffsetSelector {
        internal const int MaxSelectionAttempts = 100;

        private readonly Func<double, DitherOffset> candidateFactory;

        internal DitherOffsetSelector() {
            Random random = new Random();
            candidateFactory = ditherPixels => GenerateCandidate(ditherPixels, random);
        }

        internal DitherOffsetSelector(Func<double, DitherOffset> candidateFactory) {
            this.candidateFactory = candidateFactory ?? throw new ArgumentNullException(nameof(candidateFactory));
        }

        internal DitherOffset SelectOffset(DitherOffset previousOffset, double ditherPixels, double minimumDitherPixels, bool ditherRAOnly) {
            double effectiveMinimum = NormalizeMinimum(ditherPixels, minimumDitherPixels);
            for (int attempt = 0; attempt < MaxSelectionAttempts; attempt++) {
                DitherOffset candidate = candidateFactory(ditherPixels);
                if (ditherRAOnly) {
                    candidate = new DitherOffset(candidate.WestEastPixels, previousOffset.NorthSouthPixels);
                }

                if (MeetsMinimum(previousOffset, candidate, effectiveMinimum, ditherRAOnly)) {
                    return candidate;
                }
            }

            return GenerateFallback(previousOffset, effectiveMinimum, ditherRAOnly);
        }

        internal static double NormalizeMinimum(double ditherPixels, double minimumDitherPixels) {
            double maximum = double.IsFinite(ditherPixels) && ditherPixels > 0.0 ? ditherPixels : 0.0;
            if (double.IsNaN(minimumDitherPixels) || minimumDitherPixels <= 0.0) {
                return 0.0;
            }
            if (double.IsPositiveInfinity(minimumDitherPixels) || minimumDitherPixels > maximum) {
                return maximum;
            }
            return minimumDitherPixels;
        }

        private static DitherOffset GenerateCandidate(double ditherPixels, Random random) {
            double ditherAngle = random.NextDouble() * Math.PI;
            double targetDistancePixels = NormalDistribution.Random(mean: 0.0, stdDev: ditherPixels);
            targetDistancePixels = Math.Min(3.0d * ditherPixels, Math.Max(-3.0d * ditherPixels, targetDistancePixels));
            return new DitherOffset(
                targetDistancePixels * Math.Cos(ditherAngle),
                targetDistancePixels * Math.Sin(ditherAngle));
        }

        private static bool MeetsMinimum(DitherOffset previousOffset, DitherOffset candidate, double minimumDitherPixels, bool ditherRAOnly) {
            double westEastMovement = candidate.WestEastPixels - previousOffset.WestEastPixels;
            if (ditherRAOnly) {
                return Math.Abs(westEastMovement) >= minimumDitherPixels;
            }

            double northSouthMovement = candidate.NorthSouthPixels - previousOffset.NorthSouthPixels;
            return Math.Sqrt(westEastMovement * westEastMovement + northSouthMovement * northSouthMovement) >= minimumDitherPixels;
        }

        private static DitherOffset GenerateFallback(DitherOffset previousOffset, double minimumDitherPixels, bool ditherRAOnly) {
            if (minimumDitherPixels <= 0.0) {
                return previousOffset;
            }

            if (ditherRAOnly) {
                double direction = previousOffset.WestEastPixels > 0.0 ? -1.0 : 1.0;
                return new DitherOffset(
                    previousOffset.WestEastPixels + direction * minimumDitherPixels,
                    previousOffset.NorthSouthPixels);
            }

            double distanceFromOrigin = Math.Sqrt(
                previousOffset.WestEastPixels * previousOffset.WestEastPixels
                + previousOffset.NorthSouthPixels * previousOffset.NorthSouthPixels);
            if (distanceFromOrigin == 0.0) {
                return new DitherOffset(minimumDitherPixels, 0.0);
            }

            return new DitherOffset(
                previousOffset.WestEastPixels - minimumDitherPixels * previousOffset.WestEastPixels / distanceFromOrigin,
                previousOffset.NorthSouthPixels - minimumDitherPixels * previousOffset.NorthSouthPixels / distanceFromOrigin);
        }
    }
}
