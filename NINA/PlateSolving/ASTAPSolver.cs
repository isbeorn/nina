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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Extensions;
using NINA.Utility.Notification;

namespace NINA.PlateSolving {

    internal class ASTAPSolver : CLISolver {
        private static string imageFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "astap_tmp.jpg");
        private static string outputFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "astap_tmp.ini");

        public ASTAPSolver(string executableLocation) : base(executableLocation, imageFilePath, outputFilePath) {
        }

        protected override PlateSolveResult ReadResult(PlateSolveParameter parameter) {
            var result = new PlateSolveResult() { Success = false };
            if (File.Exists(outputFilePath)) {
                var dict = File.ReadLines(outputFilePath)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(line => line.Split(new char[] { '=' }, 2, 0))
                   .ToDictionary(parts => parts[0], parts => parts[1]);
                if (dict.ContainsKey("PLTSOLVD")) {
                    result.Success = dict["PLTSOLVD"] == "T" ? true : false;

                    if (result.Success) {
                        result.Coordinates = new Coordinates(
                            double.Parse(dict["CRVAL1"], CultureInfo.InvariantCulture),
                            double.Parse(dict["CRVAL2"], CultureInfo.InvariantCulture),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees
                        );
                        result.Orientation = double.Parse(dict["CROTA2"], CultureInfo.InvariantCulture);
                        result.Pixscale = parameter.ArcSecPerPixel;
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
                Notification.ShowError(Locale.Loc.Instance["LblASTAPNotFound"] + Environment.NewLine + executableLocation);
            }
            return result;
        }

        /// <summary>
        /// Creates the arguments to launche ASTAP process
        /// </summary>
        /// <returns></returns>
        /// <remarks>http://www.hnsky.org/astap.htm#astap_command_line</remarks>
        protected override string GetArguments(PlateSolveParameter parameter) {
            var args = new List<string>();

            //File location to solve
            args.Add($"-f \"{imageFilePath}\"");

            //Field height of image
            args.Add($"-fov {parameter.FoVH.ToString(CultureInfo.InvariantCulture)}");

            //Downsample factor
            args.Add($"-z {parameter.DownSampleFactor}");

            //Max number of stars
            args.Add($"-s {parameter.MaxObjects}");

            if (parameter.SearchRadius > 0 && parameter.Coordinates != null) {
                //Search field radius
                args.Add($"-r {parameter.SearchRadius}");

                //Right Ascension in degrees
                args.Add($"-ra {parameter.Coordinates.RA.ToString(CultureInfo.InvariantCulture)}");

                //Declination in degrees
                args.Add($"-dec {parameter.Coordinates.Dec.ToString(CultureInfo.InvariantCulture)}");
            }

            return string.Join(" ", args);
        }
    }
}