using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class CameraMediator : DeviceMediator<ICameraVM, ICameraConsumer, CameraInfo> {

        internal Task Capture(double exposureTime, bool isLightFrame, CancellationToken token, IProgress<ApplicationStatus> progress) {
            return handlerVM.Capture(exposureTime, isLightFrame, token, progress);
        }

        internal IAsyncEnumerable<ImageArray> LiveView(CancellationToken token) {
            return handlerVM.LiveView(token);
        }

        internal Task<ImageArray> Download(CancellationToken token) {
            return handlerVM.Download(token);
        }

        internal void AbortExposure() {
            handlerVM.AbortExposure();
        }

        internal void SetBinning(short x, short y) {
            handlerVM.SetBinning(x, y);
        }

        internal void SetGain(short gain) {
            handlerVM.SetGain(gain);
        }

        internal void SetSubSample(bool subSample) {
            handlerVM.SetSubSample(subSample);
        }

        internal void SetSubSampleArea(int x, int y, int width, int height) {
            handlerVM.SetSubSampleArea(x, y, width, height);
        }

        /// <summary>
        /// Updates all consumers with the current camera info
        /// </summary>
        /// <param name="cameraInfo"></param>
        override internal void BroadcastInfo(CameraInfo cameraInfo) {
            foreach (ICameraConsumer vm in vms) {
                vm.UpdateCameraInfo(cameraInfo);
            }
        }
    }
}