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

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NINA.Utility.Api {

    internal class RawTcp : ITcpIpApi {
        protected string baseAddress = "localhost";
        protected int port = 8080;
        protected int timeout = 1000;
        protected Socket sock;
        private static String response = String.Empty;

        public void Disconnect() {
            try {
                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
                sock.Disconnect(true);
            } catch (Exception e) {
                //do nothing
            }
        }

        private bool ValidateIPv4(string ipString) {
            if (String.IsNullOrWhiteSpace(ipString)) {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4) {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        /// <summary>
        /// Asynchronous conenctions, see
        /// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-client-socket-example
        /// https://csharp.hotexamples.com/examples/-/Socket/ReceiveAsync/php-socket-receiveasync-method-examples.html
        /// </summary>
        /// <returns></returns>
        public bool Connect() {
            if (IsConnected) return true;
            try {
                IPAddress ipAddr;
                if (ValidateIPv4(this.baseAddress)) {
                    byte[] x = new byte[4];
                    string[] splits = this.baseAddress.Split('.');
                    for (int i = 0; i < 4; i++)
                        x[i] = Byte.Parse(splits[i]);
                    ipAddr = new IPAddress(x);
                } else {
                    IPHostEntry ipHost = Dns.GetHostEntry(this.baseAddress);
                    ipAddr = ipHost.AddressList[0];
                    bool f = true;
                    int i = 0;
                    while (f && (i < ipHost.AddressList.Length)) {
                        if (ipHost.AddressList[i].AddressFamily == AddressFamily.InterNetwork) {
                            f = false;
                            ipAddr = ipHost.AddressList[i];
                        }
                        i++;
                    }
                }
                IPEndPoint endPoint = new IPEndPoint(ipAddr, this.port);

                this.sock = new Socket(ipAddr.AddressFamily,
                      SocketType.Stream, ProtocolType.Tcp);
                sock.ReceiveTimeout = this.timeout;
                sock.Connect(endPoint);
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
            return true;
        }

        public string RemoteEndpoint {
            get {
                try { return sock.RemoteEndPoint.ToString(); } catch (Exception ex) { return ""; }
            }
        }

        public bool IsConnected {
            get {
                try { return this.sock.Connected; } catch (Exception ex) { return false; }
            }
        }

        public async Task<string> SendTextCommand(string cmd) {
            string ret = "";
            TaskCompletionSource<bool> tcs = null;
            var args = new SocketAsyncEventArgs();
            args.Completed += (_, __) => tcs.SetResult(true);
            try {
                tcs = new TaskCompletionSource<bool>();
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                byte[] buffer = encoding.GetBytes(cmd);

                args.SetBuffer(buffer, 0, encoding.GetBytes(cmd).Length);

                if (sock.SendAsync(args)) await tcs.Task;
                else return ret;

                tcs = new TaskCompletionSource<bool>();
                args.SetBuffer(new byte[2048], 0, 2048);
                if (sock.ReceiveAsync(args)) await tcs.Task;
                if (args.BytesTransferred > 0) {
                    ret = encoding.GetString(args.Buffer, 0, args.BytesTransferred);
                }
            } catch (Exception ex) { Logger.Error(ex); }

            return ret;
        }

        string ITcpIpApi.BaseAddress {
            get {
                return baseAddress;
            }
            set {
                this.baseAddress = value;
            }
        }

        int ITcpIpApi.Port {
            get {
                return port;
            }
            set {
                port = value;
            }
        }

        int ITcpIpApi.Timeout {
            get {
                return timeout;
            }
            set {
                timeout = value;
            }
        }

        public string Protocol {
            get { return "RAW"; }
            set => throw new NotImplementedException();
        }

        public string RootPath {
            get { return ""; }
            set => throw new NotImplementedException();
        }
    }
}