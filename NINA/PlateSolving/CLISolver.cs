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
using NINA.Model.ImageData;
using NINA.Utility;
using NINA.Utility.Extensions;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal abstract class CLISolver : BaseSolver {
        protected string executableLocation;

        public CLISolver(string executableLocation) {
            this.executableLocation = executableLocation;
        }

        protected abstract string GetLocalizedPlateSolverName();

        protected abstract string GetArguments(
            string imageFilePath,
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties);

        protected abstract PlateSolveResult ReadResult(
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties);

        protected override async Task<PlateSolveResult> SolveAsyncImpl(
            IImageData source,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties,
            IProgress<ApplicationStatus> progress,
            CancellationToken cancelToken) {
            var result = new PlateSolveResult() { Success = false };
            string imagePath = null, outputPath = null;
            try {
                //Copy Image to local app data
                imagePath = await PrepareAndSaveImage(source, cancelToken);

                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSolving"] });

                outputPath = GetOutputPath(imagePath);

                await StartCLI(imagePath, outputPath, parameter, imageProperties, progress, cancelToken);

                //Extract solution coordinates
                result = ReadResult(outputPath, parameter, imageProperties);
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
                if (imagePath != null && File.Exists(imagePath)) {
                    File.Delete(imagePath);
                }
                if (outputPath != null && File.Exists(outputPath)) {
                    File.Delete(outputPath);
                }
            }
            return result;
        }

        protected async Task<string> PrepareAndSaveImage(IImageData source, CancellationToken cancelToken) {
            return await source.SaveToDisk(WORKING_DIRECTORY, Path.GetRandomFileName(), Utility.Enum.FileTypeEnum.FITS, cancelToken);
        }

        protected abstract string GetOutputPath(string imageFilePath);

        protected async Task StartCLI(string imageFilePath, string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (executableLocation != "cmd.exe" && !File.Exists(executableLocation)) {
                throw new FileNotFoundException("Executable not found", executableLocation);
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = executableLocation;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = GetArguments(imageFilePath, outputFilePath, parameter, imageProperties);
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