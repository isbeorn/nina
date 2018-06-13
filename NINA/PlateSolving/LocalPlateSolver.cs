using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal class LocalPlateSolver : IPlateSolver {
        private double _lowarcsecperpixel;
        private double _higharcsecperpixel;
        private double _searchradius;

        private Coordinates _target;
        private string cygwinLocation;

        private LocalPlateSolver(string cygwinLocation) {
            this.cygwinLocation = cygwinLocation;
        }

        public LocalPlateSolver(int focallength, double pixelsize, string cygwinLocation) : this(cygwinLocation) {
            double arcsecperpixel = (pixelsize / focallength) * 206.3;
            _lowarcsecperpixel = arcsecperpixel - 0.2;
            _higharcsecperpixel = arcsecperpixel + 0.2;
        }

        public LocalPlateSolver(int focallength, double pixelsize, double searchradius, Coordinates target, string cygwinLocation) : this(focallength, pixelsize, cygwinLocation) {
            _searchradius = searchradius;
            _target = target;
        }

        private string GetOptions() {
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
            options.Add(string.Format("-L {0}", _lowarcsecperpixel.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            options.Add(string.Format("-H {0}", _higharcsecperpixel.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));

            if (_searchradius > 0) {
                options.Add(string.Format("-3 {0} -4 {1} -5 {2}", _target.RADegrees.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), _target.Dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), _searchradius.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            }

            return string.Join(" ", options);
        }

        private PlateSolveResult Solve(MemoryStream image, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            PlateSolveResult result = new PlateSolveResult();
            string imgfilepath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "tmp.jpg");
            var wcsfilepath = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "tmp.wcs");
            try {
                progress.Report(new ApplicationStatus() { Status = "Solving..." });

                using (FileStream fs = new FileStream(imgfilepath, FileMode.Create)) {
                    image.CopyTo(fs);
                }

                var cygwinbashpath = Path.GetFullPath(Path.Combine(cygwinLocation, "bin", "bash.exe"));

                if (!File.Exists(cygwinbashpath)) {
                    Logger.Error(Locale.Loc.Instance["LblCygwinBashNotFound"] + Environment.NewLine + cygwinbashpath, null);
                    Notification.ShowError(Locale.Loc.Instance["LblCygwinBashNotFound"] + Environment.NewLine + cygwinbashpath);
                    result.Success = false;
                    return result;
                }

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                startInfo.FileName = "cmd.exe";
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.Arguments = string.Format("/C {0} --login -c '/usr/bin/solve-field {1} {2}'", cygwinbashpath, GetOptions(), imgfilepath.Replace("\\", "/"));
                //startInfo.Arguments = string.Format("/C {0} --login -c '/usr/bin/solve-field -p -O -U none -B none -R none -M none -N none --sigma 70--no -C cancel--crpix -center --objs 100 -u arcsecperpix -L {1} -H {2} {3}'", cygwinbashpath, low.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), high.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), filepath.Replace("\\", "/"));
                process.StartInfo = startInfo;
                process.Start();

                while (!process.StandardOutput.EndOfStream) {
                    progress.Report(new ApplicationStatus() { Status = process.StandardOutput.ReadLine() });
                    canceltoken.ThrowIfCancellationRequested();
                }

                if (File.Exists(wcsfilepath)) {
                    startInfo.Arguments = string.Format("/C {0} --login -c 'wcsinfo {1}'", cygwinbashpath, wcsfilepath.Replace("\\", "/"));
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

                    progress.Report(new ApplicationStatus() { Status = "Solved" });

                    /* This info does not get the center info. - removed
                        Fits solvedFits = new Fits(TMPIMGFILEPATH + "\\tmp.wcs");
                        BasicHDU solvedHDU = solvedFits.GetHDU(0);
                        result.Ra = double.Parse(solvedHDU.Header.FindCard("CRVAL1").Value, System.Globalization.CultureInfo.InvariantCulture);
                        result.Dec = double.Parse(solvedHDU.Header.FindCard("CRVAL2").Value, System.Globalization.CultureInfo.InvariantCulture);
                    */
                } else {
                    result.Success = false;
                }
            } catch (OperationCanceledException) {
                result.Success = false;
            } catch (Exception ex) {
                Logger.Error(ex);
                result.Success = false;
            } finally {
                if (File.Exists(wcsfilepath)) {
                    File.Delete(wcsfilepath);
                }
                if (File.Exists(imgfilepath)) {
                    File.Delete(imgfilepath);
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