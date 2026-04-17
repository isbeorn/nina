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
using Newtonsoft.Json.Linq;
using NINA.PlateSolving;
using NINA.PlateSolving.Solvers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class AstrometryPlateSolverBehaviorTest {

        /// <summary>
        /// Verifies Astrometry.net configuration validation rejects missing URLs, missing API keys, and copied keys containing whitespace.
        /// </summary>
        [Test]
        public void EnsureSolverValid_RejectsIncompleteOrWhitespaceConfiguration() {
            Action missingUrl = () => new TestableAstrometryPlateSolver("", "apikey").Validate(new PlateSolveParameter());
            Action missingKey = () => new TestableAstrometryPlateSolver("https://nova.astrometry.net", "").Validate(new PlateSolveParameter());
            Action whitespaceKey = () => new TestableAstrometryPlateSolver("https://nova.astrometry.net", "api key").Validate(new PlateSolveParameter());
            Action valid = () => new TestableAstrometryPlateSolver("https://nova.astrometry.net", "apikey").Validate(new PlateSolveParameter());

            missingUrl.Should().Throw<ArgumentException>().WithMessage("*URL*");
            missingKey.Should().Throw<ArgumentException>().WithMessage("*API key*");
            whitespaceKey.Should().Throw<ArgumentException>().WithMessage("*space*");
            valid.Should().NotThrow();
        }

        /// <summary>
        /// Verifies Astrometry.net failed responses preserve the distinction between normal solve failures, server errors, and unknown statuses.
        /// </summary>
        [Test]
        public void AstrometryNetFailedException_UsesStatusSpecificMessages() {
            var failure = new AstrometryPlateSolver.AstrometryNetFailedException("Job 42", JObject.Parse("""{"status":"failure"}"""));
            var error = new AstrometryPlateSolver.AstrometryNetFailedException("Authentication", JObject.Parse("""{"status":"error","errormessage":"bad key"}"""));
            var unknown = new AstrometryPlateSolver.AstrometryNetFailedException("Submission", JObject.Parse("""{"status":"queued"}"""));

            failure.Message.Should().Be("Job 42 failed to solve");
            error.Message.Should().Be("bad key");
            unknown.Message.Should().Be("Unspecified error");
        }

        /// <summary>
        /// Verifies a complete Astrometry.net success workflow maps authentication, upload, submission, job status, and calibration responses into a solved result.
        /// </summary>
        [Test]
        public async Task SolveAsync_UsesAstrometryNetWorkflowAndMapsCalibrationResult() {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Task<List<string>> server = ServeAstrometryResponses(listener);
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var solver = new TestableAstrometryPlateSolver($"http://127.0.0.1:{port}", "apikey");
            IImageData imageData = CreateImageData();
            var statuses = new List<ApplicationStatus>();
            var progress = new RecordingProgress<ApplicationStatus>(statuses);
            var parameter = new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76,
                DisableNotifications = true
            };

            PlateSolveResult result = await solver.SolveAsync(imageData, parameter, progress, CancellationToken.None);
            List<string> requests = await server.WaitAsync(TimeSpan.FromSeconds(5));

            result.Success.Should().BeTrue();
            result.Flipped.Should().BeTrue();
            result.PositionAngle.Should().BeApproximately(192.5, 1e-10);
            result.Pixscale.Should().BeApproximately(0.97, 1e-10);
            result.Radius.Should().BeApproximately(1.2, 1e-10);
            result.Coordinates.RADegrees.Should().BeApproximately(187.5, 1e-10);
            result.Coordinates.Dec.Should().BeApproximately(-22.25, 1e-10);
            requests.Should().Contain(x => x.StartsWith("POST /api/login/ "));
            requests.Should().Contain(x => x.StartsWith("POST /api/upload "));
            requests.Should().Contain(x => x.StartsWith("GET /api/submissions/123 "));
            requests.Should().Contain(x => x.StartsWith("GET /api/jobs/456 "));
            requests.Should().Contain(x => x.StartsWith("GET /api/jobs/456/calibration/ "));
            statuses.Last().Status.Should().Be("Solved");
        }

        /// <summary>
        /// Verifies Astrometry.net job failures are treated as solve failures without throwing out of the solver orchestration path.
        /// </summary>
        [Test]
        public async Task SolveAsync_JobFailureReturnsFailedSolveAndReportsFailureStatus() {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Task<List<string>> server = ServeAstrometryResponses(listener, jobFails: true);
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var solver = new TestableAstrometryPlateSolver($"http://127.0.0.1:{port}", "apikey");
            var statuses = new List<ApplicationStatus>();
            var progress = new RecordingProgress<ApplicationStatus>(statuses);
            var parameter = new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76,
                DisableNotifications = true
            };

            PlateSolveResult result = await solver.SolveAsync(CreateImageData(), parameter, progress, CancellationToken.None);
            List<string> requests = await server.WaitAsync(TimeSpan.FromSeconds(5));

            result.Success.Should().BeFalse();
            result.Coordinates.Should().BeNull();
            requests.Should().Contain(x => x.StartsWith("GET /api/jobs/456 "));
            requests.Should().NotContain(x => x.StartsWith("GET /api/jobs/456/calibration/ "));
            statuses.Last().Status.Should().Be("Solve failed");
        }

        private static async Task<List<string>> ServeAstrometryResponses(TcpListener listener, bool jobFails = false) {
            var requests = new List<string>();
            int expectedRequests = jobFails ? 4 : 5;
            for (int i = 0; i < expectedRequests; i++) {
                using TcpClient client = await listener.AcceptTcpClientAsync();
                await using NetworkStream stream = client.GetStream();
                string request = await ReadHttpRequest(stream);
                requests.Add(request);
                string path = request.Split(' ')[1];
                string response = path switch {
                    "/api/login/" => """{"status":"success","session":"session1"}""",
                    "/api/upload" => """{"status":"success","subid":123}""",
                    "/api/submissions/123" => """{"jobs":["456"]}""",
                    "/api/jobs/456" => jobFails ? """{"status":"failure"}""" : """{"status":"success"}""",
                    "/api/jobs/456/calibration/" => """{"parity":-1,"orientation":12.5,"pixscale":0.97,"radius":1.2,"ra":187.5,"dec":-22.25}""",
                    _ => """{"status":"error","errormessage":"unexpected path"}"""
                };
                byte[] body = Encoding.UTF8.GetBytes(response);
                string header = $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
                byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                await stream.WriteAsync(headerBytes);
                await stream.WriteAsync(body);
            }
            return requests;
        }

        private static async Task<string> ReadHttpRequest(NetworkStream stream) {
            var buffer = new byte[8192];
            using var request = new MemoryStream();
            int headerLength = -1;
            int contentLength = 0;
            while (true) {
                int bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0) {
                    break;
                }
                request.Write(buffer, 0, bytesRead);
                string text = Encoding.UTF8.GetString(request.ToArray());
                if (headerLength < 0) {
                    int headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (headerEnd >= 0) {
                        headerLength = headerEnd + 4;
                        foreach (string line in text[..headerEnd].Split("\r\n")) {
                            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) {
                                contentLength = int.Parse(line.Split(':')[1].Trim());
                            }
                        }
                    }
                }
                if (headerLength >= 0 && request.Length >= headerLength + contentLength) {
                    return text;
                }
            }
            return Encoding.UTF8.GetString(request.ToArray());
        }

        private static IImageData CreateImageData() {
            var metadata = new ImageMetaData();
            metadata.Telescope.Coordinates = new Coordinates(Angle.ByDegree(187.5), Angle.ByDegree(-22.25), Epoch.J2000);

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

        private sealed class TestableAstrometryPlateSolver : AstrometryPlateSolver {
            public TestableAstrometryPlateSolver(string apiurl, string apikey) : base(apiurl, apikey) {
                string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "AstrometryPlateSolver", Guid.NewGuid().ToString("N"));
                WORKING_DIRECTORY = Path.Combine(root, "Working");
                FAILED_DIRECTORY = Path.Combine(root, "Failed");
                FAILED_FILENAME = Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(WORKING_DIRECTORY);
                Directory.CreateDirectory(FAILED_DIRECTORY);
            }

            public void Validate(PlateSolveParameter parameter) => EnsureSolverValid(parameter);
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
