using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using NINA.Utility.Profile;

namespace NINA.Model.MyGuider {

    [ServiceContract]
    internal interface ISynchronizedPHD2GuiderService {

        void Initialize();

        [OperationContract]
        void Ping();

        [OperationContract]
        void ConnectClient(SynchronizedClientInfo clientInfo);

        [OperationContract]
        void DisconnectClient(Guid clientId);
    }

    internal class SynchronizedPHD2GuiderService : ISynchronizedPHD2GuiderService {
        private PHD2Guider guiderInstance;
        private List<SynchronizedClientInfo> clientInfos;

        public IProfileService ProfileService;

        public void Initialize() {
            guiderInstance = new PHD2Guider(ProfileService);
            clientInfos = new List<SynchronizedClientInfo>();
        }

        public void ConnectClient(SynchronizedClientInfo clientInfo) {
            var existingInfo = clientInfos.SingleOrDefault(c => c.InstanceID == clientInfo.InstanceID);
            if (existingInfo != null) {
                clientInfos[clientInfos.IndexOf(existingInfo)] = clientInfo;
            } else {
                clientInfos.Add(clientInfo);
            }
        }

        public void Ping() {
        }

        /// <inheritdoc />
        public void DisconnectClient(Guid clientId) {
            clientInfos.RemoveAll(c => c.InstanceID == clientId);
        }
    }

    [DataContract]
    internal class SynchronizedClientInfo {

        [DataMember]
        public Guid InstanceID { get; set; }
    }
}