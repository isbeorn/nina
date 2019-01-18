using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using NINA.Utility.Profile;

namespace NINA.Model.MyGuider {

    [ServiceContract]
    internal interface ISynchronizedPHD2GuiderService {

        void Initialize();

        [OperationContract]
        void ConnectClient(SynchronizedClientInfo clientInfo);
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
    }

    [DataContract]
    internal class SynchronizedClientInfo {

        [DataMember]
        public Guid InstanceID { get; set; }
    }
}