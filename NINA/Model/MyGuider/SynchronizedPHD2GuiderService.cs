using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    [ServiceContract]
    internal interface ISynchronizedPHD2GuiderService {

        Task<bool> Initialize();

        [OperationContract]
        [FaultContract(typeof(PHD2Fault))]
        GuideInfo GetUpdatedGuideInfos(Guid clientId);

        [OperationContract]
        double ConnectAndGetPixelScale(Guid clientId);

        [OperationContract]
        void DisconnectClient(Guid clientId);

        [OperationContract]
        bool StartGuiding();

        [OperationContract]
        bool AutoSelectGuideStar();
    }

    internal class PHD2Fault { }

    internal class SynchronizedPHD2GuiderService : ISynchronizedPHD2GuiderService {
        private PHD2Guider guiderInstance;
        private List<SynchronizedClientInfo> clientInfos;
        private bool phd2Connected = false;

        public IProfileService ProfileService;

        public async Task<bool> Initialize() {
            guiderInstance = new PHD2Guider(ProfileService);
            clientInfos = new List<SynchronizedClientInfo>();
            phd2Connected = await guiderInstance.Connect(new CancellationTokenSource().Token);
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

        public GuideInfo GetUpdatedGuideInfos(Guid clientId) {
            if (!phd2Connected) {
                throw new FaultException("PHD2 disconnected");
            }
            clientInfos.Single(c => c.InstanceID == clientId).LastPing = DateTime.Now;

            return new GuideInfo() {
                State = guiderInstance.AppState?.State,
                GuideStep = (PHD2Guider.PhdEventGuideStep)guiderInstance.GuideStep
            };
        }

        /// <inheritdoc />
        public void DisconnectClient(Guid clientId) {
            clientInfos.RemoveAll(c => c.InstanceID == clientId);
        }

        /// <inheritdoc />
        public bool StartGuiding() {
            CancellationTokenSource cts = new CancellationTokenSource();
            return guiderInstance.StartGuiding(cts.Token).Result;
        }

        /// <inheritdoc />
        public bool AutoSelectGuideStar() {
            return guiderInstance.AutoSelectGuideStar().Result;
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
    }
}