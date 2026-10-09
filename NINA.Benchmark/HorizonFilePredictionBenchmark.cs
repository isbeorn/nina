#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace NINA.Benchmark {

    // Optional file-driven measurements stay outside the ordinary TargetPrediction filter.
    [MemoryDiagnoser]
    [MedianColumn]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [Config(typeof(TargetPredictionConfig))]
    public class HorizonFilePredictionBenchmark {
        private HorizonFileWorkload workload = null!;
        private WaitLoopData legacyData = null!;
        private WaitLoopData productionData = null!;
        private Action<TargetCrossingRequest?, DateTime, TargetCrossingResult> clearPrediction = null!;
        private readonly LegacyTargetPrediction legacy = new();

        [ParamsSource(nameof(HorizonFiles))]
        public string HorizonFile { get; set; } = "";

        [Params(5, 180)]
        public int LeadMinutes { get; set; }

        public IEnumerable<string> HorizonFiles => HorizonFileWorkload.InputFiles();

        [GlobalSetup]
        public void Setup() {
            workload = HorizonFileWorkload.Create(HorizonFile, LeadMinutes);
            legacyData = workload.Prediction.CreateData();
            productionData = workload.Prediction.CreateData();
            clearPrediction = TargetPredictionWorkload.CacheWriter(productionData);
            HorizonFileDiagnostics.CheckProduction(workload);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Cold")]
        public int LegacyCold() {
            legacyData.ExpectedDateTime = DateTime.MinValue;
            return legacy.Predict(legacyData, workload.Prediction.Time);
        }

        [Benchmark]
        [BenchmarkCategory("Cold")]
        public int ProductionCold() {
            clearPrediction(null, workload.Prediction.Time, default);
            return productionData.CalculateTargetExpectedTime(workload.Prediction.Time,
                TargetCrossingComparison.AboveStrict).Evaluations;
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Repeated36")]
        public int LegacyRepeated() {
            legacyData.ExpectedDateTime = DateTime.MinValue;
            int evaluations = 0;
            for (int update = 0; update < 36; update++) {
                evaluations += legacy.Predict(legacyData, workload.Prediction.Time.AddSeconds(update * 5));
            }

            return evaluations;
        }

        [Benchmark]
        [BenchmarkCategory("Repeated36")]
        public int ProductionRepeated() {
            clearPrediction(null, workload.Prediction.Time, default);
            int evaluations = 0;
            for (int update = 0; update < 36; update++) {
                evaluations += productionData.CalculateTargetExpectedTime(workload.Prediction.Time.AddSeconds(update * 5), TargetCrossingComparison.AboveStrict).Evaluations;
            }

            return evaluations;
        }
    }

    internal sealed record HorizonReference(DateTime Time, DateTime WindowEnd, bool AlreadySatisfied);

    internal sealed record HorizonFileWorkload(string Path, double Declination, int LeadMinutes,
        TargetPredictionWorkload Prediction, HorizonReference Reference, object InputMetadata) {
        internal const string InputVariable = "NINA_TARGET_HORIZON_FILES";

        internal static string[] InputFiles() {
            var value = Environment.GetEnvironmentVariable(InputVariable);
            if (string.IsNullOrWhiteSpace(value)) {
                return [];
            }

            return value.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(System.IO.Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        internal static HorizonFileWorkload Create(string path, int leadMinutes, double declination = 20) {
            TargetPredictionEnvironment.Prepare();
            var bytes = File.ReadAllBytes(path);
            var text = File.ReadAllText(path);
            var horizon = CustomHorizon.FromReader_Standard(new StringReader(text));
            var coordinates = new Coordinates(20, declination, Epoch.J2000, Coordinates.RAType.Degrees);
            var seedTime = new DateTime(2026, 1, 15, 23, 0, 0, DateTimeKind.Utc);
            var seed = new TargetPredictionWorkload(System.IO.Path.GetFileName(path), seedTime,
                coordinates, horizon, 0);

            // Anchor to an independently observed rising transition, never the solver under test.
            var rising = HorizonFileOracle.Find(seed, seedTime, risingTransitionOnly: true);
            var time = rising.Time.AddMinutes(-leadMinutes);
            var prediction = seed with { Time = time };

            // A shaped horizon can have an earlier window inside the selected lead interval.
            // Scan from each actual start rather than assuming the anchor is its first event.
            var reference = HorizonFileOracle.Find(prediction, time);
            return new HorizonFileWorkload(path, declination, leadMinutes, prediction, reference,
                Metadata(bytes, text, horizon));
        }

        private static object Metadata(byte[] bytes, string text, CustomHorizon horizon) {
            var vertices = new SortedDictionary<double, double>();
            foreach (var line in text.Split('\n')) {
                var columns = line.Trim().Split([' ', '\t', ',', ';'], StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length == 2
                    && double.TryParse(columns[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double azimuth)
                    && double.TryParse(columns[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double altitude)) {
                    vertices[azimuth] = altitude;
                }
            }

            int inputCount = vertices.Count;
            if (!vertices.ContainsKey(0)) {
                vertices[0] = horizon.GetAltitude(0);
            }

            if (!vertices.ContainsKey(360)) {
                vertices[360] = horizon.GetAltitude(0);
            }

            var sorted = vertices.ToArray();
            var slopes = Enumerable.Range(1, sorted.Length - 1).Select(index => new {
                FromAzimuth = sorted[index - 1].Key,
                ToAzimuth = sorted[index].Key,
                FromAltitude = sorted[index - 1].Value,
                ToAltitude = sorted[index].Value,
                Slope = (sorted[index].Value - sorted[index - 1].Value) / (sorted[index].Key - sorted[index - 1].Key)
            }).OrderByDescending(segment => Math.Abs(segment.Slope)).Take(4).ToArray();

            return new {
                Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
                InputVertices = inputCount,
                EffectiveVertices = vertices.Count,
                MinimumAltitude = horizon.GetMinAltitude(),
                MaximumAltitude = horizon.GetMaxAltitude(),
                SteepestSegments = slopes
            };
        }
    }

    internal static class HorizonFileOracle {
        private const double ScanSeconds = 1;
        private const double RefineSeconds = 0.001;

        internal static HorizonReference Find(TargetPredictionWorkload workload, DateTime start,
            bool risingTransitionOnly = false) {
            bool previous = workload.AboveHorizon(start);
            if (previous && !risingTransitionOnly) {
                return new HorizonReference(start, EndOfWindow(workload, start), true);
            }

            for (var right = start.AddSeconds(ScanSeconds); right <= start.AddHours(24);
                right = right.AddSeconds(ScanSeconds)) {
                bool qualifies = workload.AboveHorizon(right);
                if (qualifies && !previous) {
                    var position = workload.Coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 450, right);
                    if (!risingTransitionOnly || position.AltitudeSite == AltitudeSite.EAST) {
                        var time = Refine(workload, right.AddSeconds(-ScanSeconds), right, true);
                        return new HorizonReference(time, EndOfWindow(workload, time), false);
                    }
                }

                previous = qualifies;
            }

            throw new InvalidOperationException("The independent full-transform scan found no crossing in 24 hours.");
        }

        private static DateTime EndOfWindow(TargetPredictionWorkload workload, DateTime start) {
            for (var right = start.AddSeconds(ScanSeconds); right <= start.AddHours(24);
                right = right.AddSeconds(ScanSeconds)) {
                if (!workload.AboveHorizon(right)) {
                    return Refine(workload, right.AddSeconds(-ScanSeconds), right, false);
                }
            }

            return start.AddHours(24);
        }

        private static DateTime Refine(TargetPredictionWorkload workload, DateTime left, DateTime right,
            bool qualifyingSide) {
            while ((right - left).TotalSeconds > RefineSeconds) {
                var middle = left.AddTicks((right.Ticks - left.Ticks) / 2);
                if (workload.AboveHorizon(middle) == qualifyingSide) {
                    right = middle;
                } else {
                    left = middle;
                }
            }

            return right;
        }
    }

    internal static class HorizonFileDiagnostics {

        internal static void CheckProduction(HorizonFileWorkload workload) {
            var data = workload.Prediction.CreateData();
            var time = workload.Prediction.Time;
            var expected = workload.Reference;
            var result = data.CalculateTargetExpectedTime(time, TargetCrossingComparison.AboveStrict);
            if (result.Status is not (TargetCrossingStatus.Found or TargetCrossingStatus.AlreadySatisfied)
                || !workload.Prediction.AboveHorizon(result.Time)
                || (result.Time - expected.Time).TotalSeconds is < -0.001 or > 10) {
                throw new InvalidOperationException($"File prediction failed the independent reference: {result}, expected {expected.Time:O}.");
            }
        }

        internal static void Write(string path) {
            var files = HorizonFileWorkload.InputFiles();
            if (files.Length == 0) {
                throw new InvalidOperationException($"Set {HorizonFileWorkload.InputVariable} to absolute input paths separated by '{System.IO.Path.PathSeparator}'.");
            }

            var rows = new List<object>();
            foreach (var file in files) {
                foreach (int lead in new[] { 5, 180 }) {
                    rows.Add(Measure(HorizonFileWorkload.Create(file, lead)));
                }
            }

            // Extra geometry/accuracy diagnostic for the first file, without additional timed cases.
            foreach (int lead in new[] { 5, 180 }) {
                rows.Add(Measure(HorizonFileWorkload.Create(files[0], lead, 60)));
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
            File.WriteAllText(path, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"File horizon diagnostics: {System.IO.Path.GetFullPath(path)}");
        }

        private static object Measure(HorizonFileWorkload workload) {
            var prediction = workload.Prediction;
            var position = prediction.Coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 450,
                workload.Reference.Time);
            var legacy = new LegacyTargetPrediction();
            var oldData = prediction.CreateData();
            var newData = prediction.CreateData();
            var updates = new List<object>();
            int oldTotal = 0;
            int newTotal = 0;
            bool allOldValid = true;
            bool allNewValid = true;
            for (int update = 0; update < 36; update++) {
                var time = prediction.Time.AddSeconds(update * 5);
                // Every update remains before the independently verified first event for these leads.
                // Removing an initial part of that empty interval cannot introduce an earlier window.
                var reference = time < workload.Reference.Time
                    ? workload.Reference
                    : time < workload.Reference.WindowEnd
                        ? workload.Reference with { Time = time, AlreadySatisfied = true }
                        : HorizonFileOracle.Find(prediction, time);
                int oldCount = legacy.Predict(oldData, time);
                var result = newData.CalculateTargetExpectedTime(time, TargetCrossingComparison.AboveStrict);
                var oldTime = oldData.ExpectedTime == Loc.Instance["LblNow"]
                    ? time : oldData.ExpectedDateTime.ToUniversalTime();
                bool oldQualifies = legacy.Found && prediction.AboveHorizon(oldTime);
                bool oldFirstWindow = oldQualifies && oldTime >= reference.Time && oldTime < reference.WindowEnd;
                bool newQualifies = result.Status is TargetCrossingStatus.Found or TargetCrossingStatus.AlreadySatisfied
                    && prediction.AboveHorizon(result.Time);
                double error = (result.Time - reference.Time).TotalSeconds;
                bool newAccurate = newQualifies && error is >= -0.001 and <= 10;
                oldTotal += oldCount;
                newTotal += result.Evaluations;
                allOldValid &= oldFirstWindow;
                allNewValid &= newAccurate;
                updates.Add(new {
                    Update = update,
                    Time = time,
                    Reference = reference,
                    LegacyFound = legacy.Found,
                    LegacyQualifies = oldQualifies,
                    LegacyFirstWindow = oldFirstWindow,
                    LegacyEvent = legacy.Found ? oldTime : (DateTime?)null,
                    LegacyErrorSeconds = legacy.Found ? (oldTime - reference.Time).TotalSeconds : (double?)null,
                    LegacyEvaluations = oldCount,
                    ProductionStatus = result.Status.ToString(),
                    ProductionQualifies = newQualifies,
                    ProductionAccurate = newAccurate,
                    ProductionEvent = result.Time,
                    ProductionErrorSeconds = error,
                    ProductionEvaluations = result.Evaluations
                });
            }

            return new {
                File = workload.Path,
                workload.Declination,
                workload.LeadMinutes,
                Observer = new { Latitude = 47, Longitude = 8, Elevation = 450 },
                RightAscensionDegrees = 20,
                Epoch = "J2000",
                Offset = 0,
                Comparison = "AboveStrict",
                OracleScanSeconds = 1,
                OracleRefinementSeconds = 0.001,
                ReferenceAzimuthDegrees = position.Azimuth.Degree,
                ReferenceAltitudeDegrees = position.Altitude.Degree,
                workload.InputMetadata,
                AllLegacyPredictionsValid = allOldValid,
                AllProductionPredictionsAccurate = allNewValid,
                LegacyEvaluations = oldTotal,
                ProductionEvaluations = newTotal,
                ReductionPercent = 100 * (1 - (double)newTotal / oldTotal),
                ValidSpeedComparison = allOldValid && allNewValid,
                Updates = updates
            };
        }
    }
}
