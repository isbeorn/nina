using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving
{
    class Platesolve2Solver : IPlateSolver {

        double _arcdegwidth;
        double _arcdegheight;
        int _regions;
        Coordinates _target;

        static string TMPIMGFILEPATH = Environment.GetEnvironmentVariable("LocalAppData") + "\\NINA";

        public Platesolve2Solver(int focallength, double pixelsize, double width, double height, int regions, Coordinates target) {
            double arcsecperpixel = (pixelsize / focallength) * 206.3;

            _arcdegwidth = (arcsecperpixel * width) / 60 / 60;
            _arcdegheight = (arcsecperpixel * height) / 60 / 60;

            _regions = regions;
            _target = target;
        }

        private PlateSolveResult Solve(MemoryStream image, IProgress<string> progress, CancellationTokenSource canceltoken) {
            PlateSolveResult result = new PlateSolveResult();
            try {
                string filepath = TMPIMGFILEPATH + "\\tmp.jpg";

                using (FileStream fs = new FileStream(filepath, FileMode.Create)) {
                    image.CopyTo(fs);
                }

                var ps2locaction = Path.GetFullPath(Settings.PS2Location);

                if (!File.Exists(ps2locaction)) {
                    Utility.Notification.ShowError(string.Format("platesolve2 not found at {0}", ps2locaction));
                    result.Success = false;
                    return result;
                }

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                startInfo.FileName = ps2locaction;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;

                var args = new string[] { Utility.Utility.ToRadians(_target.RADegrees).ToString(CultureInfo.InvariantCulture),
                    Utility.Utility.ToRadians(_target.Dec).ToString(CultureInfo.InvariantCulture),
                    Utility.Utility.ToRadians(_arcdegwidth).ToString(CultureInfo.InvariantCulture),
                    Utility.Utility.ToRadians(_arcdegheight).ToString(CultureInfo.InvariantCulture),
                    _regions.ToString(),
                    filepath,
                    "0"};

                startInfo.Arguments = string.Join(",",args);
                

                process.StartInfo = startInfo;
                process.Start();

                process.WaitForExit();

                filepath = TMPIMGFILEPATH + "\\tmp.apm";

                if (File.Exists(filepath)) {

                    using (var s = new StreamReader(filepath)) {
                        string line;
                        int linenr = 0;
                        while ((line = s.ReadLine()) != null) {
                            string[] resultArr = line.Split(',');
                            if(linenr == 0) {
                                if(resultArr.Length > 2) {
                                    int statusCode = int.Parse(resultArr[2]);
                                    
                                    if (statusCode != 1) {
                                        /* error */
                                        result.Success = false;
                                        break;
                                    }
                                    
                                    /* success */
                                    result.Success = true;
                                    double RARad = double.Parse(resultArr[0], CultureInfo.InvariantCulture);
                                    double DecRad = double.Parse(resultArr[1], CultureInfo.InvariantCulture);
                                    result.Ra = Utility.Utility.ToDegree(RARad);
                                    result.Dec = Utility.Utility.ToDegree(DecRad);
                                    
                                }
                                
                            }
                            if (linenr == 1) {
                                if (resultArr.Length > 2) {
                                    result.Pixscale = double.Parse(resultArr[0], CultureInfo.InvariantCulture);
                                    result.Orientation = double.Parse(resultArr[1], CultureInfo.InvariantCulture);
                                }
                            }
                            linenr++;
                        }
                    }
                } else {
                    result.Success = false;
                }

            } catch (OperationCanceledException ex) {
                progress.Report("Cancelled");
                Logger.Trace(ex.Message);
                result.Success = false;
            } catch (Exception ex) {
                Logger.Error(ex.Message, ex.StackTrace);
                result.Success = false;
            }

            return result;
        }

        public async Task<PlateSolveResult> SolveAsync(MemoryStream image, IProgress<string> progress, CancellationTokenSource canceltoken) {
            return await Task<PlateSolveResult>.Run(() => Solve(image, progress, canceltoken));
        }
    }
}
