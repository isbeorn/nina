#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    [ServiceContract]
    internal interface ISynchronizedPHD2GuiderService {

        /// <summary>
        /// Forwards AutoSelectGuideStar to the PHD2 instance.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Task<bool> AutoSelectGuideStar();

        /// <summary>
        /// Cancels the StartGuiding request.
        /// </summary>
        [OperationContract]
        void CancelStartGuiding();

        /// <summary>
        /// Cancels the StopGuiding request.
        /// </summary>
        [OperationContract]
        void CancelStopGuiding();

        /// <summary>
        /// Cancels an ongoing Dither request.
        /// </summary>
        [OperationContract]
        void CancelSynchronizedDither();

        /// <summary>
        /// Connects and initializes a client to the service. Has to be called first. Returns the PixelScale and adds the client to the internal client list of the service.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(PHD2Fault))]
        [FaultContract(typeof(ClientAlreadyExistsFault))]
        Task<double> ConnectAndGetPixelScale(Guid clientId);

        /// <summary>
        /// Removes the client from the internal client list of the service.
        /// </summary>
        /// <param name="clientId"></param>
        [OperationContract]
        void DisconnectClient(Guid clientId);

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
        /// Has to be called once, will launch and/or connect to PHD2. Replaces the constructor.
        /// </summary>
        /// <param name="guider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> Initialize(IGuider guider, CancellationToken ct);

        /// <summary>
        /// Forwards StartGuiding to the PHD2 instance and thus initiates guiding. Can be called from multiple instances simultaneously.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Task<bool> StartGuiding();

        /// <summary>
        /// Forwards StopGuiding to the PHD2 instance and stops guiding. Can be called from multiple instances simultaneously.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Task<bool> StopGuiding();

        /// <summary>
        /// Request to Dither to the PHD2 instance. Will return immediately if should not dither or will wait for the other client to call the same method to synchronize a Dither request.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [OperationContract]
        Task<bool> SynchronizedDither(Guid clientId);

        /// <summary>
        /// Method to sync current camera information to the service. Required so clients know when they are able to dither and when not.
        /// </summary>
        /// <param name="profileCameraState"></param>
        /// <returns></returns>
        [OperationContract]
        Task UpdateCameraInfo(ProfileCameraState profileCameraState);
    }

    [DataContract]
    internal class PHD2Fault { }

    [DataContract]
    internal class ClientAlreadyExistsFault { }
}