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

using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NINA.PlateSolving {

    internal class ASTAPSolver : CLISolver {

        internal class ASTAPValidationFailedException : Exception {

            internal ASTAPValidationFailedException(string reason) : base($"ASTAP validation failed: {reason}") {
            }
        }

        public ASTAPSolver(string executableLocation)
            : base(executableLocation) {
        }

        protected override PlateSolveResult ReadResult(
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            var result = new PlateSolveResult() { Success = false };
            if (!File.Exists(outputFilePath)) {
                Notification.ShowError("ASTAP - Plate solve failed. No output file found.");
                return result;
            }

            var dict = File.ReadLines(outputFilePath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(new char[] { '=' }, 2, 0))
                .ToDictionary(parts => parts[0], parts => parts[1]);

            dict.TryGetValue("WARNING", out var warning);

            if (!dict.ContainsKey("PLTSOLVD") || dict["PLTSOLVD"] != "T") {
                dict.TryGetValue("ERROR", out var error);
                Notification.ShowError($"ASTAP - Plate solve failed.{Environment.NewLine}{warning}{Environment.NewLine}{error}");
                return result;
            }

            if (!string.IsNullOrWhiteSpace(warning)) {
                Notification.ShowWarning($"ASTAP - {warning}");
            }

            result.Success = true;
            result.Coordinates = new Coordinates(
                double.Parse(dict["CRVAL1"], CultureInfo.InvariantCulture),
                double.Parse(dict["CRVAL2"], CultureInfo.InvariantCulture),
                Epoch.J2000,
                Coordinates.RAType.Degrees
            );
            result.Orientation = double.Parse(dict["CROTA2"], CultureInfo.InvariantCulture);
            /* Due to the way N.I.N.A. writes FITS files, the orientation is mirrored on the x-axis */
            result.Orientation = 180 - result.Orientation + 360;
            result.Pixscale = imageProperties.ArcSecPerPixel;
            return result;
        }

        protected override string GetLocalizedPlateSolverName() {
            return Locale.Loc.Instance["LblASTAPNotFound"];
        }

        /// <summary>
        /// Creates the arguments to launch ASTAP process
        /// </summary>
        /// <returns></returns>
        /// <remarks>http://www.hnsky.org/astap.htm#astap_command_line</remarks>
        protected override string GetArguments(
            string imageFilePath,
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            var args = new List<string>();

            //File location to solve
            args.Add($"-f \"{imageFilePath}\"");

            //Field height of image
            var fov = Math.Round(imageProperties.FoVH, 6);
            args.Add($"-fov {fov.ToString(CultureInfo.InvariantCulture)}");

            //Downsample factor
            args.Add($"-z {parameter.DownSampleFactor}");

            //Max number of stars
            args.Add($"-s {parameter.MaxObjects}");

            if (parameter.SearchRadius > 0 && parameter.Coordinates != null) {
                //Search field radius
                args.Add($"-r {parameter.SearchRadius}");

                var ra = Math.Round(parameter.Coordinates.RA, 6);
                //Right Ascension in degrees
                args.Add($"-ra {ra.ToString(CultureInfo.InvariantCulture)}");

                var spd = Math.Round(parameter.Coordinates.Dec + 90.0, 6);
                //South pole distance in degrees
                args.Add($"-spd {spd.ToString(CultureInfo.InvariantCulture)}");
            } else {
                //Search field radius
                args.Add($"-r {180}");
            }

            return string.Join(" ", args);
        }

        protected override void EnsureSolverValid(PlateSolveParameter parameter) {
            if (string.IsNullOrWhiteSpace(this.executableLocation)) {
                throw new ASTAPValidationFailedException($"ASTAP executable location missing! Please enter the location in the platesolver options!");
            }
            if (!File.Exists(this.executableLocation)) {
                throw new ASTAPValidationFailedException($"ASTAP executable not found at {this.executableLocation}");
            }
            var astapVersionInfo = FileVersionInfo.GetVersionInfo(this.executableLocation);
            if (astapVersionInfo.FileVersion == null) {
                // Version below 0.9.1.0
                // Only allows downsample in the range of 1 to 4
                if (parameter.DownSampleFactor == 0) {
                    throw new ASTAPValidationFailedException($"ASTAP version below 0.9.1.0 does not allow auto downsample factor value of 0! Please update your ASTAP version!");
                }
            }
            string astapPath = Path.GetDirectoryName(this.executableLocation);
            string[] g17Files = Directory.GetFiles(astapPath, "g17_*");
            if (g17Files.Length == 0) {
                throw new ASTAPValidationFailedException($"g17 database not found in {astapPath}");
            }
        }

        protected override string GetOutputPath(string imageFilePath) {
            return Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + ".ini";
        }
    }
}