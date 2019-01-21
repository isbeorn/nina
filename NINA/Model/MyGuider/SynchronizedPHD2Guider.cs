using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    internal class SynchronizedPHD2Guider : BaseINPC, IGuider, ICameraConsumer {
        private readonly IProfileService profileService;
        private readonly ICameraMediator cameraMediator;

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
        public string Name => "Synchronized PHD2 Guider";

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
            startServerTcs = new TaskCompletionSource<bool>();

            // try to run as server
            Task.Run(() => RunServer(disconnectTokenSource.Token));
            await startServerTcs.Task;

            guiderService = ConnectToServer();

            Connected = guiderService != null;

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
                    PixelScale = await guiderService.ConnectAndGetPixelScale(profileService.ActiveProfile.Id);
                    cameraMediator.RegisterConsumer(this);
                    while (true) {
                        var guideInfos = await guiderService.GetGuideInfo(profileService.ActiveProfile.Id);

                        State = guideInfos.State;
                        GuideStep = guideInfos.GuideStep;

                        await Task.Delay(TimeSpan.FromMilliseconds(1000), ct);
                        ct.ThrowIfCancellationRequested();
                    }
                } catch (FaultException<PHD2Fault>) {
                    // phd2 is not running for whatever reason, throw some error message
                    Notification.ShowError("PHD2 aborted connection");
                    faulted = true;
                    Connected = false;
                    State = "";
                    PixelScale = 0;
                    disconnectTokenSource.Cancel();
                } catch (OperationCanceledException) {
                    disconnectTokenSource.Cancel();
                    Connected = false;
                    State = "";
                    PixelScale = 0;
                    break;
                } catch (Exception) {
                    // assume nina other instance crash, restart server
                    Notification.ShowWarning("Synchronized PHD2 Service crashed, restarting this Synchronized PHD2 Guider as Service");
                    cameraMediator.RemoveConsumer(this);
                    startServerTcs = new TaskCompletionSource<bool>();
                    Task.Run(() => RunServer(disconnectTokenSource.Token));
                    await startServerTcs.Task;
                    guiderService = ConnectToServer();
                    Connected = guiderService != null;
                    faulted = !Connected;
                }
            }

            if (!faulted) {
                guiderService.DisconnectClient(profileService.ActiveProfile.Id);
            }

            cameraMediator.RemoveConsumer(this);
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

                startServerTcs.TrySetResult(await ((SynchronizedPHD2GuiderService)host.SingletonInstance)
                    .Initialize(new PHD2Guider(profileService), ct));

                // loop to keep the server alive
                while (!ct.IsCancellationRequested) {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000), ct);
                }
            }
        }

        private ISynchronizedPHD2GuiderService ConnectToServer() {
            // this could maybe need a duplexchannelfactory for back-communication
            // maybe necessary for error handling
            ChannelFactory<ISynchronizedPHD2GuiderService> guiderServiceChannelFactory
                = new ChannelFactory<ISynchronizedPHD2GuiderService>(new NetNamedPipeBinding(), new EndpointAddress(LocalHostUri + "/" + ServiceEndPoint));
            guiderServiceChannelFactory.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(1);
            guiderServiceChannelFactory.Endpoint.Binding.CloseTimeout = TimeSpan.FromSeconds(1);
            guiderServiceChannelFactory.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(600);
            guiderServiceChannelFactory.Open();
            var channel = guiderServiceChannelFactory.CreateChannel();
            ((IContextChannel)channel).OperationTimeout = TimeSpan.FromSeconds(600);
            return channel;
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
            if (exposureEndTime != deviceInfo.ExposureEndTime || cameraIsExposing != deviceInfo.IsExposing ||
                nextExposureLength != deviceInfo.NextExposureLength) {
                exposureEndTime = deviceInfo.ExposureEndTime;
                cameraIsExposing = deviceInfo.IsExposing;
                nextExposureLength = deviceInfo.NextExposureLength;

                await guiderService.UpdateCameraInfo(new ProfileCameraState() {
                    ExposureEndTime = exposureEndTime,
                    InstanceId = profileService.ActiveProfile.Id,
                    IsExposing = cameraIsExposing,
                    NextExposureTime = nextExposureLength,
                    MaxWaitTime = 20
                });
            }
        }

        private DateTime exposureEndTime;
        private bool cameraIsExposing;
        private double nextExposureLength;
    }
}