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
using BenchmarkDotNet.Jobs;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;
using System.Globalization;
using System.IO;
using System.Data.SQLite;
using System.Reflection;
using System.Text.Json;

namespace NINA.Benchmark {

    [MemoryDiagnoser]
    [MedianColumn]
    [Config(typeof(TargetPredictionConfig))]
    public class TargetPredictionColdBenchmark {
        private TargetPredictionWorkload workload = null!;
        private WaitLoopData legacyData = null!;
        private WaitLoopData productionData = null!;
        private Action<TargetCrossingRequest?, DateTime, TargetCrossingResult> clearPrediction = null!;
        private readonly LegacyTargetPrediction legacy = new();

        [Params("FlatNear", "FlatFar", "TerrainNear", "TerrainFar", "Dense", "Narrow", "Circumpolar")]
        public string Scenario { get; set; } = "FlatNear";

        [GlobalSetup]
        public void Setup() {
            workload = TargetPredictionWorkload.Create(Scenario);
            legacyData = workload.CreateData();
            productionData = workload.CreateData();
            clearPrediction = TargetPredictionWorkload.CacheWriter(productionData);
            TargetPredictionDiagnostics.Validate(workload);
        }

        [Benchmark(Baseline = true)]
        public int LegacyCold() {
            legacyData.ExpectedDateTime = DateTime.MinValue;
            return legacy.Predict(legacyData, workload.Time);
        }

        [Benchmark]
        public int ProductionCold() {
            // Clear the existing prediction before each invocation. This is the production cache's
            // normal unresolved-result path and does not add a benchmark seam to production code.
            clearPrediction(null, workload.Time, default);
            return productionData.CalculateTargetExpectedTime(workload.Time,
                TargetCrossingComparison.AboveStrict).Evaluations;
        }
    }

    [MemoryDiagnoser]
    [MedianColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [Config(typeof(TargetPredictionConfig))]
    public class TargetPredictionUpdateBenchmark {
        private TargetPredictionWorkload workload = null!;
        private WaitLoopData legacyRepeatedData = null!;
        private WaitLoopData productionRepeatedData = null!;
        private WaitLoopData legacyCachedData = null!;
        private WaitLoopData productionCachedData = null!;
        private Action<TargetCrossingRequest?, DateTime, TargetCrossingResult> clearPrediction = null!;
        private readonly LegacyTargetPrediction legacy = new();

        [Params("TerrainNear", "TerrainFar")]
        public string Scenario { get; set; } = "TerrainNear";

        [GlobalSetup]
        public void Setup() {
            workload = TargetPredictionWorkload.Create(Scenario);
            legacyRepeatedData = workload.CreateData();
            productionRepeatedData = workload.CreateData();
            legacyCachedData = workload.CreateData();
            productionCachedData = workload.CreateData();
            clearPrediction = TargetPredictionWorkload.CacheWriter(productionRepeatedData);
            legacy.Predict(legacyCachedData, workload.Time);
            productionCachedData.CalculateTargetExpectedTime(workload.Time,
                TargetCrossingComparison.AboveStrict);
            TargetPredictionDiagnostics.Validate(workload);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Repeated36")]
        public int LegacyRepeated() {
            legacyRepeatedData.ExpectedDateTime = DateTime.MinValue;
            int evaluations = 0;
            for (int update = 0; update < 36; update++) {
                evaluations += legacy.Predict(legacyRepeatedData, workload.Time.AddSeconds(update * 5));
            }

            return evaluations;
        }

        [Benchmark]
        [BenchmarkCategory("Repeated36")]
        public int ProductionRepeated() {
            clearPrediction(null, workload.Time, default);
            int evaluations = 0;
            for (int update = 0; update < 36; update++) {
                evaluations += productionRepeatedData.CalculateTargetExpectedTime(workload.Time.AddSeconds(update * 5), TargetCrossingComparison.AboveStrict).Evaluations;
            }

            return evaluations;
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("CachedUpdate")]
        public int LegacyCachedUpdate() {
            return legacy.Predict(legacyCachedData, workload.Time.AddSeconds(5));
        }

        [Benchmark]
        [BenchmarkCategory("CachedUpdate")]
        public int ProductionCachedUpdate() {
            // The captured update remains inside the primed cache's 300 second lifetime.
            return productionCachedData.CalculateTargetExpectedTime(workload.Time.AddSeconds(5), TargetCrossingComparison.AboveStrict).Evaluations;
        }
    }

    public sealed class TargetPredictionConfig : ManualConfig {

        public TargetPredictionConfig() {
            AddJob(Job.Default
                .WithId("TargetPrediction")
                .WithLaunchCount(1)
                .WithWarmupCount(3)
                .WithIterationCount(8)
                .WithArguments([
                    new MsBuildArgument("/p:UseSharedCompilation=false"),
                    new MsBuildArgument("/p:GeneratePackageOnBuild=false"),
                    new MsBuildArgument("/p:RunPostBuildEvent=Never")
                ]));
        }
    }

    internal sealed record TargetPredictionWorkload(string Name, DateTime Time, Coordinates Coordinates,
        CustomHorizon? Horizon, double Offset) {

        internal TargetCrossingRequest Request => new(Coordinates, 47, 8, 450, Horizon!, Offset,
            TargetCrossingComparison.AboveStrict);

        // Bind once during setup so cold measurements can clear the private cache without
        // reflection in the measured operation or a production API solely for benchmarks.
        internal static Action<TargetCrossingRequest?, DateTime, TargetCrossingResult> CacheWriter(WaitLoopData data) {
            return typeof(WaitLoopData).GetMethod("CacheTargetCrossing", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action<TargetCrossingRequest?, DateTime, TargetCrossingResult>>(data);
        }

        internal WaitLoopData CreateData() {
            var settings = new AstrometrySettings {
                Latitude = 47,
                Longitude = 8,
                Elevation = 450,
                Horizon = Horizon
            };
            var profile = BenchmarkGetterProxy.Create<IProfile>("get_AstrometrySettings", settings);
            var service = BenchmarkGetterProxy.Create<IProfileService>("get_ActiveProfile", profile);
            return new WaitLoopData(service, Horizon != null, "WaitUntilAboveHorizon") {
                Coordinates = new InputCoordinates(Coordinates),
                Offset = Offset,
                Comparator = ComparisonOperatorEnum.GREATER_THAN
            };
        }

        internal static TargetPredictionWorkload Create(string name) {
            TargetPredictionEnvironment.Prepare();
            var time = CapturedStart(name);
            var coordinates = new Coordinates(20, name == "Circumpolar" ? 80 : 20,
                Epoch.J2000, Coordinates.RAType.Degrees);
            CustomHorizon? horizon = name switch {
                "TerrainNear" or "TerrainFar" => ReadHorizon("0 12\n60 18\n120 24\n180 16\n240 20\n300 14\n360 12"),
                "Dense" => ReadHorizon(string.Join("\n", Enumerable.Range(0, 721).Select(index =>
                    string.Create(CultureInfo.InvariantCulture,
                        $"{index / 2d} {18 + 4 * Math.Sin(index / 2d * Math.PI / 45)}")))),
                "Circumpolar" => ReadHorizon("0 40\n90 50\n180 40\n270 30\n360 40"),
                _ => null
            };
            double offset = horizon == null ? 20 : 0;

            if (name == "Narrow") {
                // The original flat seed was exactly five minutes after the captured FlatNear
                // start. Keep its timestamp fixed so solver accuracy cannot move the notch.
                var referenceTime = CapturedStart("FlatNear").AddMinutes(5).AddHours(2);
                var center = coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 450,
                    referenceTime).Azimuth.Degree;
                horizon = ReadHorizon(string.Create(CultureInfo.InvariantCulture,
                    $"0 80\n{center - 0.15} 80\n{center - 0.05} 15\n{center + 0.05} 15\n{center + 0.15} 80\n360 80"));
                offset = 0;
            }

            var workload = new TargetPredictionWorkload(name, time, coordinates, horizon, offset);
            if (workload.AboveHorizon(time)) {
                throw new InvalidOperationException($"{name}: selected start already satisfies the predicate.");
            }

            return workload;
        }

        internal bool AboveHorizon(DateTime time) {
            var position = Coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 450, time);
            return position.Altitude.Degree > Offset + (Horizon?.GetAltitude(position.Azimuth.Degree) ?? 0);
        }

        private static DateTime CapturedStart(string name) {
            // Pin every timestamp to initial-bounded-solver-diagnostics.json. Workload selection
            // must not depend on the implementation or resolution of the solver being measured.
            string value = name switch {
                "FlatNear" => "2026-01-16T11:33:06.8855693Z",
                "FlatFar" => "2026-01-16T08:38:06.8855693Z",
                "TerrainNear" => "2026-01-16T11:34:08.0701446Z",
                "TerrainFar" => "2026-01-16T08:39:08.0701446Z",
                "Dense" => "2026-01-15T23:00:00.0000000Z",
                "Narrow" => "2026-01-15T23:00:00.0000000Z",
                "Circumpolar" => "2026-01-16T02:30:00.0000000Z",
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown target prediction workload.")
            };

            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private static CustomHorizon ReadHorizon(string text) {
            return CustomHorizon.FromReader_Standard(new StringReader(text));
        }
    }

    internal static class TargetPredictionEnvironment {
        private static bool prepared;

        internal static void Prepare() {
            if (prepared) {
                return;
            }

            // Seed the existing daily EOP cache through the public database seam. Both paths use
            // zero UT1-UTC in this synthetic fixture. No live user database or profile is opened.
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Database", "Migration"));
            var path = Path.Combine(AppContext.BaseDirectory, $"target-prediction-{Guid.NewGuid():N}.sqlite");
            var connectionString = $"Data Source={path};Pooling=False;";
            using (var connection = new SQLiteConnection(connectionString)) {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = """
                    CREATE TABLE earthrotationparameters (
                        date INTEGER PRIMARY KEY, modifiedjuliandate REAL, x REAL, y REAL,
                        ut1_utc REAL, lod REAL, dx REAL, dy REAL);
                    INSERT INTO earthrotationparameters VALUES (1768435200, 61055, 0, 0, 0, 0, 0, 0);
                    PRAGMA user_version = 16;
                    """;
                command.ExecuteNonQuery();
            }

            var database = new DatabaseInteraction(connectionString);
            var first = new DateTime(2026, 1, 13, 0, 0, 0, DateTimeKind.Utc);
            var dates = Enumerable.Range(0, 9).Select(day => first.AddDays(day))
                .Concat(Enumerable.Range(-1, 3).Select(day => DateTime.UtcNow.Date.AddDays(day)));
            foreach (var date in dates) {
                // WaitLoopData's ordinary constructor/offset setter also reads the current clock
                // during setup, so warm those dates through this isolated connection as well.
                double delta = AstroUtil.DeltaUT(date, database);
                if (delta != 0) {
                    throw new InvalidOperationException($"The isolated EOP seed returned {delta}.");
                }
            }

            prepared = true;
        }
    }

    // These getter-only interface proxies keep setup in memory without a mocking dependency
    // or a real ProfileService that could discover or save the user's profiles.
    public class BenchmarkGetterProxy : DispatchProxy {
        private string getter = "";
        private object value = null!;

        internal static T Create<T>(string getter, object value) where T : class {
            var proxy = Create<T, BenchmarkGetterProxy>();
            var implementation = (BenchmarkGetterProxy)(object)proxy;
            implementation.getter = getter;
            implementation.value = value;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? method, object?[]? arguments) {
            if (method?.Name == getter) {
                return value;
            }

            throw new NotSupportedException($"Unexpected benchmark profile call: {method?.Name}");
        }
    }

    internal static class TargetPredictionDiagnostics {

        internal static void Validate(TargetPredictionWorkload workload) {
            var data = workload.CreateData();
            var result = data.CalculateTargetExpectedTime(workload.Time,
                TargetCrossingComparison.AboveStrict);
            if (result.Status != TargetCrossingStatus.Found
                || !workload.AboveHorizon(result.Time)) {
                throw new InvalidOperationException($"{workload.Name}: production did not return a qualifying event: {result}.");
            }
        }

        internal static void Write(string path) {
            var rows = new List<object>();
            foreach (var name in new[] { "FlatNear", "FlatFar", "TerrainNear", "TerrainFar", "Dense", "Narrow", "Circumpolar" }) {
                var workload = TargetPredictionWorkload.Create(name);
                Validate(workload);
                var legacy = new LegacyTargetPrediction();
                var oldData = workload.CreateData();
                var newData = workload.CreateData();
                int legacyEvaluations = legacy.Predict(oldData, workload.Time);
                var result = newData.CalculateTargetExpectedTime(workload.Time,
                    TargetCrossingComparison.AboveStrict);
                rows.Add(new {
                    Scenario = name,
                    Mode = "Cold",
                    workload.Time,
                    LegacyFound = legacy.Found,
                    LegacyEvent = legacy.Found ? oldData.ExpectedDateTime.ToUniversalTime() : (DateTime?)null,
                    LegacyQualifies = legacy.Found && workload.AboveHorizon(oldData.ExpectedDateTime.ToUniversalTime()),
                    LegacyEvaluations = legacyEvaluations,
                    ProductionStatus = result.Status.ToString(),
                    ProductionEvent = result.Time,
                    ProductionEvaluations = result.Evaluations
                });

                if (name.StartsWith("Terrain", StringComparison.Ordinal)) {
                    int oldCached = legacy.Predict(oldData, workload.Time.AddSeconds(5));
                    var cached = newData.CalculateTargetExpectedTime(workload.Time.AddSeconds(5),
                        TargetCrossingComparison.AboveStrict);
                    rows.Add(new {
                        Scenario = name,
                        Mode = "CachedUpdate",
                        LegacyFound = legacy.Found,
                        LegacyEvaluations = oldCached,
                        ProductionStatus = cached.Status.ToString(),
                        ProductionEvaluations = cached.Evaluations,
                        ProductionEvent = cached.Time
                    });

                    int oldTotal = 0;
                    int newTotal = 0;
                    int coldRefreshes = 0;
                    bool legacyFoundEveryUpdate = true;
                    oldData = workload.CreateData();
                    newData = workload.CreateData();
                    var updates = new List<object>();
                    for (int update = 0; update < 36; update++) {
                        var time = workload.Time.AddSeconds(update * 5);
                        int oldCount = legacy.Predict(oldData, time);
                        var current = newData.CalculateTargetExpectedTime(time,
                            TargetCrossingComparison.AboveStrict);
                        if (current.Status != TargetCrossingStatus.Found) {
                            throw new InvalidOperationException($"{name}: update {update} returned {current.Status}.");
                        }

                        oldTotal += oldCount;
                        newTotal += current.Evaluations;
                        legacyFoundEveryUpdate &= legacy.Found;
                        if (current.Evaluations > 1) {
                            coldRefreshes++;
                        }

                        updates.Add(new {
                            Update = update,
                            Time = time,
                            LegacyFound = legacy.Found,
                            LegacyEvaluations = oldCount,
                            ProductionStatus = current.Status.ToString(),
                            ProductionEvaluations = current.Evaluations,
                            ProductionEvent = current.Time
                        });
                    }

                    rows.Add(new {
                        Scenario = name,
                        Mode = "Repeated36",
                        LegacyEvaluations = oldTotal,
                        ProductionEvaluations = newTotal,
                        ColdRefreshes = coldRefreshes,
                        LegacyFoundEveryUpdate = legacyFoundEveryUpdate,
                        MaximumProductionEvaluations = oldTotal / 10,
                        PassedEvaluationTarget = legacyFoundEveryUpdate && newTotal <= oldTotal / 10,
                        ReductionPercent = 100 * (1 - (double)newTotal / oldTotal),
                        Updates = updates
                    });
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Target prediction diagnostics: {Path.GetFullPath(path)}");
        }
    }
}
