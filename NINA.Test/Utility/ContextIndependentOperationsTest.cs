#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Core.Utility.TcpRaw;
using NINA.Test.Plugin;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Test.Utility {
    [TestFixture]
    public class ContextIndependentOperationsTest {
        [TestCase("get")]
        [TestCase("post")]
        [TestCase("upload")]
        [TestCase("image")]
        public async Task Http_DeferredResponse_CompletesWithoutCallerDispatch(string kind) {
            var context = new RecordingContext();
            var responseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            byte[] payload = kind == "image" ? CreatePng() : Encoding.UTF8.GetBytes("response");
            using var server = new LoopbackHttpServer(payload, responseGate: responseGate.Task);
            string uploadPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"upload-{Guid.NewGuid():N}.txt");
            FileStream? upload = null;
            try {
                if (kind == "upload") {
                    File.WriteAllText(uploadPath, "file contents");
                    upload = File.OpenRead(uploadPath);
                }
                Task operation = context.Start(() => kind switch {
                    "get" => (Task)new HttpGetRequest(server.Url).Request(CancellationToken.None),
                    "post" => new HttpPostRequest(server.Url, "request", "text/plain").Request(CancellationToken.None),
                    "upload" => new HttpUploadFile(server.Url, upload!, "file", "text/plain", new NameValueCollection()).Request(CancellationToken.None),
                    _ => new HttpDownloadImageRequest(server.Url).Request(CancellationToken.None)
                });
                Assert.That(operation.IsCompleted, Is.False, "the server has not released its response");
                responseGate.SetResult();
                await operation.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.That(context.PostCount, Is.Zero, "transport completion must not depend on dispatching to the caller");
                if (kind == "image") {
                    BitmapSource image = await (Task<BitmapSource>)operation;
                    Assert.That(image.IsFrozen, Is.True);
                    var pixel = new byte[1];
                    image.CopyPixels(pixel, 1, 0);
                    Assert.That(pixel[0], Is.EqualTo(42), "the downloaded image must remain usable from the caller thread");
                } else {
                    Assert.That(await (Task<string>)operation, Is.EqualTo("response"));
                }
            } finally {
                responseGate.TrySetResult();
                upload?.Dispose();
                if (File.Exists(uploadPath)) File.Delete(uploadPath);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task HttpGet_DeferredFailure_PreservesErrorPolicyWithoutCallerDispatch(bool rethrow) {
            var context = new RecordingContext();
            var responseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = new LoopbackHttpServer(Array.Empty<byte>(), responseGate: responseGate.Task, statusCode: 500);
            Task<string> operation = context.Start(() => new HttpGetRequest(server.Url, rethrow).Request(CancellationToken.None));
            responseGate.SetResult();

            if (rethrow) {
                Assert.ThrowsAsync<HttpRequestException>(async () => await operation.WaitAsync(TimeSpan.FromSeconds(5)));
            } else {
                Assert.That(await operation.WaitAsync(TimeSpan.FromSeconds(5)), Is.Empty);
            }
            Assert.That(context.PostCount, Is.Zero);
        }

        [Test]
        public async Task HttpGet_Cancellation_CompletesWithoutCallerDispatch() {
            var context = new RecordingContext();
            var responseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = new LoopbackHttpServer(Array.Empty<byte>(), responseGate: responseGate.Task);
            using var cancellation = new CancellationTokenSource();
            Task<string> operation = context.Start(() => new HttpGetRequest(server.Url).Request(cancellation.Token));
            try {
                cancellation.Cancel();
                try {
                    await operation.WaitAsync(TimeSpan.FromSeconds(5));
                    Assert.Fail("the request must remain canceled");
                } catch (OperationCanceledException) {
                    Assert.That(operation.IsCanceled, Is.True);
                }
                Assert.That(context.PostCount, Is.Zero);
            } finally {
                responseGate.TrySetResult();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task TcpQuery_DeferredReplyOrCancellation_DoesNotRequireCallerDispatch(bool cancel) {
            var context = new RecordingContext();
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using var cancellation = new CancellationTokenSource();
            var query = new BasicQuery("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port, "query", "ready");
            Task<string> operation = context.Start(() => query.SendQuery(cancellation.Token));
            try {
                using TcpClient client = await listener.AcceptTcpClientAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                using NetworkStream stream = client.GetStream();
                await stream.WriteAsync(Encoding.ASCII.GetBytes("ready"));
                var command = new byte[5];
                await stream.ReadExactlyAsync(command).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                Assert.That(Encoding.ASCII.GetString(command), Is.EqualTo("query"));
                Assert.That(operation.IsCompleted, Is.False);
                if (cancel) {
                    cancellation.Cancel();
                    Assert.CatchAsync<OperationCanceledException>(async () => await operation.WaitAsync(TimeSpan.FromSeconds(5)));
                    Assert.That(operation.IsCanceled, Is.True);
                } else {
                    await stream.WriteAsync(Encoding.ASCII.GetBytes("reply"));
                    Assert.That(await operation.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo("reply"));
                }
                Assert.That(context.PostCount, Is.Zero);
            } finally {
                cancellation.Cancel();
                await Task.WhenAny(operation, Task.Delay(TimeSpan.FromSeconds(5)));
                _ = operation.Exception;
            }
        }

        [Test]
        public async Task DeviceTimer_StopAndCanceledWait_CompleteWithoutCallerDispatch() {
            var context = new RecordingContext();
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var timer = new DeviceUpdateTimer(() => {
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
                return new Dictionary<string, object> { ["Connected"] = true };
            }, values => {
                if (!(bool)values["Connected"]) disconnected.TrySetResult();
            }, 0.01);
            Task run = context.Start(timer.Run);
            try {
                Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
                using var cancellation = new CancellationTokenSource();
                Task wait = context.Start(() => timer.WaitForNextUpdate(cancellation.Token));
                cancellation.Cancel();
                Assert.ThrowsAsync<TaskCanceledException>(async () => await wait.WaitAsync(TimeSpan.FromSeconds(5)));
                Task stop = context.Start(timer.Stop);
                release.Set();
                await Task.WhenAll(run, stop, disconnected.Task).WaitAsync(TimeSpan.FromSeconds(5));
                Assert.That(context.PostCount, Is.Zero);
            } finally {
                release.Set();
                await timer.Stop().WaitAsync(TimeSpan.FromSeconds(5));
                await run.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        private static byte[] CreatePng() {
            var source = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Gray8, null, new byte[] { 42 }, 1);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        private sealed class RecordingContext : SynchronizationContext {
            private int postCount;
            public int PostCount => Volatile.Read(ref postCount);

            public T Start<T>(Func<T> action) {
                SynchronizationContext? previous = Current;
                SetSynchronizationContext(this);
                try {
                    return action();
                } finally {
                    SetSynchronizationContext(previous);
                }
            }

            public override void Post(SendOrPostCallback callback, object? state) {
                Interlocked.Increment(ref postCount);
                // Drain on a worker so a regression is reported instead of hanging the test process.
                ThreadPool.QueueUserWorkItem(_ => callback(state));
            }
        }
    }
}
