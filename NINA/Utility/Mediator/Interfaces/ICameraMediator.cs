using NINA.Model;
using NINA.Model.MyCamera;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface ICameraMediator : IDeviceMediator<ICameraVM, ICameraConsumer, CameraInfo> {

        Task Capture(double exposureTime, bool isLightFrame, CancellationToken token, IProgress<ApplicationStatus> progress);

        IAsyncEnumerable<ImageArray> LiveView(CancellationToken token);

        Task<ImageArray> Download(CancellationToken token);

        void AbortExposure();

        void SetBinning(short x, short y);

        void SetGain(short gain);

        void SetSubSample(bool subSample);

        void SetSubSampleArea(int x, int y, int width, int height);
    }
}