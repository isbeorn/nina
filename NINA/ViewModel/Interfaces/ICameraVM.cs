using NINA.Model;
using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    internal interface ICameraVM {

        void SetBinning(short x, short y);

        void SetGain(short gain);

        void SetSubSample(bool subSample);

        void SetSubSampleArea(int x, int y, int width, int height);

        void AbortExposure();

        Task Capture(double exposureTime, bool isLightFrame, CancellationToken token, IProgress<ApplicationStatus> progress);

        Task LiveView(CancellationToken token);

        Task<ImageArray> Download(CancellationToken token);

        Task<bool> ChooseCamera();

        void Disconnect();
    }
}