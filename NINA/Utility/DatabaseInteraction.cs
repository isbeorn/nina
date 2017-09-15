using NINA.Model;
using NINA.Utility.Astrometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility {
    class DatabaseInteraction {
        private string _connectionString;

        public DatabaseInteraction() {
            _connectionString = string.Format(@"Data Source={0};foreign keys=true;",Settings.DatabaseLocation);
        }       

        public async Task<ICollection<string>> GetConstellations(CancellationToken token) {
            const string query = "SELECT DISTINCT(constellation) FROM dsodetail;";
            var constellations = new List<string>() { string.Empty };

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

            return constellations;
        }

        public async Task<ICollection<string>> GetObjectTypes(CancellationToken token) {
            const string query = "SELECT DISTINCT(dsotype) FROM dsodetail;";
            var dsotypes = new List<string>() { string.Empty };

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

            return dsotypes;
        }

        public async Task<AsyncObservableCollection<DeepSkyObject>> GetDeepSkyObjects(
            CancellationToken token,
            string constellation = "",
            double? rafrom = null,
            double? rathru = null,
            double? decfrom = null,
            double? decthru = null,
            string sizefrom = null,
            string sizethru = null,
            IList<string> dsotypes = null,
            string brightnessfrom = null,
            string brightnessthru = null,
            string magnitudefrom = null,
            string magnitudethru = null) {

            string query = "SELECT id, ra, dec, dsotype FROM dsodetail WHERE (1=1) ";

            if(constellation != null && constellation != string.Empty) {
                query += " AND constellation = $constellation ";
            }

            if(rafrom != null) {
                query += " AND ra > $rafrom ";
            }

            if (rathru != null) {
                query += " AND ra < $rathru ";
            }

            if (decfrom != null) {
                query += " AND dec > $decfrom ";
            }

            if (decthru != null) {
                query += " AND dec < $decthru ";
            }

            if (sizefrom != null && sizefrom != string.Empty) {
                query += " AND sizemin > $sizefrom ";
            }

            if (sizethru != null && sizethru != string.Empty) {
                query += " AND sizemax < $sizethru ";
            }

            if(dsotypes != null && dsotypes.Count > 0) {
                query += " AND dsotype IN (";
                for (int i = 0; i < dsotypes.Count; i++) {
                    query += "$dsotype" + i.ToString() + ",";
                }
                query = query.Remove(query.Length - 1);
                query += ") ";
            }

            if (brightnessfrom != null && brightnessfrom != string.Empty) {
                query += " AND surfacebrightness > $brightnessfrom ";
            }

            if (brightnessthru != null && brightnessthru != string.Empty) {
                query += " AND surfacebrightness < $brightnessthru ";
            }

            if (magnitudefrom != null && magnitudefrom != string.Empty) {
                query += " AND magnitude > $magnitudefrom ";
            }

            if (magnitudethru != null && magnitudethru != string.Empty) {
                query += " AND magnitude < $magnitudethru ";
            }

            query += " ORDER BY Id asc;";

            var dsos = new AsyncObservableCollection<DeepSkyObject>() ;

            using (SQLiteConnection connection = new SQLiteConnection(_connectionString)) {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand()) {
                    command.CommandText = query;

                    command.Parameters.AddWithValue("$constellation",constellation);
                    command.Parameters.AddWithValue("$rafrom",rafrom);
                    command.Parameters.AddWithValue("$rathru",rathru);
                    command.Parameters.AddWithValue("$decfrom",decfrom);
                    command.Parameters.AddWithValue("$decthru",decthru);
                    command.Parameters.AddWithValue("$sizefrom",sizefrom);
                    command.Parameters.AddWithValue("$sizethru",sizethru);
                    command.Parameters.AddWithValue("$brightnessfrom",brightnessfrom);
                    command.Parameters.AddWithValue("$brightnessthru",brightnessthru);
                    command.Parameters.AddWithValue("$magnitudefrom",magnitudefrom);
                    command.Parameters.AddWithValue("$magnitudethru",magnitudethru);

                    if (dsotypes != null && dsotypes.Count > 0) {
                        for (int i = 0;i < dsotypes.Count;i++) {
                            command.Parameters.AddWithValue("$dsotype" + i.ToString(),dsotypes[i]);
                        }
                    }

                    var reader = await command.ExecuteReaderAsync(token);


                    while (reader.Read()) {

                        var dso = new DeepSkyObject(reader.GetString(0));

                        var coords = new Coordinates(reader.GetDouble(1),reader.GetDouble(2),Epoch.J2000,Coordinates.RAType.Degrees);
                        dso.Coordinates = coords;
                        dso.DSOType = reader.GetString(3);
                        dsos.Add(dso);
                    }
                }
            }

            return dsos;
        }

    }
}
