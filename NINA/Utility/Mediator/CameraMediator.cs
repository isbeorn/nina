using NINA.Model;
using NINA.Model.MyCamera;
using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class CameraMediator {
        private ICameraVM cameraVM;
        private List<ICameraConsumer> vms = new List<ICameraConsumer>();

        internal void RegisterCameraVM(ICameraVM cameraVM) {
            this.cameraVM = cameraVM;
        }

        internal void RegisterConsumer(ICameraConsumer vm) {
            vms.Add(vm);
        }

        internal void RemoveConsumer(ICameraConsumer vm) {
            vms.Remove(vm);
        }

        internal Task<bool> Connect() {
            return cameraVM.ChooseCamera();
        }

        internal void Disconnect() {
            cameraVM.Disconnect();
        }

        internal Task Capture(double exposureTime, bool isLightFrame, CancellationToken token, IProgress<ApplicationStatus> progress) {
            return cameraVM.Capture(exposureTime, isLightFrame, token, progress);
        }

        internal Task LiveView(CancellationToken token) {
            return cameraVM.LiveView(token);
        }

        internal Task<ImageArray> Download(CancellationToken token) {
            return cameraVM.Download(token);
        }

        internal void AbortExposure() {
            cameraVM.AbortExposure();
        }

        internal void SetBinning(short x, short y) {
            cameraVM.SetBinning(x, y);
        }

        internal void SetGain(short gain) {
            cameraVM.SetGain(gain);
        }

        internal void SetSubSample(bool subSample) {
            cameraVM.SetSubSample(subSample);
        }

        internal void SetSubSampleArea(int x, int y, int width, int height) {
            cameraVM.SetSubSampleArea(x, y, width, height);
        }

        /// <summary>
        /// Updates all consumers with the current camera info
        /// </summary>
        /// <param name="cameraInfo"></param>
        internal void UpdateCameraInfo(CameraInfo cameraInfo) {
            foreach (ICameraConsumer vm in vms) {
                vm.UpdateCameraInfo(cameraInfo);
            }
        }
    }
}