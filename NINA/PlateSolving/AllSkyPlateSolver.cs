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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NINA.PlateSolving {
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