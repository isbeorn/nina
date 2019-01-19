using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    [ServiceContract]
    internal interface ISynchronizedPHD2GuiderService {

        Task<bool> Initialize(CancellationToken ct);

        [OperationContract]
        [FaultContract(typeof(PHD2Fault))]
        GuideInfo GetUpdatedGuideInfos(Guid clientId);

        [OperationContract]
        double ConnectAndGetPixelScale(Guid clientId);

        [OperationContract]
        void DisconnectClient(Guid clientId);

        [OperationContract]
        Task<bool> StartGuiding();

        [OperationContract]
        void CancelStartGuiding();

        [OperationContract]
        Task<bool> AutoSelectGuideStar();

        [OperationContract]
        Task<bool> StartPause(bool pause);

        [OperationContract]
        void CancelStartPause();

        [OperationContract]
        Task<bool> StopGuiding();

        [OperationContract]
        void CancelStopGuiding();

        [OperationContract]
        Task<bool> SynchronizedDither();

        [OperationContract]
        void CancelSynchronizedDither();
    }

    internal class PHD2Fault { }
}