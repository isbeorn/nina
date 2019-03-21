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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal class LocalPlateSolver : CLISolver {
        private static string imageFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "astrometry_tmp.jpg");
        private static string outputFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "astrometry_tmp.wcs");
        private string bashLocation;

        public LocalPlateSolver(string cygwinRoot) : base("cmd.exe", imageFilePath, outputFilePath) {
            this.bashLocation = Path.GetFullPath(Path.Combine(cygwinRoot, "bin", "bash.exe"));
        }

        protected override string GetArguments(PlateSolveParameter parameter) {
            List<string> options = new List<string>();

            options.Add("-p");
            options.Add("-O");
            options.Add("-U none");
            options.Add("-B none");
            options.Add("-R none");
            options.Add("-M none");
            options.Add("-N none");
            options.Add("-C cancel--crpix");
            options.Add("-center");
            options.Add("--objs 100");
            options.Add("-u arcsecperpix");
            options.Add("--no-plots");
            options.Add("-r");
            options.Add("--downsample 2");
            var lowArcSecPerPix = parameter.ArcSecPerPixel - 0.2;
            var highArcSecPerPix = parameter.ArcSecPerPixel + 0.2;
            options.Add(string.Format("-L {0}", lowArcSecPerPix.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            options.Add(string.Format("-H {0}", highArcSecPerPix.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));

            if (parameter.SearchRadius > 0 && parameter.Coordinates != null) {
                options.Add(string.Format("-3 {0} -4 {1} -5 {2}", parameter.Coordinates.RADegrees.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), parameter.Coordinates.Dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), parameter.SearchRadius.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            }

            return string.Format("/C \"\"{0}\" --login -c '/usr/bin/solve-field {1} \"{2}\"'\"", bashLocation, string.Join(" ", options), imageFilePath.Replace("\\", "/"));
        }

        protected override PlateSolveResult ReadResult(PlateSolveParameter parameter) {
            var result = new PlateSolveResult() { Success = false };
            if (File.Exists(outputFilePath)) {
                var startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                startInfo.FileName = "cmd.exe";
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.Arguments = string.Format("/C \"\"{0}\" --login -c 'wcsinfo \"{1}\"'\"", bashLocation, outputFilePath.Replace("\\", "/"));
                var process = new System.Diagnostics.Process();
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
                }
                if (wcsinfo.ContainsKey("pixscale")) {
                    result.Pixscale = double.Parse(wcsinfo["pixscale"], CultureInfo.InvariantCulture);
                }

                result.Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
                result.Success = true;
            }
            return result;
        }

        public override async Task<PlateSolveResult> SolveAsync(PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var result = new PlateSolveResult() { Success = false };
            try {
                result = await this.Solve(parameter, progress, ct);
            } catch (FileNotFoundException ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblCygwinBashNotFound"] + Environment.NewLine + executableLocation);
            }
            return result;
        }
    }
}