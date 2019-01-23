using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    [ServiceContract]
    internal interface ISynchronizedPHD2GuiderService {

        /// <summary>
        /// Has to be called once, will launch and/or connect to PHD2. Replaces the constructor.
        /// </summary>
        /// <param name="guider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> Initialize(IGuider guider, CancellationToken ct);

        /// <summary>
        /// Returns current GuideInfo containing the latest GuideStep and GuideState of PHD2. Also acts as a keep-alive string from the client to the service.
        /// Updates the services internal IsAlive and LastPing of the corresponding clientId.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(PHD2Fault))]
        Task<GuideInfo> GetGuideInfo(Guid clientId);

        /// <summary>
        /// Method to sync current camera information to the service. Required so clients know when they are able to dither and when not.
        /// </summary>
        /// <param name="profileCameraState"></param>
        /// <returns></returns>
        [OperationContract]
        Task UpdateCameraInfo(ProfileCameraState profileCameraState);

        /// <summary>
        /// Connects and initializes a client to the service. Has to be called first. Returns the PixelScale and adds the client to the internal client list of the service.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(PHD2Fault))]
        Task<double> ConnectAndGetPixelScale(Guid clientId);

        /// <summary>
        /// Removes the client from the internal client list of the service.
        /// </summary>
        /// <param name="clientId"></param>
        [OperationContract]
        void DisconnectClient(Guid clientId);

        /// <summary>
        /// Forwards StartGuiding to the PHD2 instance and thus initiates guiding. Can be called from multiple instances simultaneously.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Task<bool> StartGuiding();

        /// <summary>
        /// Cancels the StartGuiding request.
        /// </summary>
        [OperationContract]
        void CancelStartGuiding();

        /// <summary>
        /// Forwards AutoSelectGuideStar to the PHD2 instance.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Task<bool> AutoSelectGuideStar();

        /// <summary>
        /// Forwards the Pause or Resume command to the PHD2 instance and pauses or resumes PHD2. Can be called from multiple instances simultaneously.
        /// </summary>
        /// <param name="pause"></param>
        /// <returns></returns>
        [OperationContract]
        Task<bool> StartPause(bool pause);

        /// <summary>
        /// Cancels the StartPause request.
        /// </summary>
        [OperationContract]
        void CancelStartPause();

        /// <summary>
        /// Forwards StopGuiding to the PHD2 instance and stops guiding. Can be called from multiple instances simultaneously.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Task<bool> StopGuiding();

        /// <summary>
        /// Cancels the StopGuiding request.
        /// </summary>
        [OperationContract]
        void CancelStopGuiding();

        /// <summary>
        /// Request to Dither to the PHD2 instance. Will return immediately if should not dither or will wait for the other client to call the same method to synchronize a Dither request.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [OperationContract]
        Task<bool> SynchronizedDither(Guid clientId);

        /// <summary>
        /// Cancels an ongoing Dither request.
        /// </summary>
        [OperationContract]
        void CancelSynchronizedDither();
    }

    [DataContract]
    internal class PHD2Fault { }
}