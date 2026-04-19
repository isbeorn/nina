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
        private readonly string fileName;

        public LoopbackHttpServer(byte[] body, string contentType = "application/octet-stream", string fileName = null) {
            this.body = body;
            this.contentType = contentType;
            this.fileName = fileName;
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

                string contentDisposition = string.IsNullOrEmpty(fileName) ? string.Empty : $"Content-Disposition: attachment; filename=\"{fileName}\"\r\n";
                string headers =
                    "HTTP/1.1 200 OK\r\n" +
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
            var recentBytes = new Queue<byte>(4);
            while (await stream.ReadAsync(buffer.AsMemory(0, 1)) == 1) {
                recentBytes.Enqueue(buffer[0]);
                while (recentBytes.Count > 4) {
                    recentBytes.Dequeue();
                }
                if (recentBytes.Count == 4 && recentBytes.SequenceEqual(new byte[] { 13, 10, 13, 10 })) {
                    return;
                }
            }
        }
    }
}
