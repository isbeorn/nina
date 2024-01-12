#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyGuider.MetaGuide {

    public delegate void OnCameraDelegate(MetaGuideCameraMsg msg);

    public delegate void OnStatusDelegate(MetaGuideStatusMsg msg);

    public delegate void OnGuideDelegate(MetaGuideGuideMsg msg);

    public delegate void OnGuideParamsDelegate(MetaGuideGuideParamsMsg msg);

    public delegate void OnCalibrationInfoDelegate(MetaGuideCalibrationInfoMsg msg);

    public delegate void OnMountDelegate(MetaGuideMountMsg msg);

    public delegate void OnDisconnectedDelegate();

    public class MetaGuideListener {

        public event OnCameraDelegate OnCamera;

        public event OnStatusDelegate OnStatus;

        public event OnGuideDelegate OnGuide;

        public event OnGuideParamsDelegate OnGuideParams;

        public event OnCalibrationInfoDelegate OnCalibrationInfo;

        public event OnMountDelegate OnMount;

        public event OnDisconnectedDelegate OnDisconnected;

        private const int METAGUIDE_QUEUE_TIMEOUT_MS = 5000;

        public MetaGuideListener() {
        }

        private void ProcessMessage(string[] splitMessage) {
            try {
                if (splitMessage[0] == "OPENSCI" && splitMessage[1] == "ASTRO" && splitMessage[3] == "MG") {
                    string type = splitMessage[2];
                    switch (type) {
                        case "CAMERA": {
                                var parsedMessage = MetaGuideCameraMsg.Create(splitMessage);
                                if (parsedMessage != null) {
                                    this.OnCamera?.Invoke(parsedMessage);
                                }
                            }
                            break;

                        case "STATUS": {
                                var parsedMessage = MetaGuideStatusMsg.Create(splitMessage);
                                if (parsedMessage != null) {
                                    this.OnStatus?.Invoke(parsedMessage);
                                }
                            }
                            break;

                        case "GUIDE": {
                                var parsedMessage = MetaGuideGuideMsg.Create(splitMessage);
                                if (parsedMessage != null) {
                                    this.OnGuide?.Invoke(parsedMessage);
                                }
                            }
                            break;

                        case "GUIDEPARMS": {
                                var parsedMessage = MetaGuideGuideParamsMsg.Create(splitMessage);
                                if (parsedMessage != null) {
                                    this.OnGuideParams?.Invoke(parsedMessage);
                                }
                            }
                            break;

                        case "CALINFO": {
                                var parsedMessage = MetaGuideCalibrationInfoMsg.Create(splitMessage);
                                if (parsedMessage != null) {
                                    this.OnCalibrationInfo?.Invoke(parsedMessage);
                                }
                            }
                            break;

                        case "MOUNTNAME": {
                                var parsedMessage = MetaGuideMountMsg.Create(splitMessage);
                                if (parsedMessage != null) {
                                    this.OnMount?.Invoke(parsedMessage);
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError("MetaGuide Listener Error: " + ex.Message);
            }
        }

        private async Task RunConsumer(
            AsyncProducerConsumerQueue<string[]> messageQueue,
            CancellationToken cancellationToken) {
            await Task.Run(async () => {
                try {
                    while (!cancellationToken.IsCancellationRequested) {
                        string[] message = await messageQueue.DequeueAsync(cancellationToken);
                        ProcessMessage(message);
                    }
                } catch (OperationCanceledException) {
                }
            });
        }

        public async Task RunListener(
            bool useIpAddressAny,
            int port,
            CancellationToken ct) {
            await Task.Run(async () => {
                Task consumerTask = null;
                Socket socket = null;
                var consumerTokenSource = new CancellationTokenSource();

                try {
                    var messageQueue = new AsyncProducerConsumerQueue<string[]>(200);
                    // Run consumer on a separate, concurrent task to keep up with UDP messages from MetaGuide
                    consumerTask = RunConsumer(messageQueue, CancellationTokenSource.CreateLinkedTokenSource(ct, consumerTokenSource.Token).Token);

                    var listenAddr = new IPEndPoint(useIpAddressAny ? IPAddress.Any : IPAddress.Loopback, port);
                    EndPoint remoteEndpoint = listenAddr;
                    var receiveBytes = new byte[1024];

                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
                        EnableBroadcast = true,
                        ReceiveTimeout = METAGUIDE_QUEUE_TIMEOUT_MS
                    };

                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    socket.Bind(listenAddr);

                    while (!ct.IsCancellationRequested) {
                        var bytesReceived = socket.ReceiveFrom(receiveBytes, ref remoteEndpoint);
                        var rawMessage = Encoding.UTF8.GetString(receiveBytes, 0, bytesReceived);
                        var splitMessage = rawMessage.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(METAGUIDE_QUEUE_TIMEOUT_MS));
                        var timeoutOrCancelledTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutTokenSource.Token);

                        using (timeoutTokenSource.Token.Register(() => Logger.Error($"MetaGuide queue full"))) {
                            await messageQueue.EnqueueAsync(splitMessage, timeoutOrCancelledTokenSource.Token);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                    throw;
                } finally {
                    socket?.Close();
                    try { consumerTokenSource?.Cancel(); } catch { }
                    consumerTask?.WaitWithoutException(new CancellationTokenSource(METAGUIDE_QUEUE_TIMEOUT_MS).Token);
                    this.OnDisconnected?.Invoke();
                }
            }, ct);
        }
    }
}