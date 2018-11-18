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

    internal class CameraMediator : DeviceMediator<ICameraVM, ICameraConsumer, CameraInfo>, ICameraMediator {

        public Task Capture(double exposureTime, bool isLightFrame, CancellationToken token, IProgress<ApplicationStatus> progress) {
            return handler.Capture(exposureTime, isLightFrame, token, progress);
        }

        public IAsyncEnumerable<ImageArray> LiveView(CancellationToken token) {
            return handler.LiveView(token);
        }

        public Task<ImageArray> Download(CancellationToken token) {
            return handler.Download(token);
        }

        public void AbortExposure() {
            handler.AbortExposure();
        }

        public void SetBinning(short x, short y) {
            handler.SetBinning(x, y);
        }

        public void SetGain(short gain) {
            handler.SetGain(gain);
        }

        public void SetSubSample(bool subSample) {
            handler.SetSubSample(subSample);
        }

        public void SetSubSampleArea(int x, int y, int width, int height) {
            handler.SetSubSampleArea(x, y, width, height);
        }
    }
}