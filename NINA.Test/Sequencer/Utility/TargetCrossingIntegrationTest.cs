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
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;

namespace NINA.Test.Sequencer.Utility {
    [TestFixture]
    public class TargetCrossingIntegrationTest {
        private static readonly DateTime Time = new(2026, 1, 1, 22, 0, 0, DateTimeKind.Utc);

        private static IProfileService Profile() {
            var profile = new Mock<IProfileService>();
            profile.Setup(x => x.ActiveProfile.AstrometrySettings).Returns(new AstrometrySettings {
                Latitude = 47,
                Longitude = 8,
                Elevation = 100
            });

            return profile.Object;
        }

        private static Coordinates Coordinates(DateTime time) {
            var clock = Mock.Of<ICustomDateTime>(x => x.Now == time && x.UtcNow == time);
            return new Coordinates(Angle.ByHours(5), Angle.ByDegree(20), Epoch.J2000, time, clock);
        }

        private static WaitLoopData Data(bool horizon = false) => new(Profile(), horizon, "TargetTest") {
            Coordinates = new InputCoordinates(Coordinates(Time)),
            Offset = 30
        };

        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        public void EqualityPoliciesUseExactCurrentSample(int policy, bool alreadySatisfied) {
            var data = Data();
            data.UpdateCurrentTargetPosition(Time);
            data.Offset = data.CurrentAltitude;
            var result = data.CalculateTargetExpectedTime(Time, (TargetCrossingComparison)policy);

            Assert.That(result.Status == TargetCrossingStatus.AlreadySatisfied, Is.EqualTo(alreadySatisfied));
            Assert.That(data.CurrentAltitude, Is.EqualTo(data.Coordinates.Coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 100, Time).Altitude.Degree).Within(1e-10));
            if (alreadySatisfied) {
                Assert.That(data.ExpectedTime, Is.EqualTo(Loc.Instance["LblNow"]));
                Assert.That(data.ExpectedDateTime.ToUniversalTime(), Is.EqualTo(Time));
            }
        }

        [TestCase(29.996)]
        [TestCase(30.004)]
        public void CurrentAltitudeRetainsFullPrecision(double altitude) {
            var data = Data();
            data.CurrentAltitude = altitude;

            Assert.That(data.CurrentAltitude, Is.EqualTo(altitude));
        }

        [TestCase(-0.001, true)]
        [TestCase(0.0, true)]
        [TestCase(0.001, false)]
        public void AboveHorizonCheckPreservesFullPrecisionWithinOneDisplayBucket(double difference, bool expected) {
            var sut = new AboveHorizonCondition(Profile()) { DateTime = Mock.Of<ICustomDateTime>(x => x.Now == Time) };
            sut.Data.Coordinates = new InputCoordinates(Coordinates(Time));
            sut.Data.UpdateCurrentTargetPosition(Time);
            var altitude = sut.Data.CurrentAltitude;
            sut.Offset = altitude + difference;

            Assert.That(sut.Check(null, null), Is.EqualTo(expected));
            Assert.That(Math.Abs(sut.Data.CurrentAltitude - sut.Data.TargetAltitude), Is.LessThanOrEqualTo(0.01));
        }

        [TestCase(-0.001, true)]
        [TestCase(0.0, false)]
        [TestCase(0.001, false)]
        public async Task WaitAboveHorizonUsesStrictPrecision(double difference, bool finishes) {
            var sut = new WaitUntilAboveHorizon(Profile());
            sut.Data.Coordinates = new InputCoordinates(Coordinates(Time));
            sut.Data.UpdateCurrentTargetPosition(Time);
            sut.Offset = sut.Data.CurrentAltitude + difference;
            using var cancellation = new CancellationTokenSource();
            var progress = new ImmediateProgress(_ => cancellation.Cancel());
            if (finishes) {
                await sut.Execute(progress, cancellation.Token);
            } else {
                Assert.CatchAsync<OperationCanceledException>(async () => await sut.Execute(progress, cancellation.Token));
            }
        }

        [TestCase(">", -0.001, true)]
        [TestCase(">", 0.0, true)]
        [TestCase(">", 0.001, false)]
        [TestCase("<", -0.001, false)]
        [TestCase("<", 0.0, true)]
        [TestCase("<", 0.001, true)]
        public async Task WaitForAltitudePreservesInclusiveComparisons(string comparison, double difference, bool finishes) {
            var sut = new WaitForAltitude(Profile()) { AboveOrBelow = comparison };
            sut.Data.Coordinates = new InputCoordinates(Coordinates(Time));
            sut.Data.UpdateCurrentTargetPosition(Time);
            sut.Offset = sut.Data.CurrentAltitude + difference;
            using var cancellation = new CancellationTokenSource();
            var progress = new ImmediateProgress(_ => cancellation.Cancel());
            if (finishes) {
                await sut.Execute(progress, cancellation.Token);
            } else {
                Assert.CatchAsync<OperationCanceledException>(async () => await sut.Execute(progress, cancellation.Token));
            }
        }

        private sealed class ImmediateProgress(Action<ApplicationStatus> callback) : IProgress<ApplicationStatus> {
            public void Report(ApplicationStatus value) => callback(value);
        }

        [TestCase("AboveHorizonCondition")]
        [TestCase("AltitudeCondition")]
        [TestCase("WaitForAltitude")]
        [TestCase("WaitUntilAboveHorizon")]
        public void FixedTargetOffsetReadsPreserveTheCapturedHorizonSample(string caller) {
            WaitLoopData data;
            Func<double> readOffset;
            switch (caller) {
                case "AboveHorizonCondition":
                    var above = new AboveHorizonCondition(Profile()) { Offset = 30 };
                    data = above.Data;
                    readOffset = () => above.Offset;
                    break;
                case "AltitudeCondition":
                    var altitude = new AltitudeCondition(Profile()) { Offset = 30 };
                    data = altitude.Data;
                    readOffset = () => altitude.Offset;
                    break;
                case "WaitForAltitude":
                    var wait = new WaitForAltitude(Profile()) { Offset = 30 };
                    wait.Data = new WaitLoopData(Profile(), true, caller) { Offset = 30 };
                    data = wait.Data;
                    readOffset = () => wait.Offset;
                    break;
                default:
                    var horizon = new WaitUntilAboveHorizon(Profile()) { Offset = 30 };
                    data = horizon.Data;
                    readOffset = () => horizon.Offset;
                    break;
            }


            data.Coordinates = new InputCoordinates(Coordinates(Time));
            data.Horizon = CustomHorizon.FromReader_Standard(new StringReader("0 0\n90 20\n180 40\n270 60\n360 80"));
            data.UpdateCurrentTargetPosition(Time);
            var target = data.TargetAltitude;
            var notifications = 0;
            data.PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(WaitLoopData.TargetAltitude)) {
                    notifications++;
                }
            };

            Assert.That(readOffset(), Is.EqualTo(30));
            Assert.That(data.TargetAltitude, Is.EqualTo(target));
            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void OffsetEditRefreshesTargetAltitude() {
            var data = Data(true);
            data.Horizon = CustomHorizon.FromReader_Standard(new StringReader("0 20\n360 20"));
            data.UpdateCurrentTargetPosition(Time);

            data.Offset = 15.004;

            Assert.That(data.TargetAltitude, Is.EqualTo(35.004));
        }

        [Test]
        public void FutureEventIsReusedForFiveMinutesButCurrentSampleRemainsLive() {
            var data = Data();
            var cold = data.CalculateTargetExpectedTime(Time, TargetCrossingComparison.BelowStrict);

            Assert.That(cold.Status, Is.EqualTo(TargetCrossingStatus.Found));
            Assert.That(cold.Evaluations, Is.GreaterThan(1));
            for (int second = 1; second < 300; second++) {
                var result = data.CalculateTargetExpectedTime(Time.AddSeconds(second), TargetCrossingComparison.BelowStrict);

                Assert.That(result.Evaluations, Is.EqualTo(1));
                Assert.That(result.Time, Is.EqualTo(cold.Time));
                var full = data.Coordinates.Coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 100, Time.AddSeconds(second));

                Assert.That(data.CurrentAltitude, Is.EqualTo(full.Altitude.Degree).Within(1e-10));
            }
            Assert.That(data.CalculateTargetExpectedTime(Time.AddSeconds(300), TargetCrossingComparison.BelowStrict).Evaluations, Is.GreaterThan(1));
        }

        [TestCase("Coordinates")]
        [TestCase("Observer")]
        [TestCase("Horizon")]
        [TestCase("Offset")]
        [TestCase("Policy")]
        [TestCase("Backwards")]
        [TestCase("Reached")]
        public void CacheIsInvalidatedByEveryInputAndTimeChange(string change) {
            var data = Data(true);
            var policy = TargetCrossingComparison.BelowStrict;
            var first = data.CalculateTargetExpectedTime(Time, policy);

            Assert.That(first.Status, Is.EqualTo(TargetCrossingStatus.Found));
            var when = Time.AddSeconds(1);
            switch (change) {
                case "Coordinates":
                    data.Coordinates.Coordinates.RA += 0.1;
                    break;
                case "Observer":
                    data.Observer.Longitude += 1;
                    break;
                case "Horizon":
                    data.Horizon = CustomHorizon.FromReader_Standard(new StringReader("0 10\n360 10"));
                    break;
                case "Offset":
                    data.Offset += 1;
                    break;
                case "Policy":
                    policy = TargetCrossingComparison.BelowInclusive;
                    break;
                case "Backwards":
                    when = Time.AddSeconds(-1);
                    break;
                case "Reached":
                    when = first.Time;
                    break;
            }
            var result = data.CalculateTargetExpectedTime(when, policy);
            if (change == "Reached") {
                Assert.That(result.Status, Is.EqualTo(TargetCrossingStatus.AlreadySatisfied));
            } else {
                Assert.That(result.Evaluations, Is.GreaterThan(1));
            }
        }

        [Test]
        public void CloneStartsWithIndependentCacheAndSerializedShapeIsUnchanged() {
            var data = Data();
            data.CalculateTargetExpectedTime(Time, TargetCrossingComparison.BelowStrict);
            var clone = data.Clone();

            Assert.That(clone.CalculateTargetExpectedTime(Time.AddSeconds(1), TargetCrossingComparison.BelowStrict).Evaluations, Is.GreaterThan(1));
            Assert.That(data.CalculateTargetExpectedTime(Time.AddSeconds(1), TargetCrossingComparison.BelowStrict).Evaluations, Is.EqualTo(1));
            var json = JObject.Parse(JsonConvert.SerializeObject(data));

            Assert.That(json.Properties().Select(x => x.Name), Is.EquivalentTo(new[] { "Coordinates", "Offset", "Comparator" }));
        }

        [Test]
        public void UnsuccessfulPredictionClearsTimeAndIsNotCached() {
            var data = Data();
            data.CalculateTargetExpectedTime(Time, TargetCrossingComparison.BelowStrict);
            data.Offset = 90;
            var first = data.CalculateTargetExpectedTime(Time.AddSeconds(1), TargetCrossingComparison.AboveStrict);
            var second = data.CalculateTargetExpectedTime(Time.AddSeconds(2), TargetCrossingComparison.AboveStrict);

            Assert.That(first.Status, Is.EqualTo(TargetCrossingStatus.NoEvent));
            Assert.That(second.Evaluations, Is.GreaterThan(1));

            Assert.That(data.ExpectedTime, Is.EqualTo("--"));
            Assert.That(data.ExpectedDateTime, Is.EqualTo(DateTime.MinValue));

            Assert.That(data.Approximate, Is.Empty);
        }

        [Test]
        public void InvalidHorizonCannotReuseStaleSuccessOrTimestamp() {
            var sut = new AboveHorizonCondition(Profile()) { DateTime = Mock.Of<ICustomDateTime>(x => x.Now == Time) };
            sut.Data.Coordinates = new InputCoordinates(Coordinates(Time));

            Assert.That(sut.Check(null, null), Is.True);
            sut.Data.Horizon = CustomHorizon.FromReader_Standard(new StringReader("0 NaN\n360 0"));

            Assert.That(sut.Check(null, null), Is.False);
            Assert.That(sut.Data.ExpectedTime, Is.EqualTo("--"));

            Assert.That(sut.Data.ExpectedDateTime, Is.EqualTo(DateTime.MinValue));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InvalidInputsClearLiveStateAndCachedPrediction(bool missingCoordinates) {
            var data = Data();
            var originalCoordinates = data.Coordinates;
            var first = data.CalculateTargetExpectedTime(Time, TargetCrossingComparison.BelowStrict);
            Assert.That(first.Status, Is.EqualTo(TargetCrossingStatus.Found));
            data.SetApproximate(true);
            if (missingCoordinates) {
                data.Coordinates = null;
            } else {
                data.Observer.Latitude = double.NaN;
            }

            var invalid = data.CalculateTargetExpectedTime(Time.AddSeconds(1), TargetCrossingComparison.BelowStrict);

            Assert.That(invalid.Status, Is.EqualTo(TargetCrossingStatus.Exhausted));
            Assert.That(invalid.Evaluations, Is.Zero);
            Assert.That(data.CurrentAltitude, Is.NaN);
            Assert.That(data.TargetAltitude, Is.NaN);
            Assert.That(data.IsRising, Is.False);
            Assert.That(data.ExpectedTime, Is.EqualTo("--"));
            Assert.That(data.ExpectedDateTime, Is.EqualTo(DateTime.MinValue));
            Assert.That(data.Approximate, Is.Empty);

            data.Coordinates = originalCoordinates;
            data.Observer.Latitude = 47;
            var restored = data.CalculateTargetExpectedTime(Time.AddSeconds(2), TargetCrossingComparison.BelowStrict);
            Assert.That(restored.Status, Is.EqualTo(TargetCrossingStatus.Found));
            Assert.That(restored.Evaluations, Is.GreaterThan(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyCallbackOverloadsStillHonorSuppliedDelegate(bool obsolete) {
            var data = Data();
            data.Offset = 90;
            data.CurrentAltitude = 0;
            data.Comparator = NINA.Core.Enum.ComparisonOperatorEnum.GREATER_THAN;
            data.ExpectedDateTime = DateTime.Now.AddMinutes(5);
            int calls = 0;
            double Callback(DateTime when, ObserverInfo observer) {
                calls++;
                return 100;
            }

            if (obsolete) {
#pragma warning disable CS0612
                ItemUtility.CalculateExpectedTimeCommon(data, 90, true, 30, Callback);
#pragma warning restore CS0612
            } else {
                ItemUtility.CalculateExpectedTimeCommon(data, true, 30, Callback);
            }

            Assert.That(calls, Is.GreaterThan(0));
            Assert.That(data.ExpectedTime, Is.Not.EqualTo("--"));
        }
    }
}
