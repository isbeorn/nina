#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility {

    internal class DatabaseInteraction {

        static DatabaseInteraction() {
            DllLoader.LoadDll("SQLite\\SQLite.Interop.dll");
        }

        private string _connectionString;

        public DatabaseInteraction(string dbLocation) {
            _connectionString = string.Format(@"Data Source={0};foreign keys=true;", dbLocation);
        }

        public async Task<ICollection<string>> GetConstellations(CancellationToken token) {
            const string query = "SELECT DISTINCT(constellation) FROM dsodetail;";
            var constellations = new List<string>() { string.Empty };

            try {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString)) {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand()) {
                        command.CommandText = query;

                        var reader = await command.ExecuteReaderAsync(token);

                        while (reader.Read()) {
                            constellations.Add(reader["constellation"].ToString());
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError(ex.Message);
            }

            return constellations;
        }

        public async Task<ICollection<string>> GetObjectTypes(CancellationToken token) {
            const string query = "SELECT DISTINCT(dsotype) FROM dsodetail;";
            var dsotypes = new List<string>() { };
            try {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString)) {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand()) {
                        command.CommandText = query;

                        var reader = await command.ExecuteReaderAsync(token);

                        while (reader.Read()) {
                            dsotypes.Add(reader["dsotype"].ToString());
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError(ex.Message);
            }

            return dsotypes;
        }

        public async Task<List<FocusTarget>> GetBrightStars() {
            string query = @"SELECT name, ra, dec, magnitude
                            FROM brightstars";

            var brightStars = new List<FocusTarget>();
            try {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString)) {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand()) {
                        command.CommandText = query;

                        var reader = await command.ExecuteReaderAsync();

                        while (reader.Read()) {
                            var brightStar = new FocusTarget(reader.GetString(0));

                            var coords = new Coordinates(reader.GetDouble(1), reader.GetDouble(2), Epoch.J2000, Coordinates.RAType.Degrees);
                            brightStar.Coordinates = coords;

                            brightStar.Magnitude = reader.GetDouble(3);

                            brightStars.Add(brightStar);
                        }
                    }
                }
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.Notification.ShowError(ex.Message);
                }
            }

            return brightStars;
        }

        public class DeepSkyObjectSearchParams {
            public string Constellation { get; set; } = "";
            public IList<string> DsoTypes { get; set; }
            public DeepSkyObjectSearchFromThru<double?> RightAscension { get; set; } = new DeepSkyObjectSearchFromThru<double?>();
            public DeepSkyObjectSearchFromThru<double?> Declination { get; set; } = new DeepSkyObjectSearchFromThru<double?>();
            public DeepSkyObjectSearchFromThru<string> Brightness { get; set; } = new DeepSkyObjectSearchFromThru<string>();
            public DeepSkyObjectSearchFromThru<string> Size { get; set; } = new DeepSkyObjectSearchFromThru<string>();
            public DeepSkyObjectSearchFromThru<string> Magnitude { get; set; } = new DeepSkyObjectSearchFromThru<string>();
            public string ObjectName { get; set; } = string.Empty;
            public DeepSkyObjectSearchOrder SearchOrder { get; set; } = new DeepSkyObjectSearchOrder();
            public int? Limit { get; set; }
        }

        public class DeepSkyObjectSearchOrder {
            public string Field { get; set; } = "id";
            public string Direction { get; set; } = "ASC";
        }

        public class DeepSkyObjectSearchFromThru<T> {
            public T From { get; set; }
            public T Thru { get; set; }
        }

        public class DeepSkyObjectSearchCoordinates {
            public double? RaFrom { get; set; } = null;
            public double? RaThru { get; set; } = null;
            public double? DecFrom { get; set; } = null;
            public double? DecThru { get; set; } = null;
        }

        public async Task<List<DeepSkyObject>> GetDeepSkyObjects(
            string imageRepository,
            DeepSkyObjectSearchParams searchParams,
            CancellationToken token) {
            if (searchParams == null) { throw new ArgumentNullException(nameof(searchParams)); }

            string query = @"SELECT id, ra, dec, dsotype, magnitude, sizemax, group_concat(cataloguenr.catalogue || ' ' || cataloguenr.designation) aka, constellation, surfacebrightness
                             FROM dsodetail
                                INNER JOIN cataloguenr on dsodetail.id = cataloguenr.dsodetailid
                             WHERE (1=1) ";

            if (!string.IsNullOrEmpty(searchParams.Constellation)) {
                query += " AND constellation = $constellation ";
            }

            if (searchParams.RightAscension.From != null) {
                query += " AND ra >= $rafrom ";
            }

            if (searchParams.RightAscension.Thru != null) {
                query += " AND ra <= $rathru ";
            }

            if (searchParams.Declination.From != null) {
                query += " AND dec >= $decfrom ";
            }

            if (searchParams.Declination.Thru != null) {
                query += " AND dec <= $decthru ";
            }

            if (!string.IsNullOrEmpty(searchParams.Size?.From)) {
                query += " AND sizemin >= $sizefrom ";
            }

            if (!string.IsNullOrEmpty(searchParams.Size?.Thru)) {
                query += " AND sizemax <= $sizethru ";
            }

            if (searchParams.DsoTypes?.Count > 0) {
                query += " AND dsotype IN (";
                for (int i = 0; i < searchParams.DsoTypes.Count; i++) {
                    query += "$dsotype" + i.ToString() + ",";
                }
                query = query.Remove(query.Length - 1);
                query += ") ";
            }

            if (!string.IsNullOrEmpty(searchParams.Brightness?.From)) {
                query += " AND surfacebrightness >= $brightnessfrom ";
            }

            if (!string.IsNullOrEmpty(searchParams.Brightness?.Thru)) {
                query += " AND surfacebrightness <= $brightnessthru ";
            }

            if (!string.IsNullOrEmpty(searchParams.Magnitude?.From)) {
                query += " AND magnitude >= $magnitudefrom ";
            }

            if (!string.IsNullOrEmpty(searchParams.Magnitude?.Thru)) {
                query += " AND magnitude <= $magnitudethru ";
            }

            query += " GROUP BY id ";

            if (!string.IsNullOrEmpty(searchParams.ObjectName)) {
                searchParams.ObjectName = "%" + searchParams.ObjectName + "%";
                query += " HAVING aka LIKE $searchobjectname OR group_concat(cataloguenr.catalogue || cataloguenr.designation) LIKE $searchobjectname";
            }

            query += " ORDER BY " + searchParams.SearchOrder.Field + " " + searchParams.SearchOrder.Direction;

            if (searchParams.Limit != null) {
                query += " LIMIT " + searchParams.Limit + ";";
            }

            var dsos = new List<DeepSkyObject>();
            try {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString)) {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand()) {
                        command.CommandText = query;

                        command.Parameters.AddWithValue("$constellation", searchParams.Constellation);
                        command.Parameters.AddWithValue("$rafrom", searchParams.RightAscension?.From);
                        command.Parameters.AddWithValue("$rathru", searchParams.RightAscension?.Thru);
                        command.Parameters.AddWithValue("$decfrom", searchParams.Declination?.From);
                        command.Parameters.AddWithValue("$decthru", searchParams.Declination?.Thru);
                        command.Parameters.AddWithValue("$sizefrom", searchParams.Size?.From);
                        command.Parameters.AddWithValue("$sizethru", searchParams.Size?.Thru);
                        command.Parameters.AddWithValue("$brightnessfrom", searchParams.Brightness?.From);
                        command.Parameters.AddWithValue("$brightnessthru", searchParams.Brightness?.Thru);
                        command.Parameters.AddWithValue("$magnitudefrom", searchParams.Magnitude?.From);
                        command.Parameters.AddWithValue("$magnitudethru", searchParams.Magnitude?.Thru);
                        command.Parameters.AddWithValue("$searchobjectname", searchParams.ObjectName);

                        if (searchParams.DsoTypes?.Count > 0) {
                            for (int i = 0; i < searchParams.DsoTypes.Count; i++) {
                                command.Parameters.AddWithValue("$dsotype" + i.ToString(), searchParams.DsoTypes[i]);
                            }
                        }

                        var reader = await command.ExecuteReaderAsync(token);

                        while (reader.Read()) {
                            var dso = new DeepSkyObject(reader.GetString(0), imageRepository);

                            var coords = new Coordinates(reader.GetDouble(1), reader.GetDouble(2), Epoch.J2000, Coordinates.RAType.Degrees);
                            dso.Coordinates = coords;
                            dso.DSOType = reader.GetString(3);

                            if (!reader.IsDBNull(4)) {
                                dso.Magnitude = reader.GetDouble(4);
                            }

                            if (!reader.IsDBNull(5)) {
                                dso.Size = reader.GetDouble(5);
                            }

                            if (!reader.IsDBNull(6)) {
                                var akas = reader.GetString(6);
                                if (akas != string.Empty) {
                                    foreach (var name in akas.Split(',')) {
                                        dso.AlsoKnownAs.Add(name);
                                    }
                                }
                            }

                            if (!reader.IsDBNull(7)) {
                                dso.Constellation = reader.GetString(7);
                            }

                            if (!reader.IsDBNull(8)) {
                                dso.SurfaceBrightness = reader.GetDouble(8);
                            }

                            dsos.Add(dso);
                        }
                    }
                }
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.Notification.ShowError(ex.Message);
                }
            }

            return dsos;
        }
    }
}