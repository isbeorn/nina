#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using Accord;
using NINA.Database;
using NINA.Database.Schema;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Astrometry {

    internal class EarthRotationParameterUpdater {

        private async Task<DateTime> UpdateEarthRotationParameters(DateTime startDate) {
            var maxUnix = 0l;
            using (MyStopWatch.Measure()) {
                var startDateUnix = Utility.DateTimeToUnixTimeStamp(startDate);
                var data = QueryOnlineData();

                List<string> rows = new List<string>();
                using (var context = new DatabaseInteraction().GetContext()) {
                    using (var reader = new System.IO.StringReader(data)) {
                        string headerLine = reader.ReadLine();
                        string[] headerColumns = headerLine.Split(';');

                        var idxMJD = Array.FindIndex(headerColumns, x => x.ToLower() == "mjd");
                        var idxYear = Array.FindIndex(headerColumns, x => x.ToLower() == "year");
                        var idxMonth = Array.FindIndex(headerColumns, x => x.ToLower() == "month");
                        var idxDay = Array.FindIndex(headerColumns, x => x.ToLower() == "day");
                        var idxXPole = Array.FindIndex(headerColumns, x => x.ToLower() == "x_pole");
                        var idxYPole = Array.FindIndex(headerColumns, x => x.ToLower() == "y_pole");
                        var idxUT1_UTC = Array.FindIndex(headerColumns, x => x.ToLower() == "ut1-utc");
                        var idxLOD = Array.FindIndex(headerColumns, x => x.ToLower() == "lod");
                        var idxdX = Array.FindIndex(headerColumns, x => x.ToLower() == "dx");
                        var idxdY = Array.FindIndex(headerColumns, x => x.ToLower() == "dy");

                        string line;
                        while ((line = reader.ReadLine()) != null) {
                            var columns = line.Split(';');

                            //When column 5 is empty there is no prediction available
                            if (!string.IsNullOrWhiteSpace(columns[idxXPole])) {
                                int year = int.Parse(columns[idxYear]);
                                int month = int.Parse(columns[idxMonth]);
                                int day = int.Parse(columns[idxDay]);

                                var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                                var unixTimestamp = Utility.DateTimeToUnixTimeStamp(date);

                                if (unixTimestamp >= startDateUnix) {
                                    double mjd = double.Parse(columns[idxMJD], CultureInfo.InvariantCulture);

                                    double x = double.Parse(columns[idxXPole], CultureInfo.InvariantCulture);
                                    double y = double.Parse(columns[idxYPole], CultureInfo.InvariantCulture);

                                    double ut1_utc = double.Parse(columns[idxUT1_UTC], CultureInfo.InvariantCulture);

                                    double LOD = 0.0;
                                    if (!string.IsNullOrWhiteSpace(columns[idxLOD])) {
                                        LOD = double.Parse(columns[idxLOD], CultureInfo.InvariantCulture);
                                    } else {
                                        maxUnix = Math.Max(maxUnix, unixTimestamp);
                                    }

                                    double dX = 0.0;
                                    double dY = 0.0;
                                    if (!string.IsNullOrWhiteSpace(columns[idxdX])) {
                                        dX = double.Parse(columns[idxdX], CultureInfo.InvariantCulture) / 1000d;
                                        dY = double.Parse(columns[idxdY], CultureInfo.InvariantCulture) / 1000d;
                                    }

                                    //(date,modifiedjuliandate,x,y,ut1_utc,lod,dx,dy)
                                    rows.Add($"({unixTimestamp.ToString(CultureInfo.InvariantCulture)},{mjd.ToString(CultureInfo.InvariantCulture)},{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)},{ut1_utc.ToString(CultureInfo.InvariantCulture)},{LOD.ToString(CultureInfo.InvariantCulture)},{dX.ToString(CultureInfo.InvariantCulture)},{dY.ToString(CultureInfo.InvariantCulture)})");
                                }
                            }
                        }
                    }
                    //Bulk Query to insert all rows quickly
                    var query = $"INSERT OR REPLACE INTO `earthrotationparameters` (date,modifiedjuliandate,x,y,ut1_utc,lod,dx,dy) VALUES {string.Join($",{Environment.NewLine}", rows)}";
                    await context.Database.ExecuteSqlCommandAsync(query);
                }
            }
            return Utility.UnixTimeStampToDateTime(maxUnix);
        }

        private string QueryOnlineData() {
            var webClient = new WebClient();
            webClient.Headers.Add("User-Agent", "N.I.N.A. Data Import");
            webClient.Headers.Add("Accept", "*/*");
            webClient.Headers.Add("Cache-Control", "no-cache");
            webClient.Headers.Add("Host", "datacenter.iers.org");
            webClient.Headers.Add("accept-encoding", "gzip,deflate");

            return webClient.DownloadString("https://datacenter.iers.org/data/csv/finals2000A.daily.csv");
        }

        public Task Update() {
            return Task.Run(async () => {
                try {
                    var lastAvailableDate = await GetLastAvailableEarthRotationParameterDate();

                    if ((DateTime.Now - lastAvailableDate) > TimeSpan.FromDays(10)) {
                        if (NetworkInterface.GetIsNetworkAvailable()) {
                            Logger.Info($"Updating EarthRotationParameters due to being outdated. Last available parameter date {lastAvailableDate:yyyy-MM-dd}");
                            var newLastAvailableDate = await UpdateEarthRotationParameters(lastAvailableDate);
                            Logger.Info($"Updated EarthRotationParameters. New last available parameter date {newLastAvailableDate:yyyy-MM-dd}");
                        } else {
                            Logger.Info($"EarthRotationParameters is outdated but no network available to update. Last available parameter date {lastAvailableDate:yyyy-MM-dd}");
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error("Updating EarthRotationParameters failed", ex);
                }
            });
        }

        public async Task<DateTime> GetLastAvailableEarthRotationParameterDate() {
            long availableDataTimeStamp = 0;
            using (var context = new DatabaseInteraction().GetContext()) {
                availableDataTimeStamp = (await context.EarthRotationParameterSet.Where(x => x.lod != 0).OrderByDescending(x => x.date).FirstAsync()).date;
            }
            return Utility.UnixTimeStampToDateTime(availableDataTimeStamp);
        }
    }
}