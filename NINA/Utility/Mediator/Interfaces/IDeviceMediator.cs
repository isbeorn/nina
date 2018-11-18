using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IDeviceMediator<THandler, TConsumer, TInfo> : IMediator<THandler> where THandler : IDeviceVM<TInfo> where TConsumer : IDeviceConsumer<TInfo> {

        void RegisterConsumer(TConsumer consumer);

        void RemoveConsumer(TConsumer consumer);

        Task<bool> Connect();

        void Disconnect();

        void Broadcast(TInfo deviceInfo);
    }
}