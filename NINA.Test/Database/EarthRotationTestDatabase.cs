using System.Data.SQLite;

namespace NINA.Test.Database {
    internal static class EarthRotationTestDatabase {
        public static void CreateEmpty(string databasePath) {
            using var connection = new SQLiteConnection($"Data Source={databasePath};Pooling=False;");
            connection.Open();

            using var command = connection.CreateCommand();
            // Preserve Migration/1.sql's INT primary key and its index for query-plan assertions.
            command.CommandText = @"
CREATE TABLE earthrotationparameters (
    date INT,
    modifiedjuliandate REAL,
    x REAL,
    y REAL,
    ut1_utc REAL,
    lod REAL,
    dx REAL,
    dy REAL,
    PRIMARY KEY(date)
);
PRAGMA user_version = 16;
";
            command.ExecuteNonQuery();
        }
    }
}