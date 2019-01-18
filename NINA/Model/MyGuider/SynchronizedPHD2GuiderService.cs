using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility.Profile;

namespace NINA.Model.MyGuider {

    [ServiceContract]
    internal interface ISynchronizedPHD2GuiderService {

        Task<bool> Initialize();

        [OperationContract]
        GuideInfo GetUpdatedGuideInfos(Guid clientId);

        [OperationContract]
        void ConnectClient(Guid clientId);

        [OperationContract]
        void DisconnectClient(Guid clientId);

        [OperationContract]
        bool StartGuiding();

        [OperationContract]
        bool AutoSelectGuideStar();
    }

    internal class SynchronizedPHD2GuiderService : ISynchronizedPHD2GuiderService {
        private PHD2Guider guiderInstance;
        private List<SynchronizedClientInfo> clientInfos;

        public IProfileService ProfileService;

        public async Task<bool> Initialize() {
            guiderInstance = new PHD2Guider(ProfileService);
            clientInfos = new List<SynchronizedClientInfo>();
            return await guiderInstance.Connect(new CancellationTokenSource().Token);
        }

        public void ConnectClient(Guid clientId) {
            var existingInfo = clientInfos.SingleOrDefault(c => c.InstanceID == clientId);
            if (existingInfo != null) {
                clientInfos[clientInfos.IndexOf(existingInfo)].LastPing = DateTime.Now;
            } else {
                clientInfos.Add(new SynchronizedClientInfo { InstanceID = clientId, LastPing = DateTime.Now });
            }
        }

        public GuideInfo GetUpdatedGuideInfos(Guid clientId) {
            clientInfos.Single(c => c.InstanceID == clientId).LastPing = DateTime.Now;
            return new GuideInfo() { AppState = guiderInstance.AppState };
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
        public PHD2Guider.PhdEventAppState AppState { get; set; }
    }

    [DataContract]
    internal class SynchronizedClientInfo {

        [DataMember]
        public Guid InstanceID { get; set; }

        [DataMember]
        public DateTime LastPing { get; set; }
    }
}