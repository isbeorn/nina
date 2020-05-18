#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using System.Globalization;
using System.IO;

namespace NINA.PlateSolving.Solvers {

    internal class Platesolve2Solver : CLISolver {

        public Platesolve2Solver(string executableLocation)
            : base(executableLocation) {
            this.executableLocation = executableLocation;
        }

        /// <summary>
        /// Gets start arguments for Platesolve2 out of RA,Dec, ArcDegWidth, ArcDegHeight and ImageFilePath
        /// </summary>
        /// <returns></returns>
        protected override string GetArguments(
            string imageFilePath,
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            var args = new string[] {
                    Astrometry.ToRadians(parameter.Coordinates.RADegrees).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(parameter.Coordinates.Dec).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(imageProperties.FoVW).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(imageProperties.FoVH).ToString(CultureInfo.InvariantCulture),
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
        protected override PlateSolveResult ReadResult(
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
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

        protected override string GetLocalizedPlateSolverName() {
            return Locale.Loc.Instance["LblPlatesolve2NotFound"];
        }

        protected override string GetOutputPath(string imageFilePath) {
            return Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + ".apm";
        }
    }
}
