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
using NINA.Core.Enum;
using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class TopocentricCoordinatesTest {
        /// <summary>
        /// Verifies topocentric coordinate value semantics: altitude-side classification, clone
        /// independence, default constructors, and diagnostic text.
        /// </summary>
        [Test]
        public void TopocentricCoordinates_ValueSemantics_PreserveAnglesAndSiteSide() {
            TopocentricCoordinates east = new TopocentricCoordinates(
                Angle.ByDegree(90.0),
                Angle.ByDegree(45.0),
                Angle.ByDegree(51.5),
                Angle.ByDegree(13.0),
                100.0);
            TopocentricCoordinates west = new TopocentricCoordinates(
                Angle.ByDegree(270.0),
                Angle.ByDegree(45.0),
                Angle.ByDegree(51.5),
                Angle.ByDegree(13.0));

            TopocentricCoordinates copy = east.Copy();
            TopocentricCoordinates clone = east.Clone();

            east.AltitudeSite.Should().Be(AltitudeSite.EAST);
            west.AltitudeSite.Should().Be(AltitudeSite.WEST);
            copy.Should().NotBeSameAs(east);
            clone.Azimuth.Degree.Should().Be(east.Azimuth.Degree);
            clone.Elevation.Should().Be(100.0);
            west.Elevation.Should().Be(0.0);
            east.ToString().Should().Contain("Alt:");
        }

        /// <summary>
        /// Verifies topocentric compatibility transform overloads return finite celestial
        /// coordinates when no explicit observation time is supplied.
        /// </summary>
        [Test]
        public void TopocentricTransform_CompatibilityOverloads_ReturnFiniteCoordinates() {
            TopocentricCoordinates topocentric = new TopocentricCoordinates(
                Angle.ByDegree(180.0),
                Angle.ByDegree(60.0),
                Angle.ByDegree(35.0),
                Angle.ByDegree(-105.0),
                1600.0);

            Coordinates noRefraction = topocentric.Transform(Epoch.J2000);
            Coordinates withRefraction = topocentric.Transform(Epoch.J2000, 800.0, 5.0, 20.0, 0.574);

            noRefraction.RADegrees.Should().BeInRange(0.0, 360.0);
            noRefraction.Dec.Should().BeInRange(-90.0, 90.0);
            withRefraction.RADegrees.Should().BeInRange(0.0, 360.0);
            withRefraction.Dec.Should().BeInRange(-90.0, 90.0);
        }

        /// <summary>
        /// Verifies topocentric-to-celestial conversion consumes UT1-UTC from the Earth-rotation
        /// table. A controlled one-second UT1 change should shift right ascension by roughly one
        /// sidereal second while leaving the sky coordinate otherwise physically valid.
        /// Reference: https://www.iers.org/IERS/EN/DataProducts/EarthOrientationData/eop.html
        /// </summary>
        [Test]
        public void Transform_ControlledUt1MinusUtc_ShiftsRightAscensionBySiderealRotation() {
            DateTime date = new DateTime(2024, 4, 8, 18, 0, 0, DateTimeKind.Utc);
            TopocentricCoordinates topocentric = new TopocentricCoordinates(
                Angle.ByDegree(180.0),
                Angle.ByDegree(60.0),
                Angle.ByDegree(35.0),
                Angle.ByDegree(-105.0),
                1600.0);
            using TempEarthRotationDatabase zeroUt1Database = CreateEarthRotationDatabase((date, 0.0));
            using TempEarthRotationDatabase oneSecondUt1Database = CreateEarthRotationDatabase((date, 1.0));

            ClearDeltaUTCaches();
            Coordinates zeroUt1 = topocentric.Transform(date, Epoch.J2000, 0.0, 0.0, 0.0, 0.0, zeroUt1Database.Interaction);
            ClearDeltaUTCaches();
            Coordinates oneSecondUt1 = topocentric.Transform(date, Epoch.J2000, 0.0, 0.0, 0.0, 0.0, oneSecondUt1Database.Interaction);

            AngularDifference(oneSecondUt1.RADegrees, zeroUt1.RADegrees).Should().BeInRange(10.0 / 3600.0, 20.0 / 3600.0);
            oneSecondUt1.Dec.Should().BeInRange(-90.0, 90.0);
        }

        private static double AngularDifference(double actualDegrees, double expectedDegrees) {
            double difference = Math.Abs(AstroUtil.EuclidianModulus(actualDegrees - expectedDegrees + 180.0, 360.0) - 180.0);
            return difference;
        }

        private static TempEarthRotationDatabase CreateEarthRotationDatabase(params (DateTime Date, double Ut1MinusUtc)[] rows) {
            string databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"earth-rotation-{Guid.NewGuid():N}.sqlite");
            string connectionString = $"Data Source={databasePath};";
            DatabaseInteraction databaseInteraction = new DatabaseInteraction(connectionString);

            using (var context = databaseInteraction.GetContext()) {
                foreach ((DateTime date, double ut1MinusUtc) in rows) {
                    long unixTimestamp = CoreUtil.DateTimeToUnixTimeStamp(date);
                    double modifiedJulianDate = AstroUtil.GetJulianDate(date) - 2400000.5;
                    context.Database.ExecuteSqlCommand(
                        "INSERT OR REPLACE INTO `earthrotationparameters` (date, modifiedjuliandate, x, y, ut1_utc, lod, dx, dy) VALUES (@p0, @p1, 0, 0, @p2, 1, 0, 0)",
                        unixTimestamp,
                        modifiedJulianDate,
                        ut1MinusUtc);
                }
            }

            return new TempEarthRotationDatabase(databasePath, databaseInteraction);
        }

        private static void ClearDeltaUTCaches() {
            typeof(AstroUtil).GetField("DeltaUTToday", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            typeof(AstroUtil).GetField("DeltaUTYesterday", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            typeof(AstroUtil).GetField("DeltaUTTomorrow", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            typeof(AstroUtil).GetField("DeltaUTReference", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, default(DateTime));

            FieldInfo cacheField = typeof(AstroUtil).GetField("DeltaUTCache", BindingFlags.NonPublic | BindingFlags.Static);
            cacheField?.SetValue(null, new ConcurrentDictionary<DateTime, double>());
        }

        private sealed class TempEarthRotationDatabase : IDisposable {
            public TempEarthRotationDatabase(string path, DatabaseInteraction interaction) {
                Path = path;
                Interaction = interaction;
            }

            public string Path { get; }
            public DatabaseInteraction Interaction { get; }

            public void Dispose() {
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (File.Exists(Path)) {
                    File.Delete(Path);
                }
            }
        }
    }
}
