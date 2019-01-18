using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility;
using NINA.Utility.Profile;

namespace NINA.Model.MyGuider {

    internal class SynchronizedPHD2Guider : BaseINPC, IGuider {
        private readonly IProfileService profileService;

        /// <inheritdoc />
        public bool Connected { get; private set; }

        /// <inheritdoc />
        public double PixelScale { get; set; }

        /// <inheritdoc />
        public string State { get; private set; }

        /// <inheritdoc />
        public IGuideStep GuideStep { get; private set; }

        /// <inheritdoc />
        public string Name => "Synchronized PHD2 Guider " + (isServer ? "Host" : "Client");

        private bool isServer;

        private const string LocalHostUri = "net.pipe://localhost";
        private const string ServiceEndPoint = "SynchronizedPHD2Guider";

        private ISynchronizedPHD2GuiderService guiderService;

        public SynchronizedPHD2Guider(IProfileService profileService, bool isServer) {
            this.profileService = profileService;
            this.isServer = isServer;
        }

        private TaskCompletionSource<bool> serverStarted;
        private CancellationTokenSource disconnectTokenSource;

        /// <inheritdoc />
        public async Task<bool> Connect(CancellationToken ct) {
            disconnectTokenSource = new CancellationTokenSource();
            serverStarted = new TaskCompletionSource<bool>();
            if (isServer) {
                Task.Run(() => RunServer(disconnectTokenSource.Token));
                await serverStarted.Task;
            }

            guiderService = ConnectToServer();

            guiderService.ConnectClient(new SynchronizedClientInfo { InstanceID = profileService.ActiveProfile.Id });

            Connected = true;

            return true;
        }

        private async Task RunServer(CancellationToken ct) {
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
            ChannelFactory<ISynchronizedPHD2GuiderService> httpFactory = new ChannelFactory<ISynchronizedPHD2GuiderService>(new NetNamedPipeBinding(), new EndpointAddress(LocalHostUri + "/" + ServiceEndPoint));
            return httpFactory.CreateChannel();
        }

        /// <inheritdoc />
        public Task<bool> AutoSelectGuideStar() {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Disconnect() {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<bool> Pause(bool pause, CancellationToken ct) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<bool> StartGuiding(CancellationToken ct) {
            throw new NotImplementedException();
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