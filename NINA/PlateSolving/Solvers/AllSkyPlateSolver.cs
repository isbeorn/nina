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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NINA.PlateSolving.Solvers {

    internal class AllSkyPlateSolver : CLISolver {

        public AllSkyPlateSolver(string executableLocation)
            : base(executableLocation) {
        }

        protected override string GetLocalizedPlateSolverName() {
            return Locale.Loc.Instance["LblASPSNotFound"];
        }

        protected override PlateSolveResult ReadResult(
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            var result = new PlateSolveResult() { Success = false };
            if (File.Exists(outputFilePath)) {
                string[] lines = File.ReadAllLines(outputFilePath, Encoding.UTF8);
                if (lines.Length > 0) {
                    if (lines[0] == "OK" && lines.Length >= 8) {
                        var ra = double.Parse(lines[1]);
                        var dec = double.Parse(lines[2]);

                        result.Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);

                        var fovW = lines[3];
                        var fovH = lines[4];

                        result.Pixscale = double.Parse(lines[5]);
                        result.Orientation = double.Parse(lines[6]);
                        /* Due to the way N.I.N.A. writes FITS files, the orientation is mirrored on the x-axis */
                        result.Orientation = 180 - result.Orientation + 360;

                        var focalLength = lines[7];

                        result.Success = true;
                    }
                }
            }
            return result;
        }

        protected override string GetArguments(
            string imageFilePath,
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            var args = new List<string>();

            var imageFilePathArg = imageFilePath.Replace("\\", "/");
            //FileName
            args.Add($"\"{imageFilePathArg}\"");

            //OutFile
            args.Add($"\"{outputFilePath}\"");

            //FocalLength
            args.Add(parameter.FocalLength.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            //PixelSize
            args.Add(parameter.PixelSize.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            if (parameter.Coordinates != null) {
                //CurrentRA
                args.Add(parameter.Coordinates.RADegrees.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

                //CurrentDec
                args.Add(parameter.Coordinates.Dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            } else {
                args.Add("0");
                args.Add("0");
            }

            //NearRadius
            args.Add(parameter.SearchRadius.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            return $"/solvefile {string.Join(" ", args)}";
        }

        protected override string GetOutputPath(string imageFilePath) {
            return Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + ".txt";
        }
    }
}
