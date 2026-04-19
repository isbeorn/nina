#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Image.FileFormat;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Solvers;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class CliSolverBehaviorTest {

        /// <summary>
        /// Verifies successful CLI solves save a temporary image, update missing target coordinates from telescope metadata, parse the output, and clean temporary files.
        /// </summary>
        [Test]
        public async Task SolveAsync_SuccessCleansTemporaryImageOutputAndSidecarFiles() {
            var solver = new TestableCliSolver { ShouldSucceed = true };
            IImageData imageData = CreateImageData("CliSuccess", out ImageMetaData metadata);
            var statuses = new List<ApplicationStatus>();
            var progress = new RecordingProgress<ApplicationStatus>(statuses);
            var parameter = new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76,
                Coordinates = new Coordinates(Angle.ByDegree(12), Angle.ByDegree(34), Epoch.J2000)
            };

            PlateSolveResult result = await solver.SolveAsync(imageData, parameter, progress, CancellationToken.None);

            result.Success.Should().BeTrue();
            solver.ArgumentsSeen.Should().BeTrue();
            solver.OutputExistsDuringRead.Should().BeTrue();
            metadata.Target.Coordinates.RADegrees.Should().BeApproximately(metadata.Telescope.Coordinates.RADegrees, 1e-10);
            File.Exists(solver.ImagePathSeen).Should().BeFalse();
            File.Exists(solver.OutputPathSeen).Should().BeFalse();
            File.Exists(solver.SidecarPathSeen).Should().BeFalse();
            statuses.Should().Contain(x => !string.IsNullOrWhiteSpace(x.Status));
            statuses.Last().Status.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies failed CLI solves preserve the temporary image, solver output, and sidecar files in the failed-solve archive for diagnostics.
        /// </summary>
        [Test]
        public async Task SolveAsync_FailureMovesTemporaryFilesToFailedArchive() {
            var solver = new TestableCliSolver { ShouldSucceed = false };
            IImageData imageData = CreateImageData("CliFailure", out _);
            var parameter = new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76,
                Coordinates = null
            };

            try {
                PlateSolveResult result = await solver.SolveAsync(imageData, parameter, default, CancellationToken.None);

                result.Success.Should().BeFalse();
                File.Exists(solver.ImagePathSeen).Should().BeFalse();
                File.Exists(solver.OutputPathSeen).Should().BeFalse();
                File.Exists(solver.SidecarPathSeen).Should().BeFalse();
                Directory.GetFiles(solver.FailedDirectory, $"{solver.FailedFilePrefix}.CliFailure.blind*").Should().HaveCountGreaterThanOrEqualTo(3);
            } finally {
                foreach (string file in Directory.GetFiles(solver.FailedDirectory, $"{solver.FailedFilePrefix}.CliFailure.blind*")) {
                    File.Delete(file);
                }
            }
        }

        /// <summary>
        /// Verifies the solver-owned timeout token, not just the caller token, cancels a hung CLI process.
        /// </summary>
        [Test]
        public async Task SolveAsync_TimeoutUsesLinkedSolverTimeoutToken() {
            var solver = new TestableCliSolver {
                ShouldSucceed = true,
                Timeout = TimeSpan.FromMilliseconds(100),
                ArgumentsToReturn = "/C ping 127.0.0.1 -n 6 > nul"
            };
            IImageData imageData = CreateImageData("CliTimeout", out _);
            var parameter = new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76,
                Coordinates = null
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            PlateSolveResult result = await solver.SolveAsync(imageData, parameter, default, CancellationToken.None);
            stopwatch.Stop();

            result.Success.Should().BeFalse();
            solver.ReadResultCalled.Should().BeFalse();
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        }

        private static IImageData CreateImageData(string targetName, out ImageMetaData metadata) {
            metadata = new ImageMetaData();
            metadata.Target.Name = targetName;
            metadata.Telescope.Coordinates = new Coordinates(Angle.ByDegree(22.5), Angle.ByDegree(-11.25), Epoch.J2000);

            var imageData = new Mock<IImageData>();
            imageData.SetupGet(x => x.MetaData).Returns(metadata);
            imageData.SetupGet(x => x.Properties).Returns(new ImageProperties(300, 200, 16, false, 0, 0));
            imageData
                .Setup(x => x.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), true))
                .Returns((FileSaveInfo fileSaveInfo, CancellationToken _, bool _) => {
                    Directory.CreateDirectory(fileSaveInfo.FilePath);
                    string path = Path.Combine(fileSaveInfo.FilePath, $"{Guid.NewGuid():N}.fit");
                    File.WriteAllText(path, "synthetic image");
                    return Task.FromResult(path);
                });
            return imageData.Object;
        }

        private sealed class TestableCliSolver : CLISolver {
            public TestableCliSolver() : base("cmd.exe") {
                string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "CliSolver", Guid.NewGuid().ToString("N"));
                WORKING_DIRECTORY = Path.Combine(root, "Working");
                FAILED_DIRECTORY = Path.Combine(root, "Failed");
                FAILED_FILENAME = Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(WORKING_DIRECTORY);
                Directory.CreateDirectory(FAILED_DIRECTORY);
            }

            public bool ShouldSucceed { get; set; }
            public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
            public string ArgumentsToReturn { get; set; } = "/C exit 0";
            public bool ArgumentsSeen { get; private set; }
            public bool OutputExistsDuringRead { get; private set; }
            public bool ReadResultCalled { get; private set; }
            public string ImagePathSeen { get; private set; }
            public string OutputPathSeen { get; private set; }
            public string SidecarPathSeen { get; private set; }
            public string FailedDirectory => FAILED_DIRECTORY;
            public string FailedFilePrefix => FAILED_FILENAME;

            protected override string GetLocalizedPlateSolverName() {
                return "test solver";
            }

            protected override TimeSpan SolverTimeout => Timeout;

            protected override string GetArguments(string imageFilePath, string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) {
                ArgumentsSeen = true;
                ImagePathSeen = imageFilePath;
                OutputPathSeen = outputFilePath;
                SidecarPathSeen = GetSideCarFilePaths(imageFilePath).Single();
                return ArgumentsToReturn;
            }

            protected override PlateSolveResult ReadResult(string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) {
                ReadResultCalled = true;
                File.WriteAllText(outputFilePath, "solver output");
                File.WriteAllText(SidecarPathSeen, "sidecar output");
                OutputExistsDuringRead = File.Exists(outputFilePath);
                return new PlateSolveResult { Success = ShouldSucceed };
            }

            protected override string GetOutputPath(string imageFilePath) {
                return Path.ChangeExtension(imageFilePath, ".solverout");
            }

            protected override List<string> GetSideCarFilePaths(string imageFilePath) {
                return new List<string> { imageFilePath + ".sidecar" };
            }
        }

        private sealed class RecordingProgress<T> : IProgress<T> {
            private readonly IList<T> values;

            public RecordingProgress(IList<T> values) {
                this.values = values;
            }

            public void Report(T value) {
                values.Add(value);
            }
        }
    }
}
