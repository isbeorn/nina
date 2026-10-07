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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Windows.Threading;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class TheSkyXImageLinkSolverBehaviorTest {

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        [NonParallelizable]
        public async Task SolveAsync_ServerIsWaiting_KeepsDispatcherResponsiveAndProgressOnCaller(bool delayedSave, bool serverFails) {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var responses = serverFails
                ? new[] { "TypeError: Failed solve Error = 12|No Error" }
                : new[] { "0|No Error", "{\"succeeded\":true,\"imageScale\":0.97,\"imageCenterRAJ2000\":12.5,\"imageCenterDecJ2000\":-22.25}|No Error" };
            var received = responses.Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
            var releaseResponse = responses.Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
            var saveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<List<string>> server = ServeResponses(listener, async index => {
                received[index].SetResult();
                await releaseResponse[index].Task;
            }, responses);
            var solver = new TestableTheSkyXImageLinkSolver("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port);
            IImageData source = CreateImageData("ResponsiveDispatcher", delayedSave ? releaseSave.Task : null, () => saveStarted.SetResult());
            var parameter = new PlateSolveParameter { FocalLength = 600, PixelSize = 3.76, DisableNotifications = true };
            var progressThreads = new ConcurrentQueue<int>();
            var progress = new CallbackProgress<ApplicationStatus>(_ => progressThreads.Enqueue(Environment.CurrentManagedThreadId));
            var dispatcherReady = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() => {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                dispatcherReady.SetResult(dispatcher);
                Dispatcher.Run();
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Dispatcher caller = await dispatcherReady.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Task<PlateSolveResult> solve = caller.InvokeAsync(() => solver.SolveAsync(source, parameter, progress, CancellationToken.None)).Task.Unwrap();
            var responsive = new List<bool>();

            try {
                await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                releaseSave.TrySetResult();
                for (int index = 0; index < responses.Length; index++) {
                    await received[index].Task.WaitAsync(TimeSpan.FromSeconds(5));
                    Task heartbeat = caller.InvokeAsync(() => { }).Task;
                    responsive.Add(await Task.WhenAny(heartbeat, Task.Delay(TimeSpan.FromSeconds(2))) == heartbeat);
                    releaseResponse[index].TrySetResult();
                }
                PlateSolveResult result = await solve.WaitAsync(TimeSpan.FromSeconds(5));
                await server.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.That(responsive, Is.All.True, "socket exchanges must not block the caller's dispatcher, even when saving completed synchronously");
                Assert.That(result.Success, Is.EqualTo(!serverFails));
                Assert.That(progressThreads, Is.Not.Empty.And.All.EqualTo(thread.ManagedThreadId), "custom progress callbacks must retain their calling context");
                Assert.That(Directory.GetFiles(solver.WorkingDirectory), Is.Empty);
            } finally {
                releaseSave.TrySetResult();
                foreach (var response in releaseResponse) response.TrySetResult();
                try {
                    await solve.WaitAsync(TimeSpan.FromSeconds(5));
                    await server.WaitAsync(TimeSpan.FromSeconds(5));
                } finally {
                    caller.BeginInvokeShutdown(DispatcherPriority.Send);
                    Assert.That(thread.Join(TimeSpan.FromSeconds(5)), Is.True, "the test dispatcher must shut down");
                }
            }
        }

        /// <summary>
        /// Verifies TheSkyX ImageLink solver sends solve/result scripts, parses a successful result, computes radius, and cleans the temporary FITS file.
        /// </summary>
        [Test]
        public async Task SolveAsync_MapsSuccessfulImageLinkResultAndCleansTemporaryImage() {
            string resultJson = """
                {"errorCode":0,"succeeded":true,"searchAborted":false,"errorText":"","imageScale":0.97,"imagePositionAngle":123.4,"imageCenterRAJ2000":12.5,"imageCenterDecJ2000":-22.25,"imageSize":{"width":300,"height":200},"imageIsMirrored":true,"imageFilePath":"C:/Images/light.fit","imageStarCount":42,"imageFWHMInArcSeconds":3.1,"solutionRMS":0.4,"solutionRMSX":0.2,"solutionRMSY":0.3,"solutionStarCount":30,"catalogStarCount":120}|No Error
                """;
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Task<List<string>> server = ServeResponses(listener, "0|No Error", resultJson.Trim());
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var solver = new TestableTheSkyXImageLinkSolver("127.0.0.1", port);
            IImageData imageData = CreateImageData("TheSkyXSuccess");
            var statuses = new List<ApplicationStatus>();
            var progress = new RecordingProgress<ApplicationStatus>(statuses);
            var parameter = new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76,
                DisableNotifications = true,
                Coordinates = new Coordinates(Angle.ByDegree(187.5), Angle.ByDegree(-22.25), Epoch.J2000)
            };

            PlateSolveResult result = await solver.SolveAsync(imageData, parameter, progress, CancellationToken.None);
            List<string> requests = await server.WaitAsync(TimeSpan.FromSeconds(5));

            result.Success.Should().BeTrue();
            result.Flipped.Should().BeTrue();
            result.PositionAngle.Should().BeApproximately(123.4, 1e-10);
            result.Pixscale.Should().BeApproximately(0.97, 1e-10);
            result.Radius.Should().BeGreaterThan(0);
            result.Coordinates.RA.Should().BeApproximately(12.5, 1e-10);
            result.Coordinates.Dec.Should().BeApproximately(-22.25, 1e-10);
            requests[0].Should().Contain("ImageLink.execute();");
            requests[1].Should().Contain("JSON.stringify(objResult)");
            Directory.GetFiles(solver.WorkingDirectory).Should().BeEmpty();
            statuses.Should().Contain(x => x.Status == "Plate solve completed.");
        }

        private static Task<List<string>> ServeResponses(TcpListener listener, params string[] responses) {
            return ServeResponses(listener, _ => Task.CompletedTask, responses);
        }

        private static async Task<List<string>> ServeResponses(TcpListener listener, Func<int, Task> beforeResponse, params string[] responses) {
            var requests = new List<string>();
            foreach (string response in responses) {
                using TcpClient client = await listener.AcceptTcpClientAsync();
                await using NetworkStream stream = client.GetStream();
                var buffer = new byte[8192];
                int bytesRead = await stream.ReadAsync(buffer);
                requests.Add(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                await beforeResponse(requests.Count - 1);
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes);
            }
            return requests;
        }

        private static IImageData CreateImageData(string targetName, Task? saveGate = null, Action? onSave = null) {
            var metadata = new ImageMetaData();
            metadata.Target.Name = targetName;
            metadata.Telescope.Coordinates = new Coordinates(Angle.ByDegree(187.5), Angle.ByDegree(-22.25), Epoch.J2000);

            var imageData = new Mock<IImageData>();
            imageData.SetupGet(x => x.MetaData).Returns(metadata);
            imageData.SetupGet(x => x.Properties).Returns(new ImageProperties(300, 200, 16, false, 0, 0));
            imageData
                .Setup(x => x.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), true))
                .Returns(async (FileSaveInfo fileSaveInfo, CancellationToken _, bool _) => {
                    Directory.CreateDirectory(fileSaveInfo.FilePath);
                    string path = Path.Combine(fileSaveInfo.FilePath, $"{Guid.NewGuid():N}.fit");
                    File.WriteAllText(path, "synthetic image");
                    onSave?.Invoke();
                    if (saveGate != null) await saveGate;
                    return path;
                });
            return imageData.Object;
        }

        private sealed class TestableTheSkyXImageLinkSolver : TheSkyXImageLinkSolver {
            public TestableTheSkyXImageLinkSolver(string tsxHost, int tsxPort) : base(tsxHost, tsxPort) {
                string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TheSkyXImageLinkSolver", Guid.NewGuid().ToString("N"));
                WORKING_DIRECTORY = Path.Combine(root, "Working");
                FAILED_DIRECTORY = Path.Combine(root, "Failed");
                FAILED_FILENAME = Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(WORKING_DIRECTORY);
                Directory.CreateDirectory(FAILED_DIRECTORY);
            }

            public string WorkingDirectory => WORKING_DIRECTORY;
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

        private sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T> {
            public void Report(T value) => callback(value);
        }
    }
}
