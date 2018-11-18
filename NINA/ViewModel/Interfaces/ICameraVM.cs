using NINA.Model;
using NINA.Model.MyCamera;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Interfaces {

    internal interface ICameraVM : IDeviceVM<CameraInfo> {

        void SetBinning(short x, short y);

        void SetGain(short gain);

        void SetSubSample(bool subSample);

        void SetSubSampleArea(int x, int y, int width, int height);

        void AbortExposure();

        Task Capture(double exposureTime, bool isLightFrame, CancellationToken token, IProgress<ApplicationStatus> progress);

        IAsyncEnumerable<ImageArray> LiveView(CancellationToken token);

        Task<ImageArray> Download(CancellationToken token);
    }
}