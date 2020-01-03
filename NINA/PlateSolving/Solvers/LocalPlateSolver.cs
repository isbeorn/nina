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

using NINA.Utility.Astrometry;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NINA.PlateSolving.Solvers {

    internal class LocalPlateSolver : CLISolver {
        private string bashLocation;

        public LocalPlateSolver(string cygwinRoot)
            : base("cmd.exe") {
            this.bashLocation = Path.GetFullPath(Path.Combine(cygwinRoot, "bin", "bash.exe"));
        }

        protected override string GetArguments(
            string imageFilePath,
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            List<string> options = new List<string>();

            options.Add("--overwrite");
            options.Add("--index-xyls none");
            options.Add("--corr none");
            options.Add("--rdls none");
            options.Add("--match none");
            options.Add("--new-fits none");
            //options.Add("-C cancel--crpix");
            options.Add("-center");
            options.Add($"--objs {parameter.MaxObjects}");
            options.Add("--no-plots");
            options.Add("--resort");
            options.Add($"--downsample {parameter.DownSampleFactor}");
            var lowArcSecPerPix = imageProperties.ArcSecPerPixel - 0.2;
            var highArcSecPerPix = imageProperties.ArcSecPerPixel + 0.2;
            options.Add("--scale-units arcsecperpix");
            options.Add(string.Format("-L {0}", lowArcSecPerPix.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            options.Add(string.Format("-H {0}", highArcSecPerPix.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));

            if (parameter.SearchRadius > 0 && parameter.Coordinates != null) {
                options.Add($"--ra {parameter.Coordinates.RADegrees.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
                options.Add($"--dec {parameter.Coordinates.Dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
                options.Add($"--radius {parameter.SearchRadius.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
            }

            return string.Format("/C \"\"{0}\" --login -c '/usr/bin/solve-field {1} \"{2}\"'\"", bashLocation, string.Join(" ", options), imageFilePath.Replace("\\", "/"));
        }

        protected override PlateSolveResult ReadResult(string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) {
            var result = new PlateSolveResult() { Success = false };
            if (File.Exists(outputFilePath)) {
                var startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                startInfo.FileName = "cmd.exe";
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.Arguments = string.Format("/C \"\"{0}\" --login -c 'wcsinfo \"{1}\"'\"", bashLocation, outputFilePath.Replace("\\", "/"));
                using (var process = new System.Diagnostics.Process()) {
                    process.StartInfo = startInfo;
                    process.Start();
                    Dictionary<string, string> wcsinfo = new Dictionary<string, string>();
                    while (!process.StandardOutput.EndOfStream) {
                        var line = process.StandardOutput.ReadLine();
                        if (line != null) {
                            var valuepair = line.Split(' ');
                            if (valuepair != null && valuepair.Length == 2) {
                                wcsinfo[valuepair[0]] = valuepair[1];
                            }
                        }
                    }

                    double ra = 0, dec = 0;
                    if (wcsinfo.ContainsKey("ra_center")) {
                        ra = double.Parse(wcsinfo["ra_center"], CultureInfo.InvariantCulture);
                    }
                    if (wcsinfo.ContainsKey("dec_center")) {
                        dec = double.Parse(wcsinfo["dec_center"], CultureInfo.InvariantCulture);
                    }
                    if (wcsinfo.ContainsKey("orientation_center")) {
                        result.Orientation = double.Parse(wcsinfo["orientation_center"], CultureInfo.InvariantCulture);
                        /* Due to the way N.I.N.A. writes FITS files, the orientation is mirrored on the x-axis */
                        result.Orientation = 180 - result.Orientation + 360;
                    }
                    if (wcsinfo.ContainsKey("pixscale")) {
                        result.Pixscale = double.Parse(wcsinfo["pixscale"], CultureInfo.InvariantCulture);
                    }

                    result.Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
                    result.Success = true;
                }
            }
            return result;
        }

        protected override string GetLocalizedPlateSolverName() {
            return Locale.Loc.Instance["LblCygwinBashNotFound"];
        }

        protected override string GetOutputPath(string imageFilePath) {
            return Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + ".wcs";
        }
    }
}