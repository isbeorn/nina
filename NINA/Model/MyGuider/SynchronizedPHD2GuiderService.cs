using NINA.Locale;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    [DataContract]
    internal class GuideInfo {

        [DataMember]
        public PHD2Guider.PhdEventGuideStep GuideStep { get; set; }

        [DataMember]
        public string State { get; set; }
    }

    [DataContract]
    internal class ProfileCameraState {

        [DataMember]
        public DateTime ExposureEndTime { get; set; }

        [DataMember]
        public Guid InstanceId { get; set; }

        [DataMember]
        public bool IsExposing { get; set; }

        [DataMember]
        public double LastDownloadTime { get; set; }

        [DataMember]
        public double NextExposureTime { get; set; }
    }

    internal class SynchronizedClientInfo {
        public DateTime ExposureEndTime { get; set; }
        public Guid InstanceID { get; set; }

        public bool IsAlive => DateTime.Now.Subtract(LastPing).TotalSeconds < 5;
        public bool IsExposing { get; set; }
        public bool IsWaitingForDither { get; set; }
        public double LastDownloadTime { get; set; }
        public DateTime LastPing { get; set; }
        public double NextExposureTime { get; set; }
    }

    internal class SynchronizedPHD2GuiderService : ISynchronizedPHD2GuiderService {
        private readonly object ditherLock = new object();
        private readonly object startGuidingLock = new object();
        private readonly object startPauseLock = new object();
        private readonly object stopGuidingLock = new object();
        private CancellationTokenSource ditherCancellationTokenSource;
        private Task<bool> ditherTask;
        private IGuider guiderInstance;
        private TaskCompletionSource<bool> initializeTaskCompletionSource;
        private CancellationTokenSource startGuidingCancellationTokenSource;
        private Task<bool> startGuidingTask;
        private CancellationTokenSource startPauseCancellationTokenSource;
        private Task<bool> startPauseTask;
        private CancellationTokenSource stopGuidingCancellationTokenSource;
        private Task<bool> stopGuidingTask;
        public List<SynchronizedClientInfo> ConnectedClients;
        private ILoc Locale { get; set; } = Loc.Instance;
        public bool PHD2Connected { get; private set; } = false;

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

        private async Task<bool> StartGuidingTask() {
            var result = await guiderInstance.StartGuiding(startGuidingCancellationTokenSource.Token);
            return result;
        }

        private async Task<bool> StartPauseTask(bool pause) {
            var result = await guiderInstance.Pause(pause, startPauseCancellationTokenSource.Token);
            return result;
        }

        private async Task<bool> StopGuidingTask() {
            var result = await guiderInstance.StopGuiding(stopGuidingCancellationTokenSource.Token);
            return result;
        }

        /// <inheritdoc />
        public Task<bool> AutoSelectGuideStar() {
            return guiderInstance.AutoSelectGuideStar();
        }

        public void CancelStartGuiding() {
            startGuidingCancellationTokenSource?.Cancel();
        }

        public void CancelStartPause() {
            startPauseCancellationTokenSource?.Cancel();
        }

        public void CancelStopGuiding() {
            stopGuidingCancellationTokenSource?.Cancel();
        }

        public void CancelSynchronizedDither() {
            ditherCancellationTokenSource?.Cancel();
        }

        public async Task<double> ConnectAndGetPixelScale(Guid clientId) {
            var phd2Initialized = await initializeTaskCompletionSource.Task;
            if (!phd2Initialized) {
                throw new FaultException<PHD2Fault>(new PHD2Fault());
            }
            var existingInfo = ConnectedClients.SingleOrDefault(c => c.InstanceID == clientId);
            if (existingInfo != null) {
                ConnectedClients[ConnectedClients.IndexOf(existingInfo)].LastPing = DateTime.Now;
            } else {
                ConnectedClients.Add(new SynchronizedClientInfo { InstanceID = clientId, LastPing = DateTime.Now });
                Notification.ShowSuccess(string.Format(Locale["LblPhd2SynchronizedServiceClientConnected"], ConnectedClients.Count(c => c.IsAlive)));
            }

            return guiderInstance.PixelScale;
        }

        /// <inheritdoc />
        public void DisconnectClient(Guid clientId) {
            ConnectedClients.RemoveAll(c => c.InstanceID == clientId);
            Notification.ShowSuccess(string.Format(Locale["LblPhd2SynchronizedServiceClientDisconnected"], ConnectedClients.Count(c => c.IsAlive)));
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

        public async Task<bool> Initialize(IGuider guider, CancellationToken ct) {
            initializeTaskCompletionSource = new TaskCompletionSource<bool>();
            guiderInstance = guider;
            ConnectedClients = new List<SynchronizedClientInfo>();
            PHD2Connected = await guiderInstance.Connect(ct);
            if (PHD2Connected) {
                ((PHD2Guider)guiderInstance).PHD2ConnectionLost += (sender, args) => PHD2Connected = false;
                Notification.ShowSuccess(Locale["LblPhd2SynchronizedServiceStarted"]);
            }

            initializeTaskCompletionSource.TrySetResult(PHD2Connected);
            return PHD2Connected;
        }

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

        public async Task<bool> SynchronizedDither(Guid instanceId) {
            lock (ditherLock) {
                if (ditherCancellationTokenSource == null) {
                    ditherCancellationTokenSource = new CancellationTokenSource();
                }
            }

            var client = ConnectedClients.Single(c => c.InstanceID == instanceId);

            // no further exposures, just return
            if (client.NextExposureTime < 0) {
                return true;
            }

            var otherAliveClients = ConnectedClients.Where(c => c.IsAlive).Where(c => c.InstanceID != client.InstanceID).ToList();

            // no other clients exist, just dither
            if (!otherAliveClients.Any()) {
                var output = await guiderInstance.Dither(ditherCancellationTokenSource.Token);
                ditherCancellationTokenSource = null;
                return output;
            }

            if (otherAliveClients.Any(c => c.IsExposing &&
                c.ExposureEndTime.AddSeconds(client.LastDownloadTime) >= DateTime.Now.AddSeconds(client.NextExposureTime))) {
                // squeeze in more exposures
                // if there are any clients that are (AND)
                //    - exposing
                //    - alive
                //    - have an endtime + curClient.AvgDLTime that is higher than now+curClient.next exposure time
                return true;
            }

            if (otherAliveClients.All(c => c.ExposureEndTime < DateTime.Now.AddSeconds(client.NextExposureTime))) {
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

            // should never be called
            return false;
        }

        public async Task UpdateCameraInfo(ProfileCameraState profileCameraState) {
            var clientInfo = ConnectedClients.Single(c => c.InstanceID == profileCameraState.InstanceId);
            clientInfo.IsExposing = profileCameraState.IsExposing;
            clientInfo.NextExposureTime = profileCameraState.NextExposureTime;
            clientInfo.LastDownloadTime = profileCameraState.LastDownloadTime;
            clientInfo.ExposureEndTime = profileCameraState.ExposureEndTime;
        }
    }
}