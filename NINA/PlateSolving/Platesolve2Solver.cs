#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal class Platesolve2Solver : CLISolver {
        private static string imageFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "ps2_tmp.jpg");
        private static string outputFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "ps2_tmp.apm");

        public Platesolve2Solver(string executableLocation) : base(executableLocation, imageFilePath, outputFilePath) {
            this.executableLocation = executableLocation;
        }

        /// <summary>
        /// Gets start arguments for Platesolve2 out of RA,Dec, ArcDegWidth, ArcDegHeight and ImageFilePath
        /// </summary>
        /// <returns></returns>
        protected override string GetArguments(PlateSolveParameter parameter) {
            var args = new string[] {
                    Astrometry.ToRadians(parameter.Coordinates.RADegrees).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(parameter.Coordinates.Dec).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(parameter.FoVW).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(parameter.FoVH).ToString(CultureInfo.InvariantCulture),
                    parameter.Regions.ToString(),
                    imageFilePath,
                    "0"
            };
            return string.Join(",", args);
        }

        /// <summary>
        /// Extract result out of generated .axy file. File consists of three rows
        /// 1. row: RA,Dec,Code
        /// 2. row: Scale,Orientation,?,?,Stars
        /// </summary>
        /// <returns>PlateSolveResult</returns>
        protected override PlateSolveResult ReadResult(PlateSolveParameter parameter) {
            PlateSolveResult result = new PlateSolveResult() { Success = false };
            if (File.Exists(outputFilePath)) {
                using (var s = new StreamReader(outputFilePath)) {
                    string line;
                    int linenr = 0;
                    while ((line = s.ReadLine()) != null) {
                        string[] resultArr = line.Split(',');
                        if (linenr == 0) {
                            if (resultArr.Length > 2) {
                                double ra, dec;
                                int status;
                                if (resultArr.Length == 5) {
                                    /* workaround for when decimal separator is comma instead of point.
                                     won't work when result contains even numbers tho... */
                                    status = int.Parse(resultArr[4]);
                                    if (status != 1) {
                                        /* error */
                                        result.Success = false;
                                        break;
                                    }

                                    ra = double.Parse(resultArr[0] + "." + resultArr[1], CultureInfo.InvariantCulture);
                                    dec = double.Parse(resultArr[2] + "." + resultArr[3], CultureInfo.InvariantCulture);
                                } else {
                                    status = int.Parse(resultArr[2]);
                                    if (status != 1) {
                                        /* error */
                                        result.Success = false;
                                        break;
                                    }

                                    ra = double.Parse(resultArr[0], CultureInfo.InvariantCulture);
                                    dec = double.Parse(resultArr[1], CultureInfo.InvariantCulture);
                                }

                                /* success */
                                result.Success = true;
                                result.Coordinates = new Coordinates(Astrometry.ToDegree(ra), Astrometry.ToDegree(dec), Epoch.J2000, Coordinates.RAType.Degrees);
                            }
                        }
                        if (linenr == 1) {
                            if (resultArr.Length > 2) {
                                if (resultArr.Length > 5) {
                                    /* workaround for when decimal separator is comma instead of point.
                                     won't work when result contains even numbers tho... */
                                    result.Pixscale = double.Parse(resultArr[0] + "." + resultArr[1], CultureInfo.InvariantCulture);
                                    result.Orientation = double.Parse(resultArr[2] + "." + resultArr[3], CultureInfo.InvariantCulture);
                                } else {
                                    result.Pixscale = double.Parse(resultArr[0], CultureInfo.InvariantCulture);
                                    result.Orientation = double.Parse(resultArr[1], CultureInfo.InvariantCulture);
                                }
                            }
                        }
                        linenr++;
                    }
                }
            }
            return result;
        }

        public override async Task<PlateSolveResult> SolveAsync(PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var result = new PlateSolveResult() { Success = false };
            try {
                result = await this.Solve(parameter, progress, ct);
            } catch (FileNotFoundException ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblPlatesolve2NotFound"] + Environment.NewLine + executableLocation);
            }
            return result;
        }
    }
}