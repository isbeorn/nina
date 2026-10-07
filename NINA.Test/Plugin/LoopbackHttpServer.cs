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
        private readonly byte[] body;
        private readonly string contentType;
        private readonly string? fileName;
        private readonly Task? responseGate;
        private readonly int statusCode;

        public LoopbackHttpServer(byte[] body, string contentType = "application/octet-stream", string? fileName = null, Task? responseGate = null, int statusCode = 200) {
            this.body = body;
            this.contentType = contentType;
            this.fileName = fileName;
            this.responseGate = responseGate;
            this.statusCode = statusCode;
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Url = $"http://127.0.0.1:{port}";
            serverTask = Task.Run(ServeSingleRequest);
        }

        public string Url { get; }

        public void Dispose() {
            listener.Stop();
            try {
                serverTask.Wait(TimeSpan.FromSeconds(5));
            } catch {
            }
        }

        private async Task ServeSingleRequest() {
            try {
                using TcpClient client = await listener.AcceptTcpClientAsync();
                await using NetworkStream stream = client.GetStream();
                await ReadHeaders(stream);
                if (responseGate != null) await responseGate;

                string contentDisposition = string.IsNullOrEmpty(fileName) ? string.Empty : $"Content-Disposition: attachment; filename=\"{fileName}\"\r\n";
                string headers =
                    $"HTTP/1.1 {statusCode} Test response\r\n" +
                    $"Content-Length: {body.Length}\r\n" +
                    $"Content-Type: {contentType}\r\n" +
                    contentDisposition +
                    "Connection: close\r\n" +
                    "\r\n";
                byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
                await stream.WriteAsync(headerBytes);
                await stream.WriteAsync(body);
            } finally {
                listener.Stop();
            }
        }

        private static async Task ReadHeaders(NetworkStream stream) {
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
                    return;
                }
            }
        }
    }
}
