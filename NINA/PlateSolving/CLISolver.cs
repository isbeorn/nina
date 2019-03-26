using NINA.Model;
using NINA.Utility;
using NINA.Utility.Extensions;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal abstract class CLISolver : IPlateSolver {
        protected string executableLocation;
        private string imageFilePath;
        private string outputFilePath;

        public CLISolver(string executableLocation, string imageFilePath, string outputFilePath) {
            this.executableLocation = executableLocation;
            this.imageFilePath = imageFilePath;
            this.outputFilePath = outputFilePath;
        }

        public abstract Task<PlateSolveResult> SolveAsync(PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken canceltoken);

        protected abstract string GetArguments(PlateSolveParameter parameter);

        protected abstract PlateSolveResult ReadResult(PlateSolveParameter parameter);

        protected async Task<PlateSolveResult> Solve(PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var result = new PlateSolveResult() { Success = false };
            try {
                //Cleanup files from previous solves
                if (File.Exists(outputFilePath)) {
                    File.Delete(outputFilePath);
                }

                if (File.Exists(imageFilePath)) {
                    File.Delete(imageFilePath);
                }

                //Copy Image to local app data
                using (FileStream fs = new FileStream(imageFilePath, FileMode.Create)) {
                    parameter.Image.CopyTo(fs);
                }

                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSolving"] });

                await StartCLI(parameter, progress, ct);

                //Extract solution coordinates
                result = ReadResult(parameter);
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return result;
        }

        protected async Task StartCLI(PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            //var location = Path.GetFullPath(this.executableLocation);

            if (executableLocation != "cmd.exe" && !File.Exists(executableLocation)) {
                throw new FileNotFoundException();
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = executableLocation;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = GetArguments(parameter);
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
                progress.Report(new ApplicationStatus() { Status = e.Data });
            };

            process.ErrorDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
                progress.Report(new ApplicationStatus() { Status = e.Data });
            };
            Logger.Debug($"Starting process '{executableLocation}' with args '{startInfo.Arguments}'");
            process.Start();
            await process.WaitForExitAsync(ct);
        }
    }
}