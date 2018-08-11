using NINA.Model;
using NINA.Model.MyGuider;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class GuiderMediator : DeviceMediator<IGuiderVM, IGuiderConsumer, GuiderInfo> {

        public Task<bool> Dither(CancellationToken token) {
            return handlerVM.Dither(token);
        }

        public Guid StartRMSRecording() {
            return handlerVM.StartRMSRecording();
        }

        public RMS StopRMSRecording(Guid handle) {
            return handlerVM.StopRMSRecording(handle);
        }

        public Task<bool> StartGuiding(CancellationToken token) {
            return handlerVM.StartGuiding(token);
        }

        public Task<bool> StopGuiding(CancellationToken token) {
            return handlerVM.StopGuiding(token);
        }

        public Task<bool> ResumeGuiding(CancellationToken token) {
            return handlerVM.ResumeGuiding(token);
        }

        public Task<bool> PauseGuiding(CancellationToken token) {
            return handlerVM.PauseGuiding(token);
        }

        public Task<bool> AutoSelectGuideStar(CancellationToken token) {
            return handlerVM.AutoSelectGuideStar(token);
        }

        internal override void BroadcastInfo(GuiderInfo deviceInfo) {
            foreach (IGuiderConsumer vm in vms) {
                vm.UpdateGuiderInfo(deviceInfo);
            }
        }
    }
}