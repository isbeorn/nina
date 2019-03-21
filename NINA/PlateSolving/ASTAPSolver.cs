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

    internal class ASTAPSolver : IPlateSolver {
        private static string TMPIMGFILEPATH = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "astap_tmp.jpg");
        private static string TMPSOLUTIONFILEPATH = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "astap_tmp.ini");
        private string executableLocation;
        private Coordinates target;
        private double height;
        private double fov;
        private double pixelSize;
        private double focalLength;
        private double arcSecPerPixel;
        private double searchRadius;

        public ASTAPSolver(int focalLength, double pixelSize, double height, string executableLocation) {
            this.focalLength = focalLength;
            this.pixelSize = pixelSize;
            this.arcSecPerPixel = Astrometry.ArcsecPerPixel(pixelSize, focalLength);
            this.executableLocation = executableLocation;
            this.height = height;
        }

        public ASTAPSolver(int focalLength, double pixelSize, double height, double searchRadius, Coordinates target, string executableLocation) : this(focalLength, pixelSize, height, executableLocation) {
            this.searchRadius = searchRadius;
            this.target = target;
        }

        private PlateSolveResult ExtractResult() {
            var result = new PlateSolveResult() { Success = false };
            if (File.Exists(TMPSOLUTIONFILEPATH)) {
                var dict = File.ReadLines(TMPSOLUTIONFILEPATH)
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
                        result.Pixscale = this.arcSecPerPixel;
                    }
                }
            }
            return result;
        }

        public async Task<PlateSolveResult> SolveAsync(MemoryStream image, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            var result = new PlateSolveResult() { Success = false };
            try {
                //Copy Image to local app data
                using (FileStream fs = new FileStream(TMPIMGFILEPATH, FileMode.Create)) {
                    image.CopyTo(fs);
                }

                canceltoken.ThrowIfCancellationRequested();

                //Start astap
                await StartASTAPProcess(progress);

                canceltoken.ThrowIfCancellationRequested();

                //Extract solution coordinates
                result = ExtractResult();
            } finally {
                if (File.Exists(TMPSOLUTIONFILEPATH)) {
                    File.Delete(TMPSOLUTIONFILEPATH);
                }

                if (File.Exists(TMPIMGFILEPATH)) {
                    File.Delete(TMPIMGFILEPATH);
                }
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return result;
        }

        /// <summary>
        /// Creates the arguments to launche ASTAP process
        /// </summary>
        /// <returns></returns>
        /// <remarks>http://www.hnsky.org/astap.htm#astap_command_line</remarks>
        private string GetArguments() {
            var args = new List<string>();

            //File location to solve
            args.Add($"-f {TMPIMGFILEPATH}");

            //Field height of image
            args.Add($"-fov {Astrometry.ArcsecToDegree(this.arcSecPerPixel * height).ToString(CultureInfo.InvariantCulture)}");

            //Downsample factor
            args.Add("-z 2");

            //Max number of stars
            args.Add("-s 500");

            if (searchRadius > 0 && target != null) {
                //Search field radius
                args.Add($"-r {searchRadius}");

                //Right Ascension in degrees
                args.Add($"-ra {target.RA.ToString(CultureInfo.InvariantCulture)}");

                //Declination in degrees
                args.Add($"-dec {target.Dec.ToString(CultureInfo.InvariantCulture)}");
            }

            return string.Join(" ", args);
        }

        /// <summary>
        /// Runs the ASTAP process
        /// </summary>
        /// <returns>true: ran successfully; false: not found</returns>
        private Task StartASTAPProcess(IProgress<ApplicationStatus> progress) {
            var location = Path.GetFullPath(this.executableLocation);

            if (!File.Exists(location)) {
                Notification.ShowError(Locale.Loc.Instance["LblASTAPNotFound"] + Environment.NewLine + location);
                return Task.FromResult(0);
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = location;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = GetArguments();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
                progress.Report(new ApplicationStatus() { Status = e.Data });
            };

            process.ErrorDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
                progress.Report(new ApplicationStatus() { Status = e.Data });
            };

            process.Start();

            return process.WaitForExitAsync();
        }
    }
}