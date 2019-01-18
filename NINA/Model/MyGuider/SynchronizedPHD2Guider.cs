using NINA.Utility;
using NINA.Utility.Profile;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    internal class SynchronizedPHD2Guider : BaseINPC, IGuider {
        private readonly IProfileService profileService;

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
        public string Name => "Synchronized PHD2 Guider " + (isServer ? "Host" : "Client");

        private readonly bool isServer;

        private const string LocalHostUri = "net.pipe://localhost";
        private const string ServiceEndPoint = "SynchronizedPHD2Guider";

        private ISynchronizedPHD2GuiderService guiderService;

        public SynchronizedPHD2Guider(IProfileService profileService, bool isServer) {
            this.profileService = profileService;
            this.isServer = isServer;
        }

        private TaskCompletionSource<bool> serverStarted;
        private CancellationTokenSource disconnectTokenSource;
        private bool connected;
        private string state;
        private IGuideStep guideStep;

        /// <inheritdoc />
        public async Task<bool> Connect(CancellationToken ct) {
            disconnectTokenSource = new CancellationTokenSource();
            serverStarted = new TaskCompletionSource<bool>();
            var connected = true;
            if (isServer) {
                Task.Run(() => RunServer(disconnectTokenSource.Token));
                connected = await serverStarted.Task;
            }

            guiderService = ConnectToServer();

            Connected = guiderService != null && connected;

            if (Connected) {
                Task.Run(() => RunClientListener(disconnectTokenSource.Token));
            } else {
                disconnectTokenSource.Cancel();
            }

            return Connected;
        }

        private async Task RunClientListener(CancellationToken ct) {
            bool faulted = false;
            try {
                PixelScale = guiderService.ConnectAndGetPixelScale(profileService.ActiveProfile.Id);
                while (!ct.IsCancellationRequested) {
                    var guideInfos = guiderService.GetUpdatedGuideInfos(profileService.ActiveProfile.Id);

                    State = guideInfos.State;
                    GuideStep = guideInfos.GuideStep;

                    await Task.Delay(TimeSpan.FromMilliseconds(1000), ct);
                }
            } catch (Exception e) {
                Connected = false;
                faulted = true;
                State = "";
                PixelScale = 0;
                disconnectTokenSource.Cancel();
            }

            if (!faulted) {
                guiderService.DisconnectClient(profileService.ActiveProfile.Id);
            }
        }

        private async Task RunServer(CancellationToken ct) {
            // here we initialize a server singleton so we can pass on the profileservice and call initialize
            using (ServiceHost host = new ServiceHost(new SynchronizedPHD2GuiderService(), new Uri(LocalHostUri))) {
                host.AddServiceEndpoint(typeof(ISynchronizedPHD2GuiderService), new NetNamedPipeBinding(), ServiceEndPoint);
                var behavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
                behavior.IncludeExceptionDetailInFaults = true;
                behavior.InstanceContextMode = InstanceContextMode.Single;
                host.Open();
                ((SynchronizedPHD2GuiderService)host.SingletonInstance).ProfileService = profileService;
                serverStarted.TrySetResult(await ((SynchronizedPHD2GuiderService)host.SingletonInstance).Initialize());
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
            guiderServiceChannelFactory.Open();
            return guiderServiceChannelFactory.CreateChannel();
        }

        /// <inheritdoc />
        public Task<bool> AutoSelectGuideStar() {
            return Task.Run(() => guiderService.AutoSelectGuideStar());
        }

        /// <inheritdoc />
        public bool Disconnect() {
            disconnectTokenSource.Cancel();

            Connected = false;
            return true;
        }

        /// <inheritdoc />
        public Task<bool> Pause(bool pause, CancellationToken ct) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<bool> StartGuiding(CancellationToken ct) {
            return await Task.Run(() => guiderService.StartGuiding(), ct);
        }

        /// <inheritdoc />
        public Task<bool> StopGuiding(CancellationToken ct) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<bool> Dither(CancellationToken ct) {
            throw new NotImplementedException();
        }
    }

    public interface IParameterInspector {

        void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState);

        object BeforeCall(string operationName, object[] inputs);
    }

    public class UserLogger : IParameterInspector {

        public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState) {
            throw new NotImplementedException();
        }

        public object BeforeCall(string operationName, object[] inputs) {
            throw new NotImplementedException();
        }
    }
}