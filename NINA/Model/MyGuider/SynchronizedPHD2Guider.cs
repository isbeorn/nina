using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace NINA.Model.MyGuider {

    internal class SynchronizedPHD2Guider : BaseINPC, IGuider, ICameraConsumer {
        private readonly IProfileService profileService;
        private readonly ICameraMediator cameraMediator;
        private ILoc Locale { get; set; } = Loc.Instance;

        /// <inheritdoc />
        public bool Connected {
            get => connected;
            set {
                connected = value;
                RaisePropertyChanged();
            }
        }

        private double pixelScale;

        /// <inheritdoc />
        public double PixelScale {
            get => pixelScale;
            set {
                pixelScale = value;
                RaisePropertyChanged();
            }
        }

        /// <inheritdoc />
        public string State {
            get => state;
            set {
                state = value;
                RaisePropertyChanged(nameof(State));
            }
        }

        /// <inheritdoc />
        public IGuideStep GuideStep {
            get => guideStep;
            set {
                if (value != null) {
                    guideStep = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <inheritdoc />
        public string Name => Locale["LblSynchronizedPHD2Guider"];

        private const string LocalHostUri = "net.pipe://localhost";
        private const string ServiceEndPoint = "SynchronizedPHD2Guider";

        private ISynchronizedPHD2GuiderService guiderService;

        public SynchronizedPHD2Guider(IProfileService profileService, ICameraMediator cameraMediator) {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
        }

        private TaskCompletionSource<bool> startServerTcs;
        private CancellationTokenSource disconnectTokenSource;
        private bool connected;
        private string state;
        private IGuideStep guideStep;

        /// <inheritdoc />
        public async Task<bool> Connect(CancellationToken ct) {
            disconnectTokenSource = new CancellationTokenSource();

            await TryStartServiceAndConnect();

            if (Connected) {
                Task.Run(() => RunClientListener(disconnectTokenSource.Token));
            } else {
                disconnectTokenSource.Cancel();
            }

            return Connected;
        }

        private async Task RunClientListener(CancellationToken ct) {
            bool faulted = false;
            while (!faulted) {
                try {
                    // connect and register to the service and get the pixelscale (that's the only information we need only once)
                    PixelScale = await guiderService.ConnectAndGetPixelScale(profileService.ActiveProfile.Id);
                    // register to camera consumer so the camera info data is updated to the service
                    cameraMediator.RegisterConsumer(this);
                    // loop getting the latest phd2 service information indefinitely
                    while (true) {
                        var guideInfos = await guiderService.GetGuideInfo(profileService.ActiveProfile.Id);

                        State = guideInfos.State;
                        GuideStep = guideInfos.GuideStep;

                        await Task.Delay(TimeSpan.FromMilliseconds(1000), ct);
                        ct.ThrowIfCancellationRequested();
                    }
                } catch (FaultException<PHD2Fault>) {
                    // phd2 connection lost
                    faulted = true;
                    Connected = false;
                    State = "";
                    PixelScale = 0;
                    disconnectTokenSource.Cancel();
                } catch (OperationCanceledException) {
                    // user disconnected
                    disconnectTokenSource.Cancel();
                    Connected = false;
                    State = "";
                    PixelScale = 0;
                    break;
                } catch (Exception) {
                    // assume nina other instance crash, restart server
                    Notification.ShowWarning(Locale["LblSynchronizedPHD2ServiceCrashed"]);
                    // remove the consumer since we will restart the loop getting all information and re-registering on the service
                    cameraMediator.RemoveConsumer(this);
                    // try to restart the server
                    await TryStartServiceAndConnect();
                    faulted = !Connected;
                }
            }

            // faulted is false when the client disconnected willfully, not forcefully
            if (!faulted) {
                guiderService.DisconnectClient(profileService.ActiveProfile.Id);
            }

            cameraMediator.RemoveConsumer(this);
        }

        private async Task TryStartServiceAndConnect() {
            startServerTcs = new TaskCompletionSource<bool>();
            // try to run the server loop
            Task.Run(() => RunServer(disconnectTokenSource.Token));
            // wait for the server to be either started or failed to be started
            await startServerTcs.Task;
            // try to connect to any existing server
            guiderService = ConnectToSynchronizedPHD2Service();
            Connected = guiderService != null;
        }

        private async Task RunServer(CancellationToken ct) {
            // here we initialize a server singleton so we can pass on the profileservice and call initialize
            using (ServiceHost host = new ServiceHost(new SynchronizedPHD2GuiderService(), new Uri(LocalHostUri))) {
                host.AddServiceEndpoint(typeof(ISynchronizedPHD2GuiderService), new NetNamedPipeBinding(),
                    ServiceEndPoint);
                var behavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
                behavior.IncludeExceptionDetailInFaults = true;
                behavior.InstanceContextMode = InstanceContextMode.Single;
                behavior.ConcurrencyMode = ConcurrencyMode.Multiple;
                try {
                    host.Open();
                } catch (Exception) {
                    startServerTcs.TrySetResult(false);
                    return;
                }

                // initialize the server, try to start and connect to phd2
                startServerTcs.TrySetResult(await ((SynchronizedPHD2GuiderService)host.SingletonInstance)
                    .Initialize(new PHD2Guider(profileService), ct));

                // loop to keep the server alive
                while (!ct.IsCancellationRequested) {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000), ct);
                }
            }
        }

        private ISynchronizedPHD2GuiderService ConnectToSynchronizedPHD2Service() {
            var guiderServiceChannelFactory
                = new ChannelFactory<ISynchronizedPHD2GuiderService>(new NetNamedPipeBinding(), new EndpointAddress(LocalHostUri + "/" + ServiceEndPoint));
            guiderServiceChannelFactory.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(1);
            guiderServiceChannelFactory.Endpoint.Binding.CloseTimeout = TimeSpan.FromSeconds(1);
            guiderServiceChannelFactory.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(600);
            guiderServiceChannelFactory.Open();
            ISynchronizedPHD2GuiderService serviceChannel = guiderServiceChannelFactory.CreateChannel();
            // timespan for waiting for a method to return, to be on the safe side set to 10 minutes
            // the 'suspicious' cast is not suspicious at all, the IDE doesn't understand it's a channel we get returned
            ((IContextChannel)serviceChannel).OperationTimeout = TimeSpan.FromMinutes(10);
            return serviceChannel;
        }

        /// <inheritdoc />
        public bool Disconnect() {
            disconnectTokenSource.Cancel();

            Connected = false;
            return true;
        }

        /// <inheritdoc />
        public Task<bool> AutoSelectGuideStar() {
            return Task.Run(() => guiderService.AutoSelectGuideStar());
        }

        /// <inheritdoc />
        public async Task<bool> Pause(bool pause, CancellationToken ct) {
            return await Task.Run(async () => {
                ct.Register(guiderService.CancelStartPause);
                return await guiderService.StartPause(pause);
            }, ct);
        }

        /// <inheritdoc />
        public async Task<bool> StartGuiding(CancellationToken ct) {
            return await Task.Run(async () => {
                ct.Register(guiderService.CancelStartGuiding);
                return await guiderService.StartGuiding();
            }, ct);
        }

        /// <inheritdoc />
        public async Task<bool> StopGuiding(CancellationToken ct) {
            return await Task.Run(async () => {
                ct.Register(guiderService.CancelStopGuiding);
                return await guiderService.StopGuiding();
            }, ct);
        }

        /// <inheritdoc />
        public async Task<bool> Dither(CancellationToken ct) {
            return await Task.Run(async () => {
                ct.Register(guiderService.CancelSynchronizedDither);
                return await guiderService.SynchronizedDither(profileService.ActiveProfile.Id);
            }, ct);
        }

        public async void UpdateDeviceInfo(CameraInfo deviceInfo) {
            if ((exposureEndTime != deviceInfo.ExposureEndTime || cameraIsExposing != deviceInfo.IsExposing ||
                nextExposureLength != deviceInfo.NextExposureLength || lastDownloadTime != deviceInfo.LastDownloadTime) && connected) {
                exposureEndTime = deviceInfo.ExposureEndTime;
                cameraIsExposing = deviceInfo.IsExposing;
                nextExposureLength = deviceInfo.NextExposureLength;
                lastDownloadTime = deviceInfo.LastDownloadTime == -1 ? lastDownloadTime : deviceInfo.LastDownloadTime;

                await guiderService.UpdateCameraInfo(new ProfileCameraState() {
                    ExposureEndTime = exposureEndTime,
                    InstanceId = profileService.ActiveProfile.Id,
                    IsExposing = cameraIsExposing,
                    NextExposureTime = nextExposureLength,
                    LastDownloadTime = lastDownloadTime
                });
            }
        }

        private DateTime exposureEndTime;
        private bool cameraIsExposing;
        private double nextExposureLength;
        private double lastDownloadTime;
    }
}