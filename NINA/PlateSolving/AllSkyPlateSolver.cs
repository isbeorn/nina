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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal class AllSkyPlateSolver : IPlateSolver {
        private int focalLength;
        private double pixelSize;
        private double searchRadius;
        private Coordinates coords;
        private string aspsLocation;
        private string imageFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "tmp.jpg").Replace("\\", "/");
        private string outputFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "aspsresult.txt");

        public AllSkyPlateSolver(int focalLength, double pixelSize, string aspsLocation) {
            this.focalLength = focalLength;
            this.pixelSize = pixelSize;
            this.aspsLocation = aspsLocation;
        }

        public AllSkyPlateSolver(int focalLength, double pixelSize, double searchRadius, Coordinates coords, string aspsLocation) {
            this.focalLength = focalLength;
            this.pixelSize = pixelSize;
            this.searchRadius = searchRadius;
            this.coords = coords;
            this.aspsLocation = aspsLocation;
        }

        public Task<PlateSolveResult> SolveAsync(MemoryStream image, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return Task.Run(() => Solve(image, progress, ct));
        }

        private PlateSolveResult Solve(MemoryStream image, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var result = new PlateSolveResult() { Success = false };
            try {
                using (var fs = new FileStream(imageFilePath, FileMode.Create)) {
                    image.CopyTo(fs);
                }

                CallCommandLine(progress, ct);

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
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
            } finally {
                if (File.Exists(outputFilePath)) {
                    File.Delete(outputFilePath);
                }
                if (File.Exists(imageFilePath)) {
                    File.Delete(imageFilePath);
                }
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return result;
        }

        private void CallCommandLine(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = aspsLocation;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = string.Format("/solvefile {0}", GetArguments());
            process.StartInfo = startInfo;
            process.Start();

            using (ct.Register(() => process.Kill())) {
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolving"] });

                while (!process.StandardOutput.EndOfStream) {
                    progress.Report(new ApplicationStatus() { Status = process.StandardOutput.ReadLine() });
                    ct.ThrowIfCancellationRequested();
                }
            }
        }

        private string GetArguments() {
            var args = new List<string>();

            //FileName
            args.Add(imageFilePath);

            //OutFile
            args.Add(outputFilePath);

            //FocalLength
            args.Add(focalLength.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            //PixelSize
            args.Add(pixelSize.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            if (coords != null) {
                //CurrentRA
                args.Add(coords.RADegrees.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

                //CurrentDec
                args.Add(coords.Dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            } else {
                args.Add("0");
                args.Add("0");
            }

            //NearRadius
            args.Add(searchRadius.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            return string.Join(" ", args);
        }
    }
}