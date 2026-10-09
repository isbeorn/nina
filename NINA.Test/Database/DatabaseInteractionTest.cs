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
using NINA.Astrometry;
using NINA.Core.Database;
using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Database {

    [TestFixture, NonParallelizable]
    internal class DatabaseInteractionTest {

        [Test]
        [TestCase("SHEADHEIGHT", null, "SHEADHEIGHT", "ZHADHEIGHT")]    // Returns longest on searches without a search token related to name
        [TestCase("SHEADHEIGHT", "", "SHEADHEIGHT", "ZHADHEIGHT")]    // Doesn't blow up on bad input
        [TestCase("IC 443", "IC4", "IC 443", "LBN 844", "SH2-248")]    // Starts with
        [TestCase("IC 443", "IC4", "IC 443", "IC 44", "LBN 844", "SH2-248")]    // Starts with + Length priority
        [TestCase("IC 443", "43", "IC 443", "LBN 844", "SH2-248")]    // Levenshtein
        [TestCase("SHEADHEIGHT", "ZHEAD", "SHEADHEIGHT", "ZHADHEIGHT")]    // Levenshtein + Length priority
        [TestCase("ZHADHEIGHT", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "SHEADHEIGHT", "ZHADHEIGHT")]    // Doesn't blow up on bad input
        public void testGetDisplayAliasSuccesses(string expected, string? searchString, params string[] aliases) {
            // Given a DatabaseInteraction Object
            DatabaseInteraction databaseInteraction = new DatabaseInteraction();
            // And a search term

            // And search results with aliass
            List<String> aliasList = aliases.ToList<string>();

            // When locating the closest alias
            String result = databaseInteraction.GetDisplayAlias(searchString, aliasList);

            // Then closest alias should be the expected value
            result.Should().Be(expected);
        }

        [Test]
        public async Task GetDeepSkyObjects_PreservesSortOrderAfterAliasHydration() {
            var databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"dso-order-{Guid.NewGuid():N}.sqlite");

            try {
                CreateMinimalDsoDatabase(databasePath);
                var databaseInteraction = new DatabaseInteraction($"Data Source={databasePath};Pooling=False;");
                var searchParams = new DatabaseInteraction.DeepSkyObjectSearchParams {
                    SearchOrder = new DatabaseInteraction.DeepSkyObjectSearchOrder {
                        Field = "sizemax",
                        Direction = "DESC"
                    }
                };

                var result = await databaseInteraction.GetDeepSkyObjects(null as string, null, searchParams, CancellationToken.None);

                result.Select(x => x.Id).Should().Equal("Large", "Medium", "Small");
                result.Single(x => x.Id == "Medium").AlsoKnownAs.Should().Contain(new[] { "M 2", "Medium Name" });
            } finally {
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (File.Exists(databasePath)) {
                    File.Delete(databasePath);
                }
            }
        }

        [Test]
        public async Task ReadOnlyCatalogQueries_MapExpectedRows() {
            var databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"catalog-readonly-{Guid.NewGuid():N}.sqlite");

            try {
                CreateMinimalCatalogDatabase(databasePath);
                var databaseInteraction = new DatabaseInteraction($"Data Source={databasePath};Pooling=False;");

                var brightStars = await databaseInteraction.GetBrightStars();
                brightStars.Select(x => x.Name).Should().BeEquivalentTo("Alpha", "Beta");

                var alpha = brightStars.Single(x => x.Name == "Alpha");
                alpha.Coordinates.RADegrees.Should().BeApproximately(15, 1e-8);
                alpha.Coordinates.Dec.Should().BeApproximately(-10, 1e-8);
                alpha.Magnitude.Should().BeApproximately(1.23, 1e-8);

                var constellations = await databaseInteraction.GetConstellationsWithStars(CancellationToken.None);
                var constellation = constellations.Should().ContainSingle(x => x.Id == "ORI").Subject;
                constellation.StarConnections.Should().HaveCount(2);
                constellation.Stars.Select(x => x.Name).Should().BeEquivalentTo("Betelgeuse", "Bellatrix", "Rigel");
                constellation.GoesOverRaZero.Should().BeTrue();

                var boundaries = await databaseInteraction.GetConstellationBoundaries(CancellationToken.None);
                var boundary = boundaries.Should().ContainSingle(x => x.Name == "ORI").Subject;
                boundary.Boundaries.Select(x => x.RA).Should().Equal(1, 2);
                boundary.Boundaries.Select(x => x.Dec).Should().Equal(-5, 10);

                var hipsSkyMaps = await databaseInteraction.GetHipsSkyMaps();
                var hipsSkyMap = hipsSkyMaps.Should().ContainSingle().Subject;
                hipsSkyMap.Id.Should().Be("TEST_HIPS");
                hipsSkyMap.ShortName.Should().Be("Test");
                hipsSkyMap.LongName.Should().Be("Test HiPS");
                hipsSkyMap.Path.Should().Be("example/path");
                hipsSkyMap.Band.Should().Be("visible");
                hipsSkyMap.Coverage.Should().BeApproximately(42.5, 1e-8);
                hipsSkyMap.Comment.Should().Be("Synthetic test map");
            } finally {
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (File.Exists(databasePath)) {
                    File.Delete(databasePath);
                }
            }
        }

        [Test]
        public async Task FilterQueries_ReturnDistinctValuesAndPreserveCancellation() {
            var databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"dso-filters-{Guid.NewGuid():N}.sqlite");
            try {
                CreateMinimalDsoDatabase(databasePath);
                var databaseInteraction = new DatabaseInteraction($"Data Source={databasePath};Pooling=False;");

                (await databaseInteraction.GetConstellations(CancellationToken.None)).Should().Equal("ORI");
                (await databaseInteraction.GetObjectTypes(CancellationToken.None)).Should().Equal("GALAXY");

                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                (await databaseInteraction.GetConstellations(cancellation.Token)).Should().BeEmpty();
                (await databaseInteraction.GetObjectTypes(cancellation.Token)).Should().BeEmpty();
            } finally {
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(databasePath)) File.Delete(databasePath);
            }
        }

        [Test]
        public async Task GetUT1_UTC_BracketsAndPreservesLeapJumpOutsideCoverage() {
            var databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"earth-rotation-brackets-{Guid.NewGuid():N}.sqlite");
            var date = new DateTime(2016, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            try {
                var database = new DatabaseInteraction($"Data Source={databasePath};Pooling=False;");
                using (var context = database.GetContext()) {
                    context.Database.ExecuteSqlCommand("DELETE FROM earthrotationparameters");
                    foreach (var row in new[] { (date, -0.4), (date.AddDays(1), 0.6) }) {
                        context.Database.ExecuteSqlCommand(
                            "INSERT OR REPLACE INTO earthrotationparameters (date,modifiedjuliandate,x,y,ut1_utc,lod,dx,dy) VALUES (@p0,@p1,0,0,@p2,1,0,0)",
                            CoreUtil.DateTimeToUnixTimeStamp(row.Item1), AstroUtil.GetJulianDate(row.Item1) - 2400000.5, row.Item2);
                    }
                }
                (await database.GetUT1_UTC(date.AddHours(18), CancellationToken.None)).Should().BeApproximately(-0.4, 1e-10);
                (await database.GetUT1_UTC(date.AddDays(2), CancellationToken.None)).Should().BeApproximately(0.6, 1e-10);
                (await database.GetUT1_UTC(date.AddDays(-1), CancellationToken.None)).Should().BeApproximately(-0.4, 1e-10);
            } finally {
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(databasePath)) File.Delete(databasePath);
            }
        }

        [TestCase("intraday")]
        [TestCase("gap")]
        [TestCase("missing-before")]
        [TestCase("missing-after")]
        [TestCase("before")]
        [TestCase("after")]
        [TestCase("empty")]
        public async Task GetUT1_UTC_ColdDay_ReadsExactlyBracketingRowsInOneIndexedQuery(string scenario) {
            var databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"earth-rotation-query-{Guid.NewGuid():N}.sqlite");
            var date = new DateTime(2024, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            var hours = scenario switch {
                "intraday" => new[] { -48, -2, 0, 6, 18, 24, 26, 48 },
                "gap" => new[] { -48, -2, 26, 48 },
                "missing-before" => new[] { 6, 18, 26, 48 },
                "missing-after" => new[] { -48, -2, 6, 18 },
                "before" => new[] { -48, -2 },
                "after" => new[] { 26, 48 },
                _ => Array.Empty<int>()
            };
            var expectedHours = hours.Where(hour => hour >= 0 && hour <= 24).ToList();
            if (hours.Any(hour => hour < 0)) expectedHours.Insert(0, hours.Where(hour => hour < 0).Max());
            if (hours.Any(hour => hour > 24)) expectedHours.Add(hours.Where(hour => hour > 24).Min());
            var invalidate = typeof(DatabaseInteraction).GetMethod("InvalidateEarthRotationCache", BindingFlags.NonPublic | BindingFlags.Static)!;
            var interceptor = new EarthRotationReadInterceptor();
            try {
                var database = new DatabaseInteraction($"Data Source={databasePath};Pooling=False;");
                using (var context = database.GetContext()) {
                    context.Database.ExecuteSqlCommand("DELETE FROM earthrotationparameters");
                    foreach (int hour in hours) {
                        var instant = date.AddHours(hour);
                        context.Database.ExecuteSqlCommand(
                            "INSERT OR REPLACE INTO earthrotationparameters (date,modifiedjuliandate,x,y,ut1_utc,lod,dx,dy) VALUES (@p0,@p1,0,0,@p2,1,0,0)",
                            CoreUtil.DateTimeToUnixTimeStamp(instant), AstroUtil.GetJulianDate(instant) - 2400000.5, hour * 0.01);
                    }
                }
                invalidate.Invoke(null, null);
                DbInterception.Add(interceptor);
                double correction = await database.GetUT1_UTC(date.AddHours(12), CancellationToken.None);
                interceptor.Reads.Should().Be(1, "one cold UTC day must fetch its in-day rows and nearest external endpoints in one round trip");
                double expected = scenario switch { "empty" => double.NaN, "before" => -0.02, "after" => 0.26, _ => 0.12 };
                if (double.IsNaN(expected)) correction.Should().Be(double.NaN);
                else correction.Should().BeApproximately(expected, 1e-10);
                await database.GetUT1_UTC(date.AddHours(18), CancellationToken.None);
                interceptor.Reads.Should().Be(1, "a second query in the same UTC day must use the prepared snapshot");
                DbInterception.Remove(interceptor);

                TestContext.Out.WriteLine(interceptor.Sql);
                using var connection = new SQLiteConnection($"Data Source={databasePath};Pooling=False;");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = interceptor.Sql;
                foreach (var parameter in interceptor.Parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
                var returnedDates = new List<long>();
                using (var reader = command.ExecuteReader()) {
                    // EF aliases the leading date column as C1 in its UNION projection.
                    while (reader.Read()) returnedDates.Add(reader.GetInt64(0));
                }
                returnedDates.Should().Equal(expectedHours.Select(hour => CoreUtil.DateTimeToUnixTimeStamp(date.AddHours(hour))),
                    "the executed provider SQL must retain all intraday rows and exactly the nearest external endpoints");
                command.CommandText = "EXPLAIN QUERY PLAN " + interceptor.Sql;
                var plan = new List<string>();
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) plan.Add(reader.GetString(3));
                }
                foreach (var step in plan) TestContext.Out.WriteLine(step);
                var tableAliases = Regex.Matches(interceptor.Sql,
                        @"\b(?:FROM|JOIN)\s+\[earthrotationparameters\]\s+AS\s+\[(?<alias>[^\]]+)\]", RegexOptions.IgnoreCase)
                    .Select(match => match.Groups["alias"].Value).Append("earthrotationparameters").ToArray();
                plan.Should().NotContain(step => tableAliases.Any(alias => Regex.IsMatch(step,
                        @"^\s*SCAN\s+(?:TABLE\s+)?" + Regex.Escape(alias) + @"(?:\s|$)", RegexOptions.IgnoreCase)),
                    "an indexed aggregate does not compensate for a full scan of the base EOP table");
                plan.Where(step => step.Contains("SEARCH", StringComparison.OrdinalIgnoreCase) && step.Contains("date", StringComparison.OrdinalIgnoreCase))
                    .Should().HaveCount(3, "each of the in-day, previous and following branches must search the existing date index");
            } finally {
                DbInterception.Remove(interceptor);
                invalidate.Invoke(null, null);
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(databasePath)) File.Delete(databasePath);
            }
        }

        [Test]
        public async Task GetUT1_UTC_ChartNavigation_RetainsDaysAndEvictsOldestAtBound() {
            var databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"earth-rotation-month-{Guid.NewGuid():N}.sqlite");
            var date = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var invalidate = typeof(DatabaseInteraction).GetMethod("InvalidateEarthRotationCache", BindingFlags.NonPublic | BindingFlags.Static)!;
            var interceptor = new EarthRotationReadInterceptor();
            try {
                var database = new DatabaseInteraction($"Data Source={databasePath};Pooling=False;");
                using (var context = database.GetContext()) {
                    context.Database.ExecuteSqlCommand("DELETE FROM earthrotationparameters");
                    for (int day = 0; day <= 97; day++) {
                        var instant = date.AddDays(day);
                        context.Database.ExecuteSqlCommand(
                            "INSERT OR REPLACE INTO earthrotationparameters (date,modifiedjuliandate,x,y,ut1_utc,lod,dx,dy) VALUES (@p0,@p1,0,0,@p2,1,0,0)",
                            CoreUtil.DateTimeToUnixTimeStamp(instant), AstroUtil.GetJulianDate(instant) - 2400000.5, day * 0.001);
                    }
                }
                invalidate.Invoke(null, null);
                DbInterception.Add(interceptor);
                for (int day = 0; day < 96; day++) {
                    (await database.GetUT1_UTC(date.AddDays(day).AddHours(12), CancellationToken.None)).Should().BeApproximately((day + 0.5) * 0.001, 1e-10);
                }
                interceptor.Reads.Should().Be(96);
                for (int day = 95; day >= 0; day--) await database.GetUT1_UTC(date.AddDays(day).AddHours(12), CancellationToken.None);
                interceptor.Reads.Should().Be(96, "revisiting cached chart dates must retain each prepared source snapshot");
                await database.GetUT1_UTC(date.AddDays(96).AddHours(12), CancellationToken.None);
                interceptor.Reads.Should().Be(97);
                await database.GetUT1_UTC(date.AddDays(1).AddHours(12), CancellationToken.None);
                interceptor.Reads.Should().Be(97, "adding the next day must retain other recent days");
                (await database.GetUT1_UTC(date.AddHours(12), CancellationToken.None)).Should().BeApproximately(0.0005, 1e-10);
                interceptor.Reads.Should().Be(98, "the oldest of the 96 retained entries must be evicted when the bound is exceeded");
            } finally {
                DbInterception.Remove(interceptor);
                invalidate.Invoke(null, null);
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(databasePath)) File.Delete(databasePath);
            }
        }

        private sealed class EarthRotationReadInterceptor : DbCommandInterceptor {
            public int Reads { get; private set; }
            public string Sql { get; private set; }
            public List<(string Name, object Value)> Parameters { get; private set; }

            public override void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> context) {
                if (command.CommandText.Contains("ut1_utc", StringComparison.OrdinalIgnoreCase)) {
                    Reads++;
                    if (Reads == 1) {
                        Sql = command.CommandText;
                        Parameters = command.Parameters.Cast<DbParameter>().Select(parameter => (parameter.ParameterName, parameter.Value)).ToList();
                    }
                }
                base.ReaderExecuting(command, context);
            }
        }

        [TestCase("update")]
        [TestCase("expiry")]
        [TestCase("rollover")]
        public async Task EarthRotationCacheStamp_RefreshesPreparedRowsAfterValidityBoundary(string boundary) {
            var databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"earth-rotation-validity-{Guid.NewGuid():N}.sqlite");
            var date = new DateTime(2024, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            var invalidate = typeof(DatabaseInteraction).GetMethod("InvalidateEarthRotationCache", flags)!;
            var getStamp = typeof(DatabaseInteraction).GetMethod("GetEarthRotationCacheStamp", flags)!;
            try {
                invalidate.Invoke(null, null);
                var database = new DatabaseInteraction($"Data Source={databasePath};Pooling=False;");
                using (var context = database.GetContext()) {
                    context.Database.ExecuteSqlCommand("DELETE FROM earthrotationparameters");
                    foreach (var row in new[] { (date, 0.1), (date.AddDays(1), 0.3) }) {
                        context.Database.ExecuteSqlCommand(
                            "INSERT OR REPLACE INTO earthrotationparameters (date,modifiedjuliandate,x,y,ut1_utc,lod,dx,dy) VALUES (@p0,@p1,0,0,@p2,1,0,0)",
                            CoreUtil.DateTimeToUnixTimeStamp(row.Item1), AstroUtil.GetJulianDate(row.Item1) - 2400000.5, row.Item2);
                    }
                }
                var query = date.AddHours(12);
                (await database.GetUT1_UTC(query, CancellationToken.None)).Should().BeApproximately(0.2, 1e-10);
                var first = ((long Generation, DateTime Expires))getStamp.Invoke(null, null)!;
                first.Expires.Should().BeAfter(DateTime.UtcNow);
                first.Expires.Should().BeOnOrBefore(DateTime.UtcNow.Date.AddDays(1));
                ((ValueTuple<long, DateTime>)getStamp.Invoke(null, null)!).Item1.Should().Be(first.Generation);

                using (var context = database.GetContext()) {
                    context.Database.ExecuteSqlCommand("UPDATE earthrotationparameters SET ut1_utc = ut1_utc + 1");
                }
                // A warm snapshot remains coherent until its update, expiry or UTC-day boundary.
                (await database.GetUT1_UTC(query, CancellationToken.None)).Should().BeApproximately(0.2, 1e-10);
                if (boundary == "update") {
                    invalidate.Invoke(null, null);
                } else if (boundary == "expiry") {
                    typeof(DatabaseInteraction).GetField("earthRotationCacheExpires", flags)!.SetValue(null, DateTime.UtcNow.AddSeconds(-1));
                } else {
                    typeof(DatabaseInteraction).GetField("earthRotationCacheExpires", flags)!.SetValue(null, DateTime.UtcNow.Date);
                }
                var invalid = ((long Generation, DateTime Expires))getStamp.Invoke(null, null)!;
                invalid.Generation.Should().BeGreaterThan(first.Generation);
                invalid.Expires.Should().BeOnOrBefore(DateTime.UtcNow);
                (await database.GetUT1_UTC(query, CancellationToken.None)).Should().BeApproximately(1.2, 1e-10);

                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                (await database.GetUT1_UTC(query, cancellation.Token)).Should().Be(double.NaN);
            } finally {
                invalidate.Invoke(null, null);
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(databasePath)) File.Delete(databasePath);
            }
        }

        private static void CreateMinimalDsoDatabase(string databasePath) {
            using var connection = new SQLiteConnection($"Data Source={databasePath};Pooling=False;");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
CREATE TABLE dsodetail (
    id TEXT NOT NULL PRIMARY KEY,
    ra REAL,
    dec REAL,
    magnitude REAL,
    surfacebrightness REAL,
    sizemin NUMERIC,
    sizemax REAL,
    positionangle REAL,
    nrofstars REAL,
    brighteststar REAL,
    constellation TEXT,
    dsotype TEXT,
    dsoclass TEXT,
    notes REAL,
    syncedfrom TEXT,
    lastmodified TEXT
);
CREATE TABLE cataloguenr (
    dsodetailid TEXT,
    catalogue TEXT,
    designation TEXT,
    PRIMARY KEY(dsodetailid, catalogue, designation),
    FOREIGN KEY(dsodetailid) REFERENCES dsodetail(id)
);
PRAGMA user_version = 16;
INSERT INTO dsodetail (id, ra, dec, sizemax, constellation, dsotype) VALUES
    ('Small', 0, 0, 10, 'ORI', 'GALAXY'),
    ('Large', 0, 0, 30, 'ORI', 'GALAXY'),
    ('Medium', 0, 0, 20, 'ORI', 'GALAXY');
INSERT INTO cataloguenr (dsodetailid, catalogue, designation) VALUES
    ('Small', 'S', '1'),
    ('Large', 'L', '3'),
    ('Medium', 'M', '2'),
    ('Medium', 'NAME', 'Medium Name');
";
            command.ExecuteNonQuery();
        }

        private static void CreateMinimalCatalogDatabase(string databasePath) {
            using var connection = new SQLiteConnection($"Data Source={databasePath};Pooling=False;");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
CREATE TABLE brightstars (
    name TEXT NOT NULL PRIMARY KEY,
    ra REAL,
    dec REAL,
    magnitude REAL,
    syncedfrom TEXT
);
CREATE TABLE constellationstar (
    id INTEGER NOT NULL PRIMARY KEY,
    name TEXT,
    ra REAL NOT NULL,
    dec REAL NOT NULL,
    mag REAL
);
CREATE TABLE constellation (
    constellationid TEXT,
    starid INTEGER,
    followstarid INTEGER
);
CREATE TABLE constellationboundaries (
    constellation TEXT,
    position INTEGER,
    ra REAL,
    dec REAL
);
CREATE TABLE hipsskymaps (
    id TEXT NOT NULL PRIMARY KEY,
    shortname TEXT NOT NULL,
    longname TEXT NOT NULL,
    path TEXT NOT NULL,
    band TEXT,
    coverage REAL,
    comment TEXT
);
PRAGMA user_version = 16;
INSERT INTO brightstars (name, ra, dec, magnitude, syncedfrom) VALUES
    ('Alpha', 15, -10, 1.23, 'test'),
    ('Beta', 30, 20, 2.34, 'test');
INSERT INTO constellationstar (id, name, ra, dec, mag) VALUES
    (1, 'Betelgeuse', 350, 7, 0.5),
    (2, 'Rigel', 10, -8, 0.2),
    (3, 'Bellatrix', 45, 6, 1.6);
INSERT INTO constellation (constellationid, starid, followstarid) VALUES
    ('ORI', 1, 2),
    ('ORI', 2, 3);
INSERT INTO constellationboundaries (constellation, position, ra, dec) VALUES
    ('ORI', 2, 2, 10),
    ('ORI', 1, 1, -5);
INSERT INTO hipsskymaps (id, shortname, longname, path, band, coverage, comment) VALUES
    ('TEST_HIPS', 'Test', 'Test HiPS', 'example/path', 'visible', 42.5, 'Synthetic test map');
";
            command.ExecuteNonQuery();
        }
    }
}
