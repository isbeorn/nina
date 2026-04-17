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
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Solvers;
using System.Globalization;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class SolverTranslationBehaviorTest {

        /// <summary>
        /// Verifies ASTAP command arguments use the documented RA-hours and SPD-degrees inputs, include bounded search options, and fall back to a 180-degree blind radius.
        /// </summary>
        [Test]
        public void ASTAPSolver_ArgumentsUseDocumentedRaHoursAndSpdDegreeInputsAndBlindFallback() {
            var solver = new TestableASTAPSolver(@"C:\astap.exe");
            PlateSolveParameter hinted = CreateParameter();
            PlateSolveImageProperties properties = CreateImageProperties(hinted, width: 300, height: 200);

            string hintedArgs = solver.Arguments(@"C:\Images\light.fit", @"C:\Images\light.ini", hinted, properties);
            PlateSolveParameter blindParameter = CreateParameter();
            blindParameter.Coordinates = null;
            string blindArgs = solver.Arguments(@"C:\Images\light.fit", @"C:\Images\light.ini", blindParameter, properties);

            hintedArgs.Should().Contain("-f \"C:\\Images\\light.fit\"");
            hintedArgs.Should().Contain("-fov ");
            hintedArgs.Should().Contain("-z 2");
            hintedArgs.Should().Contain("-s 150");
            hintedArgs.Should().Contain("-r 5");
            hintedArgs.Should().Contain("-ra 12.5");
            hintedArgs.Should().Contain("-spd 67.75");
            blindArgs.Should().Contain("-r 180");
            blindArgs.Should().NotContain("-ra ");
        }

        /// <summary>
        /// Verifies ASTAP result parsing handles successful WCS output, missing output files, and failed solve reports deterministically.
        /// </summary>
        [Test]
        public void ASTAPSolver_ReadResult_ParsesWcsAndFailureStates() {
            string directory = CreateTempDirectory();
            try {
                string successPath = Path.Combine(directory, "astap.ini");
                File.WriteAllLines(successPath, new[] {
                    "PLTSOLVD=T",
                    "WARNING=Low star count",
                    "CRVAL1=201.5",
                    "CRVAL2=-43.25",
                    "CRPIX1=150",
                    "CRPIX2=100",
                    "CD1_1=-0.00027",
                    "CD1_2=0",
                    "CD2_1=0",
                    "CD2_2=0.00027"
                });

                string failurePath = Path.Combine(directory, "failed.ini");
                File.WriteAllLines(failurePath, new[] {
                    "PLTSOLVD=F",
                    "WARNING=No quad match",
                    "ERROR=Too few stars"
                });

                var solver = new TestableASTAPSolver(@"C:\astap.exe");
                PlateSolveParameter parameter = CreateParameter();
                PlateSolveImageProperties properties = CreateImageProperties(parameter, width: 300, height: 200);

                PlateSolveResult success = solver.Result(successPath, parameter, properties);
                PlateSolveResult failed = solver.Result(failurePath, new PlateSolveParameter { DisableNotifications = true }, properties);
                PlateSolveResult missing = solver.Result(Path.Combine(directory, "missing.ini"), new PlateSolveParameter { DisableNotifications = true }, properties);

                success.Success.Should().BeTrue();
                success.Coordinates.RADegrees.Should().BeApproximately(201.5, 1e-8);
                success.Coordinates.Dec.Should().BeApproximately(-43.25, 1e-8);
                success.Pixscale.Should().BeApproximately(0.972, 1e-8);
                success.Radius.Should().BeGreaterThan(0);
                success.PositionAngle.Should().BeApproximately(180, 1e-8);
                success.Flipped.Should().BeTrue();
                failed.Success.Should().BeFalse();
                missing.Success.Should().BeFalse();
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies ASTAP validation rejects missing executables and legacy auto-downsample combinations before a solve starts.
        /// </summary>
        [Test]
        public void ASTAPSolver_ValidationRejectsMissingOrLegacyAutoDownsampleConfiguration() {
            var missingSolver = new TestableASTAPSolver(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "astap.exe"));
            Action missing = () => missingSolver.Validate(CreateParameter());

            string directory = CreateTempDirectory();
            try {
                string executable = Path.Combine(directory, "astap.exe");
                File.WriteAllBytes(executable, Array.Empty<byte>());
                var legacySolver = new TestableASTAPSolver(executable);
                Action legacyAutoDownsample = () => legacySolver.Validate(CreateParameter(downSampleFactor: 0));
                Action validLegacy = () => legacySolver.Validate(CreateParameter(downSampleFactor: 2));

                missing.Should().Throw<ASTAPSolver.ASTAPValidationFailedException>();
                legacyAutoDownsample.Should().Throw<ASTAPSolver.ASTAPValidationFailedException>();
                validLegacy.Should().NotThrow();
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies PlateSolve2 command-line radians and result files, including mirrored orientation and decimal-comma compatibility.
        /// </summary>
        [Test]
        public void Platesolve2Solver_TranslatesArgumentsAndResultFiles() {
            string directory = CreateTempDirectory();
            try {
                var solver = new TestablePlatesolve2Solver(@"C:\ps2.exe");
                PlateSolveParameter parameter = CreateParameter();
                PlateSolveImageProperties properties = CreateImageProperties(parameter, width: 300, height: 200);
                string outputPath = Path.Combine(directory, "result.apm");
                File.WriteAllLines(outputPath, new[] {
                    $"{Math.PI.ToString(CultureInfo.InvariantCulture)},{(Math.PI / 6).ToString(CultureInfo.InvariantCulture)},1",
                    "1.25,45,-1"
                });

                string args = solver.Arguments(@"C:\Images\light.fit", outputPath, parameter, properties);
                PlateSolveResult result = solver.Result(outputPath, parameter, properties);

                args.Should().Contain(AstroUtil.ToRadians(187.5).ToString(CultureInfo.InvariantCulture));
                args.Should().Contain(AstroUtil.ToRadians(-22.25).ToString(CultureInfo.InvariantCulture));
                args.Should().Contain(@"C:\Images\light.fit");
                result.Success.Should().BeTrue();
                result.Coordinates.RADegrees.Should().BeApproximately(180, 1e-8);
                result.Coordinates.Dec.Should().BeApproximately(30, 1e-8);
                result.Pixscale.Should().BeApproximately(1.25, 1e-8);
                result.PositionAngle.Should().BeApproximately(315, 1e-8);
                result.Flipped.Should().BeFalse();
                result.Radius.Should().BeGreaterThan(0);

                string commaPath = Path.Combine(directory, "comma.apm");
                File.WriteAllLines(commaPath, new[] {
                    "3,141592653589793,0,523598775598299,1",
                    "1,25,45,5,-1,0"
                });
                PlateSolveResult commaResult = solver.Result(commaPath, parameter, properties);
                commaResult.Success.Should().BeTrue();
                commaResult.Pixscale.Should().BeApproximately(1.25, 1e-8);
                commaResult.PositionAngle.Should().BeApproximately(314.5, 1e-8);
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies PlateSolve3 supports both hinted and blind argument sets and parses successful and failed result files.
        /// </summary>
        [Test]
        public void Platesolve3Solver_TranslatesArgumentsAndResultFiles() {
            string directory = CreateTempDirectory();
            try {
                var solver = new TestablePlatesolve3Solver(@"C:\ps3.exe");
                PlateSolveParameter hinted = CreateParameter();
                PlateSolveImageProperties properties = CreateImageProperties(hinted, width: 300, height: 200);
                string outputPath = Path.Combine(directory, "result_PS3.txt");
                File.WriteAllLines(outputPath, new[] {
                    "True",
                    $"{Math.PI.ToString(CultureInfo.InvariantCulture)},{(Math.PI / 6).ToString(CultureInfo.InvariantCulture)}",
                    "103132.4,123.4",
                    "Triangle",
                    "0,0"
                });
                string failedPath = Path.Combine(directory, "failed_PS3.txt");
                File.WriteAllText(failedPath, "False");

                string hintedArgs = solver.Arguments(@"C:\Images\light.fit", outputPath, hinted, properties);
                PlateSolveParameter blindParameter = CreateParameter();
                blindParameter.Coordinates = null;
                string blindArgs = solver.Arguments(@"C:\Images\light.fit", outputPath, blindParameter, properties);
                PlateSolveResult result = solver.Result(outputPath, hinted, properties);
                PlateSolveResult failed = solver.Result(failedPath, hinted, properties);

                hintedArgs.Should().StartWith("\"C:\\Images\\light.fit\"");
                hintedArgs.Should().Contain(AstroUtil.ToRadians(187.5).ToString(CultureInfo.InvariantCulture));
                blindArgs.Should().Contain("\"C:\\Images\\light.fit\" 0 0");
                result.Success.Should().BeTrue();
                result.Coordinates.RADegrees.Should().BeApproximately(180, 1e-8);
                result.Coordinates.Dec.Should().BeApproximately(30, 1e-8);
                result.Pixscale.Should().BeApproximately(2.0, 1e-3);
                result.PositionAngle.Should().BeApproximately(123.4, 1e-8);
                failed.Success.Should().BeFalse();
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies All Sky Plate Solver argument formatting and UTF-8 result parsing for successful and invalid outputs.
        /// </summary>
        [Test]
        public void AllSkyPlateSolver_TranslatesArgumentsAndResultFiles() {
            string directory = CreateTempDirectory();
            try {
                var solver = new TestableAllSkyPlateSolver(@"C:\asps.exe");
                PlateSolveParameter parameter = CreateParameter();
                PlateSolveImageProperties properties = CreateImageProperties(parameter, width: 300, height: 200);
                string outputPath = Path.Combine(directory, "asps.txt");
                File.WriteAllLines(outputPath, new[] {
                    "OK",
                    "201.5",
                    "-43.25",
                    "1.2",
                    "0.8",
                    "0.972",
                    "12.5",
                    "600"
                });
                string failedPath = Path.Combine(directory, "failed.txt");
                File.WriteAllText(failedPath, "FAILED");

                string args = solver.Arguments(@"C:\Images\light.fit", outputPath, parameter, properties);
                PlateSolveResult result = solver.Result(outputPath, parameter, properties);
                PlateSolveResult failed = solver.Result(failedPath, parameter, properties);

                args.Should().StartWith("/solvefile");
                args.Should().Contain("\"C:/Images/light.fit\"");
                args.Should().Contain("\"" + outputPath + "\"");
                args.Should().Contain("600.00");
                args.Should().Contain("3.76");
                args.Should().Contain("187.50");
                args.Should().Contain("-22.25");
                args.Should().Contain("5.00");
                result.Success.Should().BeTrue();
                result.Coordinates.RADegrees.Should().BeApproximately(201.5, 1e-8);
                result.Coordinates.Dec.Should().BeApproximately(-43.25, 1e-8);
                result.Pixscale.Should().BeApproximately(0.972, 1e-8);
                result.PositionAngle.Should().BeApproximately(192.5, 1e-8);
                result.Radius.Should().BeGreaterThan(0);
                failed.Success.Should().BeFalse();
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies the local astrometry.net wrapper emits Cygwin solve-field arguments with scale bounds and optional hinted sky coordinates.
        /// </summary>
        [Test]
        public void LocalPlateSolver_TranslatesHintedAndBlindArguments() {
            var solver = new TestableLocalPlateSolver(@"C:\cygwin64");
            PlateSolveParameter hinted = CreateParameter();
            PlateSolveImageProperties properties = CreateImageProperties(hinted, width: 300, height: 200);
            PlateSolveParameter blind = CreateParameter();
            blind.Coordinates = null;

            string hintedArgs = solver.Arguments(@"C:\Images\light.fit", @"C:\Images\light.wcs", hinted, properties);
            string blindArgs = solver.Arguments(@"C:\Images\light.fit", @"C:\Images\light.wcs", blind, properties);

            hintedArgs.Should().StartWith("/C \"\"");
            hintedArgs.Should().Contain(@"C:\cygwin64\bin\bash.exe");
            hintedArgs.Should().Contain("/usr/bin/solve-field");
            hintedArgs.Should().Contain("--overwrite");
            hintedArgs.Should().Contain("--objs 150");
            hintedArgs.Should().Contain("--downsample 2");
            hintedArgs.Should().Contain("--scale-units arcsecperpix");
            hintedArgs.Should().Contain("--ra 187.50");
            hintedArgs.Should().Contain("--dec -22.25");
            hintedArgs.Should().Contain("--radius 5.00");
            hintedArgs.Should().Contain("\"C:/Images/light.fit\"");
            blindArgs.Should().NotContain("--ra");
            blindArgs.Should().NotContain("--dec");
            blindArgs.Should().NotContain("--radius");
        }

        private static PlateSolveParameter CreateParameter(Coordinates coordinates = null, int downSampleFactor = 2) {
            return new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76,
                Binning = 1,
                SearchRadius = 5,
                Regions = 99,
                DownSampleFactor = downSampleFactor,
                MaxObjects = 150,
                DisableNotifications = true,
                Coordinates = coordinates ?? new Coordinates(Angle.ByDegree(187.5), Angle.ByDegree(-22.25), Epoch.J2000)
            };
        }

        private static PlateSolveImageProperties CreateImageProperties(PlateSolveParameter parameter, int width, int height) {
            var imageData = new Mock<IImageData>();
            imageData.SetupGet(x => x.Properties).Returns(new ImageProperties(width, height, 16, false, 0, 0));
            return PlateSolveImageProperties.Create(parameter, imageData.Object);
        }

        private static string CreateTempDirectory() {
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PlateSolverTranslation", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private sealed class TestableASTAPSolver : ASTAPSolver {
            public TestableASTAPSolver(string executableLocation) : base(executableLocation) {
            }

            public string Arguments(string imageFilePath, string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                GetArguments(imageFilePath, outputFilePath, parameter, imageProperties);

            public PlateSolveResult Result(string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                ReadResult(outputFilePath, parameter, imageProperties);

            public void Validate(PlateSolveParameter parameter) => EnsureSolverValid(parameter);
        }

        private sealed class TestablePlatesolve2Solver : Platesolve2Solver {
            public TestablePlatesolve2Solver(string executableLocation) : base(executableLocation) {
            }

            public string Arguments(string imageFilePath, string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                GetArguments(imageFilePath, outputFilePath, parameter, imageProperties);

            public PlateSolveResult Result(string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                ReadResult(outputFilePath, parameter, imageProperties);
        }

        private sealed class TestablePlatesolve3Solver : Platesolve3Solver {
            public TestablePlatesolve3Solver(string executableLocation) : base(executableLocation) {
            }

            public string Arguments(string imageFilePath, string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                GetArguments(imageFilePath, outputFilePath, parameter, imageProperties);

            public PlateSolveResult Result(string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                ReadResult(outputFilePath, parameter, imageProperties);
        }

        private sealed class TestableAllSkyPlateSolver : AllSkyPlateSolver {
            public TestableAllSkyPlateSolver(string executableLocation) : base(executableLocation) {
            }

            public string Arguments(string imageFilePath, string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                GetArguments(imageFilePath, outputFilePath, parameter, imageProperties);

            public PlateSolveResult Result(string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                ReadResult(outputFilePath, parameter, imageProperties);
        }

        private sealed class TestableLocalPlateSolver : LocalPlateSolver {
            public TestableLocalPlateSolver(string cygwinRoot) : base(cygwinRoot) {
            }

            public string Arguments(string imageFilePath, string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) =>
                GetArguments(imageFilePath, outputFilePath, parameter, imageProperties);
        }
    }
}
