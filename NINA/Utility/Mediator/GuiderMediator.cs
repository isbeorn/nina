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
            return handler.Dither(token);
        }

        public Guid StartRMSRecording() {
            return handler.StartRMSRecording();
        }

        public RMS StopRMSRecording(Guid handle) {
            return handler.StopRMSRecording(handle);
        }

        public Task<bool> StartGuiding(CancellationToken token) {
            return handler.StartGuiding(token);
        }

        public Task<bool> StopGuiding(CancellationToken token) {
            return handler.StopGuiding(token);
        }

        public Task<bool> ResumeGuiding(CancellationToken token) {
            return handler.ResumeGuiding(token);
        }

        public Task<bool> PauseGuiding(CancellationToken token) {
            return handler.PauseGuiding(token);
        }

        public Task<bool> AutoSelectGuideStar(CancellationToken token) {
            return handler.AutoSelectGuideStar(token);
        }
    }
}