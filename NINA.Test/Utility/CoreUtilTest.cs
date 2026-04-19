#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Utility {

    [TestFixture]
    public class CoreUtilTest {

        /// <summary>
        /// Verifies byte formatting at representative binary units so image/file sizes remain readable in diagnostics.
        /// </summary>
        [Test]
        [TestCase(0L, "0 Bytes")]
        [TestCase(5000L, "4.88 KiB")]
        [TestCase(5000000L, "4.77 MiB")]
        [TestCase(5000000000L, "4.66 GiB")]
        public void FormatBytes_RepresentativeValues_UsesLargestBinaryUnit(long bytes, string expected) {
            Assert.That(CoreUtil.FormatBytes(bytes), Is.EqualTo(expected));
        }

        /// <summary>
        /// Verifies Euclidean modulus semantics for positive, negative, and zero divisors using deterministic numeric examples.
        /// </summary>
        [Test]
        public void EuclidianModulus_NegativeDividendAndDivisor_ReturnsMathematicalRemainderSign() {
            Assert.That(CoreUtil.EuclidianModulus(-1d, 360d), Is.EqualTo(359d));
            Assert.That(CoreUtil.EuclidianModulus(361d, 360d), Is.EqualTo(1d));
            Assert.That(CoreUtil.EuclidianModulus(1d, -360d), Is.EqualTo(-359d));
            Assert.That(CoreUtil.EuclidianModulus(5d, 0d), Is.NaN);
        }

        /// <summary>
        /// Verifies closest-step rounding for positive, negative, and tie cases that are common in device setting increments.
        /// </summary>
        [Test]
        [TestCase(12.24d, 0.5d, 12.0d)]
        [TestCase(12.25d, 0.5d, 12.5d)]
        [TestCase(-12.24d, 0.5d, -12.0d)]
        [TestCase(-12.25d, 0.5d, -12.5d)]
        [TestCase(0d, 0.5d, 0d)]
        public void GetClosestNumber_RepresentativeSteps_RoundsToNearestMagnitude(double value, double step, double expected) {
            Assert.That(CoreUtil.GetClosestNumber(value, step), Is.EqualTo(expected));
        }

        /// <summary>
        /// Verifies that filename sanitization replaces path separators and platform-invalid filename characters deterministically.
        /// </summary>
        [Test]
        public void ReplaceAllInvalidFilenameChars_PathSeparatorsAndInvalidCharacters_ReplacesWithSafeText() {
            string value = "M31/Light\\Frame" + new string(Path.GetInvalidFileNameChars()[0], 1);

            string result = CoreUtil.ReplaceAllInvalidFilenameChars(value);

            Assert.That(result, Does.Contain("M31-Light-Frame"));
            Assert.That(result.IndexOfAny(Path.GetInvalidFileNameChars()), Is.EqualTo(-1));
        }

        /// <summary>
        /// Verifies Unix timestamp conversion against a fixed UTC instant without depending on local clock state.
        /// </summary>
        [Test]
        public void DateTimeToUnixTimeStamp_FixedUtcInstant_ReturnsExpectedSeconds() {
            DateTime instant = new DateTime(2026, 4, 17, 1, 2, 3, DateTimeKind.Utc);

            long result = CoreUtil.DateTimeToUnixTimeStamp(instant);

            Assert.That(result, Is.EqualTo(1776387723));
            Assert.That(CoreUtil.UnixTimeStampToDateTime(1776387723d), Is.EqualTo(instant));
        }

        /// <summary>
        /// Verifies that list serialization round-trips real values and malformed input fails closed to an empty list.
        /// </summary>
        [Test]
        public void SerializeAndDeserializeList_ValidAndInvalidInput_RoundTripsOrReturnsEmpty() {
            List<string> filters = new List<string> { "Lum", "Red", "OIII" };

            string serialized = CoreUtil.SerializeList(filters);
            IList<string> roundTrip = CoreUtil.DeserializeList<string>(serialized);
            IList<string> invalid = CoreUtil.DeserializeList<string>("{not-json");

            Assert.That(roundTrip, Is.EqualTo(filters));
            Assert.That(invalid, Is.Empty);
        }

        /// <summary>
        /// Verifies that negative-duration delays do not sleep and still report a non-negative elapsed time.
        /// </summary>
        [Test]
        public async Task Delay_NegativeDuration_DoesNotSleepAndReturnsElapsedTime() {
            using CancellationTokenSource cts = new CancellationTokenSource();

            TimeSpan elapsed = await CoreUtil.Delay(TimeSpan.FromMilliseconds(-1), cts.Token);

            Assert.That(elapsed, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
            Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(1)));
        }
    }
}
