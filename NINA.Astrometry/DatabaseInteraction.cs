#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility.Extensions;
using System.Text.RegularExpressions;
using NINA.Core.Model;
using NINA.Core.Database;
using System.Windows.Media.Imaging;

namespace NINA.Astrometry {

    public class DatabaseInteraction {

        static DatabaseInteraction() {
        }

        private string connectionString;

        public DatabaseInteraction()
            : this(string.Format(@"Data Source={0};", Environment.ExpandEnvironmentVariables(@"%localappdata%\NINA\NINA.sqlite"))) {
        }

        public DatabaseInteraction(string connectionString) {
            this.connectionString = connectionString;
        }

        public NINADbContext GetContext() {
            return new NINADbContext(connectionString);
        }

        public async Task<ICollection<string>> GetConstellations(CancellationToken token) {
            try {
                using (var context = new NINADbContext(connectionString)) {
                    return await context.DsoDetailSet.Select(x => x.constellation).Distinct().ToListAsync(token);
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
            return new List<string>();
        }

        public async Task<ICollection<string>> GetObjectTypes(CancellationToken token) {
            try {
                using (var context = new NINADbContext(connectionString)) {
                    return await context.DsoDetailSet.Select(x => x.dsotype).Distinct().ToListAsync(token);
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
            return new List<string>();
        }

        public async Task<double> GetUT1_UTC(DateTime date, CancellationToken token) {
            var unixTimestamp = CoreUtil.DateTimeToUnixTimeStamp(date);
            try {
                using (var context = new NINADbContext(connectionString)) {
                    var rows = await context.EarthRotationParameterSet.OrderBy(x => Math.Abs(x.date - unixTimestamp)).Take(1).ToListAsync(token);
                    return rows.First().ut1_utc;
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                }
            }
            return double.NaN;
        }

        public async Task<List<FocusTarget>> GetBrightStars() {
            var brightStars = new List<FocusTarget>();
            try {
                using (var context = new NINADbContext(connectionString)) {
                    var rows = await context.BrightStarsSet.ToListAsync();

                    foreach (var row in rows) {
                        var brightStar = new FocusTarget(row.name);
                        var coords = new Coordinates(row.ra, row.dec, Epoch.J2000, Coordinates.RAType.Degrees);
                        brightStar.Coordinates = coords;
                        brightStar.Magnitude = row.magnitude;
                        brightStars.Add(brightStar);
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
            return brightStars;
        }

        public class DeepSkyObjectSearchParams {
            public string Constellation { get; set; } = "";
            public IList<string> DsoTypes { get; set; }
            public DeepSkyObjectSearchFromThru<double?> RightAscension { get; set; } = new DeepSkyObjectSearchFromThru<double?>();
            public DeepSkyObjectSearchFromThru<double?> Declination { get; set; } = new DeepSkyObjectSearchFromThru<double?>();
            public DeepSkyObjectSearchFromThru<double?> Brightness { get; set; } = new DeepSkyObjectSearchFromThru<double?>();
            public DeepSkyObjectSearchFromThru<double?> Size { get; set; } = new DeepSkyObjectSearchFromThru<double?>();
            public DeepSkyObjectSearchFromThru<double?> Magnitude { get; set; } = new DeepSkyObjectSearchFromThru<double?>();
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

        public async Task<List<Constellation>> GetConstellationsWithStars(CancellationToken token) {
            var starList = new List<Star>();
            var constellations = new List<Constellation>();
            try {
                using (var context = new NINADbContext(connectionString)) {
                    var starlist = await context.ConstellationStarSet.ToListAsync(token);
                    starList = starlist.Select(x => new Star(x.id, x.name, new Coordinates(x.ra, x.dec, Epoch.J2000, Coordinates.RAType.Degrees), x.mag)).ToList();

                    var rows = await context.ConstellationSet.OrderBy(x => x.constellationid).ToListAsync(token);

                    foreach (var row in rows) {
                        var constId = row.constellationid;
                        var constellation = constellations.SingleOrDefault(c => c.Id == constId);
                        if (constellation == null) {
                            constellation = new Constellation(constId);
                            constellations.Add(constellation);
                        }
                        Star star1 = starList.First(s => s.Id == row.starid);
                        Star star2 = starList.First(s => s.Id == row.followstarid);

                        constellation.StarConnections.Add(new Tuple<Star, Star>(star1, star2));
                    }

                    // make a list of unique stars
                    foreach (var constellation in constellations) {
                        constellation.Stars = new List<Star>(
                            constellation.StarConnections
                                .Select(t => t.Item1)
                                .Concat(constellation.StarConnections.Select(t => t.Item2))
                                .GroupBy(b => b.Name)
                                .Select(b => b.First())
                                .ToList()
                        );
                        bool goesOver0 = false;
                        foreach (var pair in constellation.StarConnections) {
                            goesOver0 = Math.Max(pair.Item1.Coords.RADegrees, pair.Item2.Coords.RADegrees) -
                                        Math.Min(pair.Item1.Coords.RADegrees, pair.Item2.Coords.RADegrees) > 180;
                            if (goesOver0) {
                                break;
                            }
                        }

                        constellation.GoesOverRaZero = goesOver0;
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }

            return constellations;
        }

        public async Task<List<ConstellationBoundary>> GetConstellationBoundaries(CancellationToken token) {
            var constellationBoundaries = new List<ConstellationBoundary>();
            try {
                using (var context = new NINADbContext(connectionString)) {
                    var rows = await context.ConstellationBoundariesSet.OrderBy(x => x.constellation).ThenBy(x => x.position).ToListAsync(token);

                    ConstellationBoundary boundary = null;
                    var prevName = string.Empty;
                    foreach (var row in rows) {
                        var name = row.constellation;
                        if (prevName != name) {
                            prevName = name;
                            boundary = new ConstellationBoundary();
                            constellationBoundaries.Add(boundary);
                            boundary.Name = name;
                        }
                        boundary.Boundaries.Add(new Coordinates(row.ra, row.dec, Epoch.J2000, Coordinates.RAType.Hours));
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
            return constellationBoundaries;
        }

        public async Task<List<string>> GetCatalogues(int minCount, CancellationToken token) {
            using var sw = MyStopWatch.Measure();

            try {
                using var context = new NINADbContext(connectionString);

                var query = from catalogeNrs in context.CatalogueNrSet
                            where catalogeNrs.catalogue != "NAME"
                            group catalogeNrs by catalogeNrs.catalogue into groupedCatalogueNrs
                            where groupedCatalogueNrs.Count() >= minCount
                            orderby groupedCatalogueNrs.Count() descending
                            select groupedCatalogueNrs.Key;

                return await query.ToListAsync();
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
            return null;
        }

        public Task<List<DeepSkyObject>> GetDeepSkyObjects(
            string imageRepository,
            CustomHorizon horizon,
            DeepSkyObjectSearchParams searchParams,
            CancellationToken token) {
            return GetDeepSkyObjects(null as Func<SkyObjectBase, Task<BitmapSource>>, horizon, searchParams, token);
        }

        public async Task<List<DeepSkyObject>> GetDeepSkyObjects(
            Func<SkyObjectBase, Task<BitmapSource>> imageFactory,
            CustomHorizon horizon,
            DeepSkyObjectSearchParams searchParams,
            CancellationToken token) {
            using (MyStopWatch.Measure()) {
                if (searchParams == null) { throw new ArgumentNullException(nameof(searchParams)); }

                var dsos = new List<DeepSkyObject>();
                try {
                    using (var context = new NINADbContext(connectionString)) {
                        var query = from dso in context.DsoDetailSet
                                    select new {
                                        dso.id,
                                        dso.ra,
                                        dso.dec,
                                        dso.dsotype,
                                        dso.magnitude,
                                        dso.sizemin,
                                        dso.sizemax,
                                        dso.constellation,
                                        dso.surfacebrightness,
                                        dso.positionangle
                                    };

                        if (!string.IsNullOrEmpty(searchParams.Constellation)) {
                            query = query.Where(x => x.constellation.ToLower() == searchParams.Constellation.ToLower());
                        }

                        if (searchParams.RightAscension.From != null && (searchParams.RightAscension.Thru == null || searchParams.RightAscension.Thru > searchParams.RightAscension.From)) {
                            query = query.Where(x => x.ra >= searchParams.RightAscension.From);
                        }

                        if (searchParams.RightAscension.Thru != null && (searchParams.RightAscension.From == null || searchParams.RightAscension.Thru > searchParams.RightAscension.From)) {
                            query = query.Where(x => x.ra <= searchParams.RightAscension.Thru);
                        }

                        if (searchParams.RightAscension.From != null && searchParams.RightAscension.Thru != null && searchParams.RightAscension.Thru < searchParams.RightAscension.From) {
                            query = query.Where(x => x.ra >= searchParams.RightAscension.From || x.ra <= searchParams.RightAscension.Thru);
                        }

                        if (searchParams.Declination.From != null) {
                            query = query.Where(x => x.dec >= searchParams.Declination.From);
                        }

                        if (searchParams.Declination.Thru != null) {
                            query = query.Where(x => x.dec <= searchParams.Declination.Thru);
                        }

                        if (searchParams.Size.From.HasValue) {
                            query = query.Where(x => x.sizemax >= searchParams.Size.From);
                        }

                        if (searchParams.Size.Thru.HasValue) {
                            query = query.Where(x => x.sizemax <= searchParams.Size.Thru);
                        }

                        if (searchParams.Brightness.From.HasValue) {
                            query = query.Where(x => x.surfacebrightness >= searchParams.Brightness.From);
                        }

                        if (searchParams.Brightness.Thru.HasValue) {
                            query = query.Where(x => x.surfacebrightness <= searchParams.Brightness.Thru);
                        }

                        if (searchParams.Magnitude.From.HasValue) {
                            query = query.Where(x => x.magnitude >= searchParams.Magnitude.From);
                        }

                        if (searchParams.Magnitude.Thru.HasValue) {
                            query = query.Where(x => x.magnitude <= searchParams.Magnitude.Thru);
                        }

                        if (searchParams.DsoTypes?.Count > 0) {
                            query = query.Where(x => searchParams.DsoTypes.Contains(x.dsotype));
                        }

                        if (!string.IsNullOrEmpty(searchParams.ObjectName)) {
                            var name = searchParams.ObjectName.Trim().ToLower();
                            var idQuery = context.CatalogueNrSet.Where(x => x.dsodetailid.ToLower().Contains(name) || (x.catalogue + x.designation).ToLower().Contains(name) || (x.catalogue + " " + x.designation).ToLower().Contains(name)).Select(x => x.dsodetailid).Distinct();

                            query = query.Join(idQuery, dsoDetail => dsoDetail.id, e => e, (dsoDetail, id) => dsoDetail);
                        }

                        if (searchParams.SearchOrder.Direction == "ASC") {
                            query = query.OrderBy(searchParams.SearchOrder.Field);
                        } else {
                            query = query.OrderByDescending(searchParams.SearchOrder.Field);
                        }

                        if (searchParams.Limit != null) {
                            query = query.Take(searchParams.Limit.Value);
                        }

                        var dsosTask = query.ToListAsync(token);

                        var catalogueTask = (from q in query
                                             join cat in context.CatalogueNrSet on q.id equals cat.dsodetailid
                                             select new { cat.dsodetailid, designation = cat.catalogue == "NAME" ? cat.designation : cat.catalogue + " " + cat.designation })
                                             .GroupBy(x => x.dsodetailid)
                                             .ToDictionaryAsync(x => x.Key, x => x.ToList(), token);

                        await Task.WhenAll(dsosTask, catalogueTask);

                        var dsoResult = dsosTask.Result;
                        var catalogueResult = catalogueTask.Result;

                        foreach (var row in dsoResult) {
                            var id = row.id;
                            var coords = new Coordinates(row.ra, row.dec, Epoch.J2000, Coordinates.RAType.Degrees);
                            var dso = new DeepSkyObject(row.id, coords, imageFactory, horizon);

                            dso.DSOType = row.dsotype;

                            if (row.magnitude.HasValue) {
                                dso.Magnitude = row.magnitude;
                            }

                            if (row.sizemax.HasValue) {
                                dso.Size = row.sizemax;
                            }

                            if(row.sizemin.HasValue) {
                                dso.SizeMin = row.sizemin;
                            }

                            if(row.positionangle.HasValue) {
                                dso.PositionAngle = Angle.ByDegree(row.positionangle.Value);
                            }

                            dso.AlsoKnownAs = catalogueResult[row.id].Select(x => x.designation).ToList();

                            dso.Name = GetDisplayAlias(searchParams.ObjectName, dso.AlsoKnownAs);

                            if (!string.IsNullOrEmpty(row.constellation)) {
                                dso.Constellation = row.constellation;
                            }

                            if (row.surfacebrightness.HasValue) {
                                dso.SurfaceBrightness = row.surfacebrightness;
                            }

                            dsos.Add(dso);
                        }
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    if (!ex.Message.Contains("Execution was aborted by the user")) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    }
                }

                return dsos;
            }
        }

        /// <summary>
        /// Display alias is determined in the following order:
        ///   1) No value provided, use the longest alias
        ///   2) Use the alias that starts with the same token as our input, if multiple match, use longest
        ///   3) Use the alias that has the smallest levenshtein distance, length breaks ties, longest wins.
        /// </summary>
        /// <param name="searchName">Token searched for</param>
        /// <param name="aliases">List of alternate names for a dso</param>
        /// <returns></returns>
        public string GetDisplayAlias(string searchName, List<string> aliases) {
            // No search by name, default to longest
            if (string.IsNullOrEmpty(searchName)) {
                return aliases.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur);
            }

            string cleanedSearchName = cleanForSearching(searchName);

            var cleanedAliases = aliases
                .Select(name => new {
                    name,
                    cleanName = cleanForSearching(name)
                })
                .OrderByDescending(alias => alias.cleanName.Length)
                .ToList();

            // Do any of them start with what we're typing? If so, go with the longest
            string result = cleanedAliases
                .Where(alias => alias.cleanName.StartsWith(cleanedSearchName))
                .Select(alias => alias.name)
                .FirstOrDefault();

            if (null != result) {
                return result;
            }

            // None of them start with it, so let's use levenshtein distance, length breaks ties
            return cleanedAliases
                .OrderBy(alias => Fastenshtein.Levenshtein.Distance(cleanedSearchName, alias.cleanName))
                .ThenByDescending(alias => alias.cleanName.Length)
                .Select(alias => alias.name)
                .FirstOrDefault();
        }

        /// <summary>
        /// Removes non-word characters from a search string, excluding dashes. It leaves dashes in.
        /// </summary>
        /// <param name="searchString">Search string to clean</param>
        /// <returns>Cleaned search string, safe to use to compare</returns>
        private string cleanForSearching(string token) {
            return Regex.Replace(token, @"[^\w\-]*", "", RegexOptions.Multiline).ToUpper();
        }
    }
}