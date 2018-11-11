using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;

namespace NINA.PlateSolving {

    internal class AllSkyPlateSolver : IPlateSolver {
        private int focalLength;
        private double pixelSize;
        private double searchRadius;
        private Coordinates coords;
        private string aspsLocation;
        private string imageFilePath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "tmp.jpg");
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
                        if (lines[0] == "OK" && lines.Length > 8) {
                            var ra = double.Parse(lines[1]);
                            var dec = double.Parse(lines[2]);

                            result.Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);

                            var fovW = lines[3];
                            var fovH = lines[4];

                            result.Pixscale = double.Parse(lines[5]);
                            result.Orientation = double.Parse(lines[6]);

                            var focalLength = lines[7];
                        }
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            return result;
        }

        private void CallCommandLine(IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = string.Format("/C {0} /solvefile {1}", aspsLocation, GetArguments());
            process.StartInfo = startInfo;
            process.Start();

            while (!process.StandardOutput.EndOfStream) {
                progress.Report(new ApplicationStatus() { Status = process.StandardOutput.ReadLine() });
                canceltoken.ThrowIfCancellationRequested();
            }
        }

        private string GetArguments() {
            var args = new List<string>();

            args.Add("FileName");
            args.Add(imageFilePath);

            args.Add("OutFile");
            args.Add(outputFilePath);

            args.Add("FocalLength");
            args.Add(focalLength.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            args.Add("PixelSize");
            args.Add(pixelSize.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            if (coords != null) {
                args.Add("CurrentRA");
                args.Add(coords.RA.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

                args.Add("CurrentDec");
                args.Add(coords.Dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            }

            args.Add("NearRadius");
            args.Add(searchRadius.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            return string.Join(" ", args);
        }
    }
}