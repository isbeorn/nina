using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal abstract class DeviceMediator<THandler, TConsumer, TInfo> where THandler : IDeviceVM {
        protected THandler handlerVM;
        protected List<TConsumer> vms = new List<TConsumer>();

        internal void RegisterVM(THandler telescopeVM) {
            this.handlerVM = telescopeVM;
        }

        internal void RegisterConsumer(TConsumer vm) {
            vms.Add(vm);
        }

        internal void RemoveConsumer(TConsumer vm) {
            vms.Remove(vm);
        }

        internal Task<bool> Connect() {
            return handlerVM.Connect();
        }

        internal void Disconnect() {
            handlerVM.Disconnect();
        }

        abstract internal void BroadcastInfo(TInfo deviceInfo);
    }
}