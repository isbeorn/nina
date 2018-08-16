using NINA.Model;
using NINA.Model.MyGuider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Interfaces {

    internal interface IGuiderVM : IDeviceVM<GuiderInfo> {

        Task<bool> Dither(CancellationToken token);

        Guid StartRMSRecording();

        Task<bool> StartGuiding(CancellationToken token);

        Task<bool> StopGuiding(CancellationToken token);

        Task<bool> PauseGuiding(CancellationToken token);

        Task<bool> ResumeGuiding(CancellationToken token);

        Task<bool> AutoSelectGuideStar(CancellationToken token);

        RMS StopRMSRecording(Guid handle);
    }
}