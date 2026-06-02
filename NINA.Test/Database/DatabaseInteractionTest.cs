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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Database {

    [TestFixture]
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
