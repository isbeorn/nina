#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NINA.Test.Plugin {

    internal sealed class LoopbackHttpServer : IDisposable {
        private readonly TcpListener listener;
        private readonly Task serverTask;
        private readonly IReadOnlyList<Response> responses;
        private readonly string contentType;
        private readonly string? fileName;
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> requests = new();
        private readonly TaskCompletionSource requestReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public LoopbackHttpServer(byte[] body, string contentType = "application/octet-stream", string? fileName = null, Task? responseGate = null, int statusCode = 200)
            : this([new Response(body, statusCode, responseGate)], contentType, fileName) {
        }

        public LoopbackHttpServer(IReadOnlyList<Response> responses, string contentType = "application/json", string? fileName = null) {
            this.responses = responses;
            this.contentType = contentType;
            this.fileName = fileName;
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Url = $"http://127.0.0.1:{port}";
            serverTask = Task.Run(ServeRequests);
        }

        public string Url { get; }

        public Task RequestReceived => requestReceived.Task;

        public IReadOnlyList<string> Requests => requests.ToArray();

        public sealed record Response(byte[] Body, int StatusCode = 200, Task? Gate = null);

        public void Dispose() {
            listener.Stop();
            try {
                serverTask.Wait(TimeSpan.FromSeconds(5));
            } catch {
            }
        }

        private async Task ServeRequests() {
            try {
                foreach (var response in responses) {
                    using TcpClient client = await listener.AcceptTcpClientAsync();
                    await using NetworkStream stream = client.GetStream();
                    requests.Enqueue(await ReadHeaders(stream));
                    requestReceived.TrySetResult();
                    if (response.Gate != null) {
                        await response.Gate;
                    }

                    string contentDisposition = string.IsNullOrEmpty(fileName) ? string.Empty : $"Content-Disposition: attachment; filename=\"{fileName}\"\r\n";
                    string headers =
                        $"HTTP/1.1 {response.StatusCode} Test response\r\n" +
                        $"Content-Length: {response.Body.Length}\r\n" +
                        $"Content-Type: {contentType}\r\n" +
                        contentDisposition +
                        "Connection: close\r\n" +
                        "\r\n";
                    byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
                    try {
                        await stream.WriteAsync(headerBytes);
                        await stream.WriteAsync(response.Body);
                    } catch (IOException) {
                        // A canceled request can close its connection while later requests still need a response.
                    }
                }
            } finally {
                listener.Stop();
            }
        }

        private static async Task<string> ReadHeaders(NetworkStream stream) {
            var buffer = new byte[1];
            var headers = new StringBuilder();
            var recentBytes = new Queue<byte>(4);
            while (await stream.ReadAsync(buffer.AsMemory(0, 1)) == 1) {
                headers.Append((char)buffer[0]);
                recentBytes.Enqueue(buffer[0]);
                while (recentBytes.Count > 4) {
                    recentBytes.Dequeue();
                }
                if (recentBytes.Count == 4 && recentBytes.SequenceEqual(new byte[] { 13, 10, 13, 10 })) {
                    string? contentLength = headers.ToString().Split("\r\n")
                        .FirstOrDefault(line => line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                    if (contentLength != null) {
                        int remaining = int.Parse(contentLength.Split(':', 2)[1].Trim());
                        var bodyBuffer = new byte[4096];
                        while (remaining > 0) {
                            int read = await stream.ReadAsync(bodyBuffer.AsMemory(0, Math.Min(remaining, bodyBuffer.Length)));
                            if (read == 0) throw new EndOfStreamException();
                            remaining -= read;
                        }
                    }
                    return headers.ToString();
                }
            }

            return headers.ToString();
        }
    }
}
