using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal class Platesolve2Solver : IPlateSolver {
        private double _arcdegwidth;
        private double _arcdegheight;
        private int _regions;
        private Coordinates _target;

        private static string TMPIMGFILEPATH = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "tmp.jpg");
        private static string TMPSOLUTIONFILEPATH = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "tmp.apm");
        private string _ps2Location;

        public Platesolve2Solver(int focallength, double pixelsize, double width, double height, int regions, Coordinates target, string ps2Location) {
            this._ps2Location = ps2Location;
            double arcsecperpixel = (pixelsize / focallength) * 206.3;

            _arcdegwidth = Astrometry.ArcsecToDegree(arcsecperpixel * width);
            _arcdegheight = Astrometry.ArcsecToDegree(arcsecperpixel * height);

            _regions = regions;
            _target = target;
        }

        /// <summary>
        /// Gets start arguments for Platesolve2 out of RA,Dec, ArcDegWidth, ArcDegHeight and ImageFilePath
        /// </summary>
        /// <returns></returns>
        private string GetArguments() {
            var args = new string[] {
                    Astrometry.ToRadians(_target.RADegrees).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(_target.Dec).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(_arcdegwidth).ToString(CultureInfo.InvariantCulture),
                    Astrometry.ToRadians(_arcdegheight).ToString(CultureInfo.InvariantCulture),
                    _regions.ToString(),
                    TMPIMGFILEPATH,
                    "0"
            };
            return string.Join(",", args);
        }

        /// <summary>
        /// Runs the platesolve2 process
        /// </summary>
        /// <returns>true: ran successfully; false: not found</returns>
        private bool StartPlatesolve2Process() {
            var ps2locaction = Path.GetFullPath(this._ps2Location);

            if (!File.Exists(ps2locaction)) {
                Notification.ShowError(Locale.Loc.Instance["LblPlatesolve2NotFound"] + Environment.NewLine + ps2locaction);
                return false;
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = ps2locaction;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = GetArguments();
            process.StartInfo = startInfo;
            process.Start();

            process.WaitForExit();
            return true;
        }

        /// <summary>
        /// Extract result out of generated .axy file. File consists of three rows
        /// 1. row: RA,Dec,Code
        /// 2. row: Scale,Orientation,?,?,Stars
        /// </summary>
        /// <returns>PlateSolveResult</returns>
        private PlateSolveResult ExtractResult() {
            PlateSolveResult result = new PlateSolveResult() { Success = false };
            if (File.Exists(TMPSOLUTIONFILEPATH)) {
                using (var s = new StreamReader(TMPSOLUTIONFILEPATH)) {
                    string line;
                    int linenr = 0;
                    while ((line = s.ReadLine()) != null) {
                        string[] resultArr = line.Split(',');
                        if (linenr == 0) {
                            if (resultArr.Length > 2) {
                                double ra, dec;
                                int status;
                                if (resultArr.Length == 5) {
                                    /* workaround for when decimal separator is comma instead of point.
                                     won't work when result contains even numbers tho... */
                                    status = int.Parse(resultArr[4]);
                                    if (status != 1) {
                                        /* error */
                                        result.Success = false;
                                        break;
                                    }

                                    ra = double.Parse(resultArr[0] + "." + resultArr[1], CultureInfo.InvariantCulture);
                                    dec = double.Parse(resultArr[2] + "." + resultArr[3], CultureInfo.InvariantCulture);
                                } else {
                                    status = int.Parse(resultArr[2]);
                                    if (status != 1) {
                                        /* error */
                                        result.Success = false;
                                        break;
                                    }

                                    ra = double.Parse(resultArr[0], CultureInfo.InvariantCulture);
                                    dec = double.Parse(resultArr[1], CultureInfo.InvariantCulture);
                                }

                                /* success */
                                result.Success = true;
                                result.Coordinates = new Coordinates(Astrometry.ToDegree(ra), Astrometry.ToDegree(dec), Epoch.J2000, Coordinates.RAType.Degrees);
                            }
                        }
                        if (linenr == 1) {
                            if (resultArr.Length > 2) {
                                if (resultArr.Length > 5) {
                                    /* workaround for when decimal separator is comma instead of point.
                                     won't work when result contains even numbers tho... */
                                    result.Pixscale = double.Parse(resultArr[0] + "." + resultArr[1], CultureInfo.InvariantCulture);
                                    result.Orientation = double.Parse(resultArr[2] + "." + resultArr[3], CultureInfo.InvariantCulture);
                                } else {
                                    result.Pixscale = double.Parse(resultArr[0], CultureInfo.InvariantCulture);
                                    result.Orientation = double.Parse(resultArr[1], CultureInfo.InvariantCulture);
                                }
                            }
                        }
                        linenr++;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Solves image by first copying to filesystem -&gt; calling platesolve2 -&gt; parsing
        /// result file
        /// </summary>
        /// <param name="image">      </param>
        /// <param name="progress">   </param>
        /// <param name="canceltoken"></param>
        /// <returns></returns>
        private PlateSolveResult Solve(MemoryStream image, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            PlateSolveResult result = new PlateSolveResult() { Success = false };
            try {
                progress.Report(new ApplicationStatus() { Status = "Solving..." });
                //Copy Image to local app data
                using (FileStream fs = new FileStream(TMPIMGFILEPATH, FileMode.Create)) {
                    image.CopyTo(fs);
                }

                canceltoken.ThrowIfCancellationRequested();

                //Start platesolve2
                if (!StartPlatesolve2Process()) {
                    return result;
                }

                canceltoken.ThrowIfCancellationRequested();

                //Extract solution coordinates
                result = ExtractResult();
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
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

        public async Task<PlateSolveResult> SolveAsync(MemoryStream image, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            return await Task<PlateSolveResult>.Run(() => Solve(image, progress, canceltoken));
        }
    }
}