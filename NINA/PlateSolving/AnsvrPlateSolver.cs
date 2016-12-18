using NINA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using nom.tam.fits;

namespace NINA.PlateSolving {
    class AnsvrPlateSolver : IPlateSolver {
        

        double _lowarcsecperpixel;
        double _higharcsecperpixel;

        public AnsvrPlateSolver(int focallength, double pixelsize) {
            double arcsecperpixel = (pixelsize / focallength) * 206.3;
            _lowarcsecperpixel = arcsecperpixel - 0.2;
            _higharcsecperpixel = arcsecperpixel + 0.2;
        }

        private PlateSolveResult solve(MemoryStream image, double low, double high, IProgress<string> progress, CancellationTokenSource canceltoken) {
            PlateSolveResult result = new PlateSolveResult();
            string path = Directory.GetCurrentDirectory() + @"\tmp";
            string filepath = path + "\\tmp.jpeg";
            /*write image to temporary dir */
            Directory.CreateDirectory(path);
            using (FileStream fs = new FileStream(filepath, FileMode.Create)) {
                image.CopyTo(fs);
            }


            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = string.Format("/C %localappdata%\\cygwin_ansvr\\bin\\bash.exe --login -c '/usr/bin/solve-field -p -O -U none -B none -R none -M none -N none -C cancel--crpix -center -z 2 --objs 100 -u arcsecperpix -L {0} -H {1} {2}'", low.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), high.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), filepath.Replace("\\", "/"));            
            process.StartInfo = startInfo;
            process.Start();            

            while(!process.StandardOutput.EndOfStream) {
                progress.Report(process.StandardOutput.ReadLine());
                canceltoken.Token.ThrowIfCancellationRequested();
            }

            
            if(File.Exists(path+ "\\tmp.wcs")) {
                progress.Report("Solved");
                Fits solvedFits = new Fits(path + "\\tmp.wcs");
                BasicHDU solvedHDU = solvedFits.GetHDU(0);
                result.Ra = double.Parse(solvedHDU.Header.FindCard("CRVAL1").Value, System.Globalization.CultureInfo.InvariantCulture);
                result.Dec = double.Parse(solvedHDU.Header.FindCard("CRVAL2").Value, System.Globalization.CultureInfo.InvariantCulture);
            }





            return result;
        }

        public async Task<PlateSolveResult> blindSolve(MemoryStream image, IProgress<string> progress, CancellationTokenSource canceltoken) {
            return await Task<PlateSolveResult>.Run(() => solve(image, _lowarcsecperpixel, _higharcsecperpixel, progress, canceltoken));
        }
    }
}
