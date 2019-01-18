using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility;
using NINA.Utility.Profile;

namespace NINA.Model.MyGuider {

    internal class SynchronizedPHD2Guider : BaseINPC, IGuider {
        private readonly IProfileService profileService;

        /// <inheritdoc />
        public bool Connected {
            get { return _connected; }
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        /// <inheritdoc />
        public double PixelScale { get; set; }

        /// <inheritdoc />
        public string State {
            get { return _state; }
            private set {
                _state = value;
                RaisePropertyChanged();
            }
        }

        /// <inheritdoc />
        public IGuideStep GuideStep { get; private set; }

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
        private bool _connected;
        private string _state;

        /// <inheritdoc />
        public async Task<bool> Connect(CancellationToken ct) {
            disconnectTokenSource = new CancellationTokenSource();
            serverStarted = new TaskCompletionSource<bool>();
            if (isServer) {
                Task.Run(() => RunServer(disconnectTokenSource.Token));
                await serverStarted.Task;
            }

            guiderService = ConnectToServer();

            Connected = true;

            Task.Run(() => RunClientListener(disconnectTokenSource.Token));

            return Connected;
        }

        private async Task RunClientListener(CancellationToken ct) {
            bool faulted = false;
            try {
                guiderService.ConnectClient(profileService.ActiveProfile.Id);
                while (!ct.IsCancellationRequested) {
                    var guideInfos = guiderService.GetUpdatedGuideInfos(profileService.ActiveProfile.Id);

                    State = guideInfos.AppState?.ToString();

                    await Task.Delay(TimeSpan.FromMilliseconds(1000), ct);
                }
            } catch {
                Connected = false;
                faulted = true;
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
                behavior.InstanceContextMode = InstanceContextMode.Single;
                host.Open();
                ((SynchronizedPHD2GuiderService)host.SingletonInstance).ProfileService = profileService;
                ((SynchronizedPHD2GuiderService)host.SingletonInstance).Initialize();
                serverStarted.TrySetResult(true);
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
            throw new NotImplementedException();
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
}