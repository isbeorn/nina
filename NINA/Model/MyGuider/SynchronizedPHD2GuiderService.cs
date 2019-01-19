using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    internal class SynchronizedPHD2GuiderService : ISynchronizedPHD2GuiderService {
        private PHD2Guider guiderInstance;
        private List<SynchronizedClientInfo> clientInfos;
        private bool phd2Connected = false;

        public IProfileService ProfileService;

        public async Task<bool> Initialize(CancellationToken ct) {
            guiderInstance = new PHD2Guider(ProfileService);
            clientInfos = new List<SynchronizedClientInfo>();
            phd2Connected = await guiderInstance.Connect(ct);
            if (phd2Connected) {
                guiderInstance.PHD2ConnectionLost += (sender, args) => phd2Connected = false;
            }
            return phd2Connected;
        }

        public double ConnectAndGetPixelScale(Guid clientId) {
            var existingInfo = clientInfos.SingleOrDefault(c => c.InstanceID == clientId);
            if (existingInfo != null) {
                clientInfos[clientInfos.IndexOf(existingInfo)].LastPing = DateTime.Now;
            } else {
                clientInfos.Add(new SynchronizedClientInfo { InstanceID = clientId, LastPing = DateTime.Now });
            }

            return guiderInstance.PixelScale;
        }

        public async Task<GuideInfo> GetGuideInfo(Guid clientId) {
            if (!phd2Connected) {
                throw new FaultException("PHD2 disconnected");
            }

            var clientInfo = clientInfos.Single(c => c.InstanceID == clientId);
            clientInfo.LastPing = DateTime.Now;

            return new GuideInfo() {
                State = guiderInstance.AppState?.State,
                GuideStep = (PHD2Guider.PhdEventGuideStep)guiderInstance.GuideStep
            };
        }

        public async Task UpdateCameraInfo(ProfileCameraState profileCameraState) {
            var clientInfo = clientInfos.Single(c => c.InstanceID == profileCameraState.InstanceID);
            clientInfo.IsExposing = profileCameraState.IsExposing;
            clientInfo.NextExposureTime = profileCameraState.NextExposureTime;
            Console.WriteLine(clientInfo.InstanceID + " IsExposing: " + clientInfo.IsExposing);
            Console.WriteLine(clientInfo.InstanceID + " NextExposureTime: " + clientInfo.NextExposureTime);
            clientInfo.ExposureEndTime = profileCameraState.ExposureEndTime;
        }

        /// <inheritdoc />
        public void DisconnectClient(Guid clientId) {
            clientInfos.RemoveAll(c => c.InstanceID == clientId);
        }

        private CancellationTokenSource startGuidingCancellationTokenSource;
        private TaskCompletionSource<bool> startGuidingTaskCompletionSource;
        private readonly object startGuidingLock = new object();

        /// <inheritdoc />
        public async Task<bool> StartGuiding() {
            lock (startGuidingLock) {
                if (startGuidingCancellationTokenSource == null) {
                    startGuidingCancellationTokenSource = new CancellationTokenSource();
                    startGuidingTaskCompletionSource = new TaskCompletionSource<bool>();
                    Task.Run(() => StartGuidingTask(startGuidingTaskCompletionSource));
                }
            }

            var result = await startGuidingTaskCompletionSource.Task;

            startGuidingCancellationTokenSource = null;
            return result;
        }

        private async Task StartGuidingTask(TaskCompletionSource<bool> tcs) {
            var result = await guiderInstance.StartGuiding(startGuidingCancellationTokenSource.Token);
            tcs.TrySetResult(result);
        }

        public void CancelStartGuiding() {
            startGuidingCancellationTokenSource?.Cancel();
        }

        /// <inheritdoc />
        public Task<bool> AutoSelectGuideStar() {
            return guiderInstance.AutoSelectGuideStar();
        }

        private CancellationTokenSource startPauseCancellationTokenSource;
        private TaskCompletionSource<bool> startPauseTaskCompletionSource;
        private readonly object startPauseLock = new object();

        public async Task<bool> StartPause(bool pause) {
            lock (startPauseLock) {
                if (startPauseCancellationTokenSource == null) {
                    startPauseCancellationTokenSource = new CancellationTokenSource();
                    startPauseTaskCompletionSource = new TaskCompletionSource<bool>();
                    Task.Run(() => StartPauseTask(startPauseTaskCompletionSource, pause));
                }
            }

            var result = await startPauseTaskCompletionSource.Task;

            startPauseCancellationTokenSource = null;
            return result;
        }

        private async Task StartPauseTask(TaskCompletionSource<bool> tcs, bool pause) {
            var result = await guiderInstance.Pause(pause, startPauseCancellationTokenSource.Token);
            tcs.TrySetResult(result);
        }

        public void CancelStartPause() {
            startPauseCancellationTokenSource?.Cancel();
        }

        private CancellationTokenSource stopGuidingCancellationTokenSource;
        private TaskCompletionSource<bool> stopGuidingTaskCompletionSource;
        private readonly object stopGuidingLock = new object();

        public async Task<bool> StopGuiding() {
            lock (stopGuidingLock) {
                if (stopGuidingCancellationTokenSource == null) {
                    stopGuidingCancellationTokenSource = new CancellationTokenSource();
                    stopGuidingTaskCompletionSource = new TaskCompletionSource<bool>();
                    Task.Run(() => StopGuidingTask(stopGuidingTaskCompletionSource));
                }
            }

            var result = await stopGuidingTaskCompletionSource.Task;

            stopGuidingCancellationTokenSource = null;
            return result;
        }

        private async Task StopGuidingTask(TaskCompletionSource<bool> tcs) {
            var result = await guiderInstance.StopGuiding(stopGuidingCancellationTokenSource.Token);
            tcs.TrySetResult(result);
        }

        public void CancelStopGuiding() {
            stopGuidingCancellationTokenSource?.Cancel();
        }

        private CancellationTokenSource ditherCancellationTokenSource;
        private TaskCompletionSource<bool> ditherTaskCompletionSource;
        private readonly object ditherLock = new object();

        public async Task<bool> SynchronizedDither(Guid instanceId) {
            lock (ditherLock) {
                if (ditherCancellationTokenSource == null) {
                    ditherCancellationTokenSource = new CancellationTokenSource();
                    ditherTaskCompletionSource = new TaskCompletionSource<bool>();
                }
            }

            var clientInfo = clientInfos.Single(c => c.InstanceID == instanceId);

            clientInfo.IsWaitingForDither = true;

            var otherClientsExist = clientInfos.Any(c => c.IsAlive && c.InstanceID != clientInfo.InstanceID);

            if (!otherClientsExist) {
                var output = await guiderInstance.Dither(ditherCancellationTokenSource.Token);
                ditherCancellationTokenSource = null;
                return output;
            }

            if (!clientInfos.Any(c => c.IsAlive && c.IsExposing)) {
                Task.Run(() => DitherTask(ditherTaskCompletionSource));
            }

            var result = await ditherTaskCompletionSource.Task;

            foreach (var client in clientInfos) {
                client.IsWaitingForDither = false;
            }

            lock (ditherLock) {
                if (ditherCancellationTokenSource != null) {
                    ditherCancellationTokenSource = null;
                }
            }

            return result;
        }

        private async Task DitherTask(TaskCompletionSource<bool> tcs) {
            var result = await guiderInstance.Dither(ditherCancellationTokenSource.Token);
            tcs.TrySetResult(result);
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

        public bool IsAlive => DateTime.Now.Subtract(LastPing).TotalSeconds < 2;

        public DateTime ExposureEndTime { get; set; }

        public bool IsExposing { get; set; }

        public bool IsWaitingForDither { get; set; }

        public double NextExposureTime { get; set; }
    }

    [DataContract]
    internal class ProfileCameraState {

        [DataMember]
        public Guid InstanceID { get; set; }

        [DataMember]
        public bool IsExposing { get; set; }

        [DataMember]
        public DateTime ExposureEndTime { get; set; }

        [DataMember]
        public double NextExposureTime { get; set; }
    }
}