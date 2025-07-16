#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NINA.PlateSolving.Solvers {

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
                Logger.Error("ASTAP - Plate solve failed. No output file found.");
                if (!parameter.DisableNotifications) {
                    Notification.ShowError(Loc.Instance["LblASTAPNoOutputFile"]);
                }
                return result;
            }

            var dict = File.ReadLines(outputFilePath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(new char[] { '=' }, 2, 0))
                .ToDictionary(parts => parts[0], parts => parts[1]);

            dict.TryGetValue("WARNING", out var warning);

            if (!dict.ContainsKey("PLTSOLVD") || dict["PLTSOLVD"] != "T") {
                dict.TryGetValue("ERROR", out var error);
                Logger.Error($"ASTAP - Plate solve failed.{Environment.NewLine}{warning}{Environment.NewLine}{error}");
                if (!parameter.DisableNotifications) {
                    Notification.ShowError(String.Format(Loc.Instance["LblASTAPSolveFailed"], warning, error));
                }
                return result;
            }

            if (!string.IsNullOrWhiteSpace(warning)) {
                Logger.Warning($"ASTAP - {warning}");
                if (!parameter.DisableNotifications) {
                    Notification.ShowExternalWarning($"{warning}", Loc.Instance["LblASTAPWarning"]);
                }
            }

            var wcs = new WorldCoordinateSystem(
                double.Parse(dict["CRVAL1"], CultureInfo.InvariantCulture),
                double.Parse(dict["CRVAL2"], CultureInfo.InvariantCulture),
                double.Parse(dict["CRPIX1"], CultureInfo.InvariantCulture),
                double.Parse(dict["CRPIX2"], CultureInfo.InvariantCulture),
                double.Parse(dict["CD1_1"], CultureInfo.InvariantCulture),
                double.Parse(dict["CD1_2"], CultureInfo.InvariantCulture),
                double.Parse(dict["CD2_1"], CultureInfo.InvariantCulture),
                double.Parse(dict["CD2_2"], CultureInfo.InvariantCulture)
            );

            result.Success = true;
            result.Coordinates = new Coordinates(
                double.Parse(dict["CRVAL1"], CultureInfo.InvariantCulture),
                double.Parse(dict["CRVAL2"], CultureInfo.InvariantCulture),
                Epoch.J2000,
                Coordinates.RAType.Degrees
            );

            // result.PositionAngle = 360 - double.Parse(dict["CROTA2"], CultureInfo.InvariantCulture);

            /*
             * CDELT1 and CDELT2 are obsolete.
             * To calculate pixel scale, we should add the squares of CD1_2 and CD2_2 and take the square root to get degrees.
             */
            if (dict.ContainsKey("CD1_2") && dict.ContainsKey("CD2_2")) {
                double.TryParse(dict["CD1_2"], NumberStyles.Any, CultureInfo.InvariantCulture, out double cr1y);
                double.TryParse(dict["CD2_2"], NumberStyles.Any, CultureInfo.InvariantCulture, out double cr2y);

                result.Pixscale = AstroUtil.DegreeToArcsec(Math.Sqrt(Math.Pow(cr1y, 2) + Math.Pow(cr2y, 2)));
            }
            if(!double.IsNaN(result.Pixscale)) {
                result.Radius = AstroUtil.ArcsecToDegree(Math.Sqrt(Math.Pow(imageProperties.ImageWidth * result.Pixscale, 2) + Math.Pow(imageProperties.ImageHeight * result.Pixscale, 2)) / 2d);
            }            
            result.PositionAngle = 360 - (wcs.Rotation - 180);
            result.Flipped = !wcs.Flipped;

            return result;
        }

        protected override string GetLocalizedPlateSolverName() {
            return Loc.Instance["LblASTAPNotFound"];
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
        }

        protected override string GetOutputPath(string imageFilePath) {
            return Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + ".ini";
        }

        protected override List<string> GetSideCarFilePaths(string imageFilePath) {
            return new List<string>() {
                Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + ".wcs"
            };
        }
    }
}