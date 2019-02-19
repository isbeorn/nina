using NINA.Locale;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1998

namespace NINA.Model.MyGuider {

    /// <summary>
    /// This class is used to send over guide data from the service to the guider client.
    /// </summary>
    [DataContract]
    internal class GuideInfo {

        [DataMember]
        public PHD2Guider.PhdEventGuideStep GuideStep { get; set; }

        [DataMember]
        public string State { get; set; }
    }

    /// <summary>
    /// This class is used to send over camera data from the guider client to this service.
    /// </summary>
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

    /// <summary>
    /// This class holds information in the service about the connected client
    /// </summary>
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

    /// <summary>
    /// The basic flow of this class is as follows
    /// 1. It is instanced by a ChannelFactory
    /// 2. Initialize is called which passes an IGuider object of type PHD2Guider, replacing the standard constructor
    /// 3. Clients connect with ConnectAndGetPixelScale
    /// 4a. Clients loop and get new information with GetGuideInfo, updating their LastPing time which also determines if they are alive
    /// 4b. Clients loop and push new information when changed with UpdateCameraInfo
    /// 5a. Methods are called as normal from the clients and they are being forwarded to the PHD2Guider, executing them
    ///     - Methods can be cancelled synchronized with CancelMethodName which is implemented in the Clients with the Client CancellationToken
    /// 5b. Exception to that is the SynchronizedDither method, which is the main usage of this GuiderService
    ///     - Clients will call that method with following possible outcomes
    ///         1. They return immediately and no dithering happens
    ///             - this will happen when any other client is currently exposing
    ///             - the other client needs to have a longer still ongoing exposure time than the current client + its download time
    ///             - the client has no further exposures
    ///         2. They wait for the other instances and dithering happens
    ///             - this will happen if 1 is not fulfilled
    ///         3. Dithering happens immediately
    ///             - this will only happen if no other clients are connected
    /// 6. Clients can de-register themselves by using DisconnectClient
    ///     - if clients crash or fail to send the information from the loop as stated in 4a, they will be considered as dead and not be taken into consideration in 5b
    /// </summary>
    /// <remarks>
    ///     The methods in this service are implemented in a way that multi-threading is possible.
    ///     This is important for the asynchronized calls of UpdateCameraInfo as well as GetGuideInfo
    /// </remarks>
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

        /// <inheritdoc />
        public Task<bool> AutoSelectGuideStar() {
            return guiderInstance.AutoSelectGuideStar();
        }

        /// <inheritdoc />
        public void CancelStartGuiding() {
            startGuidingCancellationTokenSource?.Cancel();
        }

        /// <inheritdoc />
        public void CancelStartPause() {
            startPauseCancellationTokenSource?.Cancel();
        }

        /// <inheritdoc />
        public void CancelStopGuiding() {
            stopGuidingCancellationTokenSource?.Cancel();
        }

        /// <inheritdoc />
        public void CancelSynchronizedDither() {
            ditherCancellationTokenSource?.Cancel();
        }

        /// <inheritdoc />
        public async Task<double> ConnectAndGetPixelScale(Guid clientId) {
            var phd2Initialized = await initializeTaskCompletionSource.Task;
            if (!phd2Initialized) {
                throw new FaultException<PHD2Fault>(new PHD2Fault());
            }
            var existingInfo = ConnectedClients.SingleOrDefault(c => c.InstanceID == clientId);
            if (existingInfo != null && existingInfo.IsAlive) {
                throw new FaultException<ClientAlreadyExistsFault>(new ClientAlreadyExistsFault());
            }

            ConnectedClients.Add(new SynchronizedClientInfo { InstanceID = clientId, LastPing = DateTime.Now });
            Notification.ShowSuccess(string.Format(Locale["LblPhd2SynchronizedServiceClientConnected"], ConnectedClients.Count(c => c.IsAlive)));

            return guiderInstance.PixelScale;
        }

        /// <inheritdoc />
        public void DisconnectClient(Guid clientId) {
            ConnectedClients.RemoveAll(c => c.InstanceID == clientId);
            Notification.ShowSuccess(string.Format(Locale["LblPhd2SynchronizedServiceClientDisconnected"], ConnectedClients.Count(c => c.IsAlive)));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task<bool> Initialize(IGuider guider, CancellationToken ct) {
            initializeTaskCompletionSource = new TaskCompletionSource<bool>();
            guiderInstance = guider;
            ConnectedClients = new List<SynchronizedClientInfo>();
            PHD2Connected = await guiderInstance.Connect();
            if (PHD2Connected) {
                ((PHD2Guider)guiderInstance).PHD2ConnectionLost += (sender, args) => PHD2Connected = false;
                Notification.ShowSuccess(Locale["LblPhd2SynchronizedServiceStarted"]);
            }

            initializeTaskCompletionSource.TrySetResult(PHD2Connected);
            return PHD2Connected;
        }

        /// <inheritdoc />
        public Task<bool> StartGuiding() {
            lock (startGuidingLock) {
                var taskStatus = startGuidingTask?.Status;
                if (taskStatus == TaskStatus.Faulted ||
                    taskStatus == TaskStatus.Canceled ||
                    taskStatus == TaskStatus.RanToCompletion ||
                    startGuidingTask == null) {
                    startGuidingCancellationTokenSource?.Dispose();
                    startGuidingCancellationTokenSource = new CancellationTokenSource();
                    startGuidingTask = guiderInstance.StartGuiding(startGuidingCancellationTokenSource.Token);
                }
            }

            return startGuidingTask;
        }

        /// <inheritdoc />
        public Task<bool> StartPause(bool pause) {
            lock (startPauseLock) {
                var taskStatus = startPauseTask?.Status;
                if (taskStatus == TaskStatus.Faulted ||
                    taskStatus == TaskStatus.Canceled ||
                    taskStatus == TaskStatus.RanToCompletion ||
                    startPauseTask == null) {
                    startPauseCancellationTokenSource?.Dispose();
                    startPauseCancellationTokenSource = new CancellationTokenSource();
                    startPauseTask = guiderInstance.Pause(pause, startPauseCancellationTokenSource.Token);
                }
            }

            return startPauseTask;
        }

        /// <inheritdoc />
        public Task<bool> StopGuiding() {
            lock (stopGuidingLock) {
                var taskStatus = stopGuidingTask?.Status;
                if (taskStatus == TaskStatus.Faulted ||
                    taskStatus == TaskStatus.Canceled ||
                    taskStatus == TaskStatus.RanToCompletion ||
                    stopGuidingTask == null) {
                    stopGuidingCancellationTokenSource?.Dispose();
                    stopGuidingCancellationTokenSource = new CancellationTokenSource();
                    stopGuidingTask = guiderInstance.StopGuiding(stopGuidingCancellationTokenSource.Token);
                }
            }

            return stopGuidingTask;
        }

        /// <inheritdoc />
        public async Task<bool> SynchronizedDither(Guid instanceId) {
            lock (ditherLock) {
                if (ditherCancellationTokenSource == null) {
                    ditherCancellationTokenSource?.Dispose();
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

            // squeeze in more exposures
            // if there are any clients that are (AND)
            //    - exposing
            //    - alive
            //    - have an endtime + curClient.LastDownloadTime that is higher than now+curClient.NextExposureTime
            if (otherAliveClients.Any(c => c.IsExposing &&
                c.ExposureEndTime.AddSeconds(client.LastDownloadTime) >= DateTime.Now.AddSeconds(client.NextExposureTime))) {
                return true;
            }

            // if all clients finish before our added next exposure time we will dither
            // one client has to launch the dither task that will wait for all alive clients to dither
            lock (ditherLock) {
                if (ditherTask == null) {
                    ditherTask = DitherTask();
                }
            }

            // this will indicate to the DitherTask that this client is waiting for dithering
            // the DitherTask will call dithering when all clients are waiting or doing nothing
            client.IsWaitingForDither = true;

            var result = await ditherTask;

            client.IsWaitingForDither = false;

            lock (ditherLock) {
                ditherCancellationTokenSource = null;
                ditherTask = null;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task UpdateCameraInfo(ProfileCameraState profileCameraState) {
            var clientInfo = ConnectedClients.Single(c => c.InstanceID == profileCameraState.InstanceId);
            clientInfo.IsExposing = profileCameraState.IsExposing;
            clientInfo.NextExposureTime = profileCameraState.NextExposureTime;
            clientInfo.LastDownloadTime = profileCameraState.LastDownloadTime;
            clientInfo.ExposureEndTime = profileCameraState.ExposureEndTime;
        }
    }
}