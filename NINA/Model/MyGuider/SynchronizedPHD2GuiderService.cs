using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    internal class SynchronizedPHD2GuiderService : ISynchronizedPHD2GuiderService {
        private IGuider guiderInstance;
        public List<SynchronizedClientInfo> ConnectedClients;
        public bool PHD2Connected { get; private set; } = false;

        public async Task<bool> Initialize(IGuider guider, CancellationToken ct) {
            guiderInstance = guider;
            ConnectedClients = new List<SynchronizedClientInfo>();
            PHD2Connected = await guiderInstance.Connect(ct);
            if (PHD2Connected) {
                ((PHD2Guider)guiderInstance).PHD2ConnectionLost += (sender, args) => PHD2Connected = false;
                Notification.ShowSuccess("Synchronized PHD2 Service started");
            }
            return PHD2Connected;
        }

        public double ConnectAndGetPixelScale(Guid clientId) {
            var existingInfo = ConnectedClients.SingleOrDefault(c => c.InstanceID == clientId);
            if (existingInfo != null) {
                ConnectedClients[ConnectedClients.IndexOf(existingInfo)].LastPing = DateTime.Now;
            } else {
                ConnectedClients.Add(new SynchronizedClientInfo { InstanceID = clientId, LastPing = DateTime.Now });
                Notification.ShowSuccess("Client connected to synchronized PHD2 service, connected clients: " + ConnectedClients.Count(c => c.IsAlive));
            }

            return guiderInstance.PixelScale;
        }

        public async Task<GuideInfo> GetGuideInfo(Guid clientId) {
            if (!PHD2Connected) {
                throw new FaultException<PHD2Fault>(new PHD2Fault());
            }

            var clientInfo = ConnectedClients.Single(c => c.InstanceID == clientId);
            clientInfo.LastPing = DateTime.Now;

            return new GuideInfo() {
                State = ((PHD2Guider)guiderInstance).AppState?.State,
                GuideStep = (PHD2Guider.PhdEventGuideStep)guiderInstance.GuideStep
            };
        }

        public async Task UpdateCameraInfo(ProfileCameraState profileCameraState) {
            var clientInfo = ConnectedClients.Single(c => c.InstanceID == profileCameraState.InstanceId);
            clientInfo.IsExposing = profileCameraState.IsExposing;
            clientInfo.NextExposureTime = profileCameraState.NextExposureTime;
            clientInfo.MaxWaitTime = profileCameraState.MaxWaitTime;
            clientInfo.ExposureEndTime = profileCameraState.ExposureEndTime;
        }

        /// <inheritdoc />
        public void DisconnectClient(Guid clientId) {
            ConnectedClients.RemoveAll(c => c.InstanceID == clientId);
        }

        private CancellationTokenSource startGuidingCancellationTokenSource;
        private readonly object startGuidingLock = new object();
        private Task<bool> startGuidingTask;

        /// <inheritdoc />
        public async Task<bool> StartGuiding() {
            lock (startGuidingLock) {
                if (startGuidingCancellationTokenSource == null) {
                    startGuidingCancellationTokenSource = new CancellationTokenSource();
                    startGuidingTask = StartGuidingTask();
                }
            }

            var result = await startGuidingTask;

            startGuidingCancellationTokenSource = null;
            return result;
        }

        private async Task<bool> StartGuidingTask() {
            var result = await guiderInstance.StartGuiding(startGuidingCancellationTokenSource.Token);
            return result;
        }

        public void CancelStartGuiding() {
            startGuidingCancellationTokenSource?.Cancel();
        }

        /// <inheritdoc />
        public Task<bool> AutoSelectGuideStar() {
            return guiderInstance.AutoSelectGuideStar();
        }

        private CancellationTokenSource startPauseCancellationTokenSource;
        private readonly object startPauseLock = new object();
        private Task<bool> startPauseTask;

        public async Task<bool> StartPause(bool pause) {
            lock (startPauseLock) {
                if (startPauseCancellationTokenSource == null) {
                    startPauseCancellationTokenSource = new CancellationTokenSource();
                    startPauseTask = StartPauseTask(pause);
                }
            }

            var result = await startPauseTask;

            startPauseCancellationTokenSource = null;
            return result;
        }

        private async Task<bool> StartPauseTask(bool pause) {
            var result = await guiderInstance.Pause(pause, startPauseCancellationTokenSource.Token);
            return result;
        }

        public void CancelStartPause() {
            startPauseCancellationTokenSource?.Cancel();
        }

        private CancellationTokenSource stopGuidingCancellationTokenSource;
        private readonly object stopGuidingLock = new object();
        private Task<bool> stopGuidingTask;

        public async Task<bool> StopGuiding() {
            lock (stopGuidingLock) {
                if (stopGuidingCancellationTokenSource == null) {
                    stopGuidingCancellationTokenSource = new CancellationTokenSource();
                    stopGuidingTask = StopGuidingTask();
                }
            }

            var result = await stopGuidingTask;

            stopGuidingCancellationTokenSource = null;
            return result;
        }

        private async Task<bool> StopGuidingTask() {
            var result = await guiderInstance.StopGuiding(stopGuidingCancellationTokenSource.Token);
            return result;
        }

        public void CancelStopGuiding() {
            stopGuidingCancellationTokenSource?.Cancel();
        }

        private CancellationTokenSource ditherCancellationTokenSource;
        private readonly object ditherLock = new object();
        private Task<bool> ditherTask;

        public async Task<bool> SynchronizedDither(Guid instanceId) {
            lock (ditherLock) {
                if (ditherCancellationTokenSource == null) {
                    ditherCancellationTokenSource = new CancellationTokenSource();
                }
            }

            var client = ConnectedClients.Single(c => c.InstanceID == instanceId);

            // no further exposures, just return
            if (client.NextExposureTime == -1) {
                return true;
            }

            var otherClientsExist = ConnectedClients.Any(c => c.IsAlive && c.InstanceID != client.InstanceID);

            // no other clients exist, just dither
            if (!otherClientsExist) {
                var output = await guiderInstance.Dither(ditherCancellationTokenSource.Token);
                ditherCancellationTokenSource = null;
                return output;
            }

            if (ConnectedClients.All(c => c.ExposureEndTime < DateTime.Now.AddSeconds(client.MaxWaitTime))) {
                // if all clients finish before our max waiting time we just continue
                // one client has to launch the dither task that will wait for all alive clients to dither
                lock (ditherLock) {
                    if (ditherTask == null) {
                        ditherTask = DitherTask();
                    }
                }

                client.IsWaitingForDither = true;

                var result = await ditherTask;

                client.IsWaitingForDither = false;

                lock (ditherLock) {
                    ditherCancellationTokenSource = null;
                    ditherTask = null;
                }

                return result;
            }

            if (ConnectedClients.Any(c =>
                c.InstanceID != client.InstanceID && c.IsExposing && c.IsAlive &&
                c.ExposureEndTime.AddSeconds(c.MaxWaitTime) >= DateTime.Now.AddSeconds(client.NextExposureTime))) {
                // squeeze in more exposures
                // if there are any clients that are (AND)
                //    - exposing
                //    - alive
                //    - have an endtime + waittime that is higher than now+next exposure time
                return true;
            }

            // should never be called
            return false;
        }

        private async Task<bool> DitherTask() {
            // here we wait for all alive clients collectively to be
            // either not shooting (sequence end) or waiting for dither (midst of a sequence)
            try {
                while (!ConnectedClients.Where(c => c.IsAlive)
                    .All(c => c.IsWaitingForDither || c.ExposureEndTime < DateTime.Now)) {
                    await Task.Delay(TimeSpan.FromSeconds(1), ditherCancellationTokenSource.Token);
                    ditherCancellationTokenSource.Token.ThrowIfCancellationRequested();
                }

                var result = await guiderInstance.Dither(ditherCancellationTokenSource.Token);
                return result;
            } catch (OperationCanceledException) {
                return false;
            }
        }

        public void CancelSynchronizedDither() {
            ditherCancellationTokenSource?.Cancel();
        }
    }

    [DataContract]
    internal class GuideInfo {

        [DataMember]
        public string State { get; set; }

        [DataMember]
        public PHD2Guider.PhdEventGuideStep GuideStep { get; set; }
    }

    internal class SynchronizedClientInfo {
        public Guid InstanceID { get; set; }

        public DateTime LastPing { get; set; }

        public bool IsAlive => DateTime.Now.Subtract(LastPing).TotalSeconds < 5;

        public DateTime ExposureEndTime { get; set; }

        public bool IsExposing { get; set; }

        public bool IsWaitingForDither { get; set; }

        public double NextExposureTime { get; set; }

        public double MaxWaitTime { get; set; }
    }

    [DataContract]
    internal class ProfileCameraState {

        [DataMember]
        public Guid InstanceId { get; set; }

        [DataMember]
        public bool IsExposing { get; set; }

        [DataMember]
        public DateTime ExposureEndTime { get; set; }

        [DataMember]
        public double NextExposureTime { get; set; }

        [DataMember]
        public double MaxWaitTime { get; set; }
    }
}