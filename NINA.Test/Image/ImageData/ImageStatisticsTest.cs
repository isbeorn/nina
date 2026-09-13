#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;

namespace NINA.Test.Image.ImageData {

    [TestFixture]
    public class ImageStatisticsTest {

        /* All test images are at least 64x64 pixels. An odd pixel count keeps the median on an
         * actual (integer) sample value, which is the case the histogram based median/MAD walk
         * is defined for, so the odd sized cases use a 65x65 frame. */
        private const int EvenWidth = 64;

        private const int EvenHeight = 64;
        private const int EvenPixelCount = EvenWidth * EvenHeight;

        private const int OddWidth = 65;
        private const int OddHeight = 65;
        private const int OddPixelCount = OddWidth * OddHeight;

        [OneTimeSetUp]
        public void WarmUp() {
            /* run once so that JIT and first-use allocation costs are not attributed to the
             * first test that happens to execute */
            ImageStatistics.Create(Properties(EvenWidth, EvenHeight), new ushort[EvenPixelCount]);

            TestContext.Out.WriteLine($"Stopwatch.IsHighResolution: {Stopwatch.IsHighResolution}, Stopwatch.Frequency: {Stopwatch.Frequency} Hz ({1e9d / Stopwatch.Frequency:F1} ns/tick)");
        }

        private static ImageProperties Properties(int width, int height, int bitDepth = 16) {
            return new ImageProperties(width: width, height: height, bitDepth: bitDepth, isBayered: false, gain: 0, offset: 0);
        }

        /// <summary>
        /// Invokes <see cref="ImageStatistics.Create(ImageProperties, ushort[])"/> and records the
        /// wall clock duration of that single call using the platform high resolution timer.
        /// </summary>
        private static IImageStatistics CreateMeasured(ImageProperties properties, ushort[] data) {
            var stopwatch = Stopwatch.StartNew();
            var statistics = ImageStatistics.Create(properties, data);
            var elapsedTicks = stopwatch.ElapsedTicks;

            var elapsedMicroseconds = elapsedTicks * 1_000_000d / Stopwatch.Frequency;
            TestContext.Out.WriteLine(
                $"{TestContext.CurrentContext.Test.Name}: ImageStatistics.Create over {properties.Width}x{properties.Height} " +
                $"({data.Length} pixels) took {elapsedMicroseconds / 1000d:F4} ms ({elapsedMicroseconds:F1} us, {elapsedTicks} ticks)");

            return statistics;
        }

        [Test]
        public void Create_SequentialValues_ComputesAllStatistics() {
            /* 65x65 = 4225 pixels holding the values 1..4225, each exactly once */
            var data = new ushort[OddPixelCount];
            for (var i = 0; i < data.Length; i++) {
                data[i] = (ushort)(i + 1);
            }

            var statistics = CreateMeasured(Properties(OddWidth, OddHeight), data);

            statistics.BitDepth.Should().Be(16);
            statistics.Min.Should().Be(1);
            statistics.MinOccurrences.Should().Be(1);
            statistics.Max.Should().Be(OddPixelCount);
            statistics.MaxOccurrences.Should().Be(1);
            /* mean and median of 1..n are both (n + 1) / 2 */
            statistics.Mean.Should().BeApproximately((OddPixelCount + 1) / 2d, 1e-9);
            statistics.Median.Should().BeApproximately((OddPixelCount + 1) / 2d, 1e-9);
            /* deviations from the median are 0, 1, 1, 2, 2, ... so the MAD lands on (n - 1) / 4 */
            statistics.MedianAbsoluteDeviation.Should().BeApproximately((OddPixelCount - 1) / 4d, 1e-9);
            /* population standard deviation of 1..n is sqrt((n^2 - 1) / 12) */
            statistics.StDev.Should().BeApproximately(Math.Sqrt((((double)OddPixelCount * OddPixelCount) - 1d) / 12d), 1e-9);
        }

        [Test]
        public void Create_RepeatedExtremes_CountsMinAndMaxOccurrences() {
            /* 65x65 = 4225 pixels: 1500x value 5, 725x value 7, 2000x value 9 */
            var data = BuildHistogramImage((5, 1500), (7, 725), (9, 2000));
            data.Length.Should().Be(OddPixelCount);

            var statistics = CreateMeasured(Properties(OddWidth, OddHeight), data);

            statistics.Min.Should().Be(5);
            statistics.MinOccurrences.Should().Be(1500);
            statistics.Max.Should().Be(9);
            statistics.MaxOccurrences.Should().Be(2000);
            /* the middle sample falls inside the run of sevens */
            statistics.Median.Should().BeApproximately(7d, 1e-9);
            statistics.Mean.Should().BeApproximately(ReferenceMean(data), 1e-9);
            statistics.MedianAbsoluteDeviation.Should().BeApproximately(2d, 1e-9);
            statistics.StDev.Should().BeApproximately(ReferenceStandardDeviation(data), 1e-6);
        }

        [Test]
        public void Create_ExtremesDiscoveredLate_ResetsOccurrenceCounters() {
            /* the running min/max are only established part way through the image, so the
             * occurrence counters have to be reset when a new extreme shows up */
            var data = new ushort[EvenPixelCount];
            Array.Fill(data, (ushort)500);
            data[100] = 100;
            data[101] = 100;
            data[102] = 100;
            data[EvenPixelCount - 2] = 900;
            data[EvenPixelCount - 1] = 900;

            var statistics = CreateMeasured(Properties(EvenWidth, EvenHeight), data);

            statistics.Min.Should().Be(100);
            statistics.MinOccurrences.Should().Be(3);
            statistics.Max.Should().Be(900);
            statistics.MaxOccurrences.Should().Be(2);
        }

        [Test]
        public void Create_EvenNumberOfValues_InterpolatesMedianBetweenCenterValues() {
            /* 64x64 = 4096 pixels holding the values 1..4096, each exactly once */
            var data = new ushort[EvenPixelCount];
            for (var i = 0; i < data.Length; i++) {
                data[i] = (ushort)(i + 1);
            }

            var statistics = CreateMeasured(Properties(EvenWidth, EvenHeight), data);

            /* even sample count, so the median is interpolated between the two center values */
            statistics.Median.Should().BeApproximately((EvenPixelCount + 1) / 2d, 1e-9);
            statistics.Mean.Should().BeApproximately((EvenPixelCount + 1) / 2d, 1e-9);
            statistics.Min.Should().Be(1);
            statistics.MinOccurrences.Should().Be(1);
            statistics.Max.Should().Be(EvenPixelCount);
            statistics.MaxOccurrences.Should().Be(1);
            statistics.StDev.Should().BeApproximately(ReferenceStandardDeviation(data), 1e-6);
        }

        [Test]
        public void Create_ConstantImage_HasZeroDispersion() {
            var data = new ushort[EvenPixelCount];
            Array.Fill(data, (ushort)1234);

            var statistics = CreateMeasured(Properties(EvenWidth, EvenHeight), data);

            statistics.Min.Should().Be(1234);
            statistics.MinOccurrences.Should().Be(EvenPixelCount);
            statistics.Max.Should().Be(1234);
            statistics.MaxOccurrences.Should().Be(EvenPixelCount);
            statistics.Mean.Should().BeApproximately(1234d, 1e-9);
            statistics.Median.Should().BeApproximately(1234d, 1e-9);
            statistics.StDev.Should().BeApproximately(0d, 1e-9);
            statistics.MedianAbsoluteDeviation.Should().BeApproximately(0d, 1e-9);
        }

        [Test]
        public void Create_ZeroImage_HasZeroStatistics() {
            var data = new ushort[EvenPixelCount];

            var statistics = CreateMeasured(Properties(EvenWidth, EvenHeight), data);

            statistics.Min.Should().Be(0);
            statistics.MinOccurrences.Should().Be(EvenPixelCount);
            statistics.Max.Should().Be(0);
            statistics.MaxOccurrences.Should().Be(EvenPixelCount);
            statistics.Mean.Should().BeApproximately(0d, 1e-9);
            statistics.Median.Should().BeApproximately(0d, 1e-9);
            statistics.StDev.Should().BeApproximately(0d, 1e-9);
            statistics.MedianAbsoluteDeviation.Should().BeApproximately(0d, 1e-9);
        }

        [TestCase(42)]
        [TestCase(1337)]
        [TestCase(20260424)]
        public void Create_RandomImage_MatchesReferenceImplementations(int seed) {
            /* 101x101 = 10201 pixels of uniform noise */
            const int width = 101;
            const int height = 101;
            var random = new Random(seed);
            var data = new ushort[width * height];
            for (var i = 0; i < data.Length; i++) {
                data[i] = (ushort)random.Next(0, 60000);
            }

            var statistics = CreateMeasured(Properties(width, height), data);

            var min = data.Min();
            var max = data.Max();
            statistics.Min.Should().Be(min);
            statistics.MinOccurrences.Should().Be(data.Count(value => value == min));
            statistics.Max.Should().Be(max);
            statistics.MaxOccurrences.Should().Be(data.Count(value => value == max));
            statistics.Mean.Should().BeApproximately(ReferenceMean(data), 1e-6);
            statistics.Median.Should().BeApproximately(ReferenceMedian(data), 1e-6);
            statistics.StDev.Should().BeApproximately(ReferenceStandardDeviation(data), 1e-6);
            statistics.MedianAbsoluteDeviation.Should().BeApproximately(ReferenceMedianAbsoluteDeviation(data), 1e-6);
        }

        [Test]
        public void Create_NarrowDistribution_ComputesMedianAbsoluteDeviation() {
            /* 65x65 = 4225 samples centred on 3000 and spread symmetrically by +/-1..2112 */
            const ushort center = 3000;
            var spread = (OddPixelCount - 1) / 2;
            var data = new ushort[OddPixelCount];
            data[0] = center;
            for (var i = 1; i <= spread; i++) {
                data[(2 * i) - 1] = (ushort)(center - i);
                data[2 * i] = (ushort)(center + i);
            }

            var statistics = CreateMeasured(Properties(OddWidth, OddHeight), data);

            statistics.Min.Should().Be(center - spread);
            statistics.Max.Should().Be(center + spread);
            statistics.Median.Should().BeApproximately(center, 1e-9);
            statistics.Mean.Should().BeApproximately(center, 1e-9);
            statistics.MedianAbsoluteDeviation.Should().BeApproximately(ReferenceMedianAbsoluteDeviation(data), 1e-9);
            statistics.StDev.Should().BeApproximately(ReferenceStandardDeviation(data), 1e-6);
        }

        private static ushort[] BuildHistogramImage(params (ushort Value, int Occurrences)[] bins) {
            return bins.SelectMany(bin => Enumerable.Repeat(bin.Value, bin.Occurrences)).ToArray();
        }

        private static double ReferenceMean(ushort[] data) {
            return data.Sum(value => (double)value) / data.Length;
        }

        private static double ReferenceStandardDeviation(ushort[] data) {
            var mean = ReferenceMean(data);
            var sumSquares = data.Sum(value => (value - mean) * (value - mean));
            return Math.Sqrt(sumSquares / data.Length);
        }

        private static double ReferenceMedian(ushort[] data) {
            var sorted = data.OrderBy(value => value).ToArray();
            var middle = sorted.Length / 2;
            return sorted.Length % 2 == 0
                ? (sorted[middle - 1] + sorted[middle]) / 2d
                : sorted[middle];
        }

        private static double ReferenceMedianAbsoluteDeviation(ushort[] data) {
            var median = ReferenceMedian(data);
            var deviations = data.Select(value => Math.Abs(value - median)).OrderBy(value => value).ToArray();
            var middle = deviations.Length / 2;
            return deviations.Length % 2 == 0
                ? (deviations[middle - 1] + deviations[middle]) / 2d
                : deviations[middle];
        }
    }
}
