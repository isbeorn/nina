using NINA.Model;
using NINA.Model.MyGuider;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IGuiderMediator : IDeviceMediator<IGuiderVM, IGuiderConsumer, GuiderInfo> {

        Task<bool> Dither(CancellationToken token);

        Guid StartRMSRecording();

        RMS StopRMSRecording(Guid handle);

        Task<bool> StartGuiding(CancellationToken token);

        Task<bool> StopGuiding(CancellationToken token);

        Task<bool> ResumeGuiding(CancellationToken token);

        Task<bool> PauseGuiding(CancellationToken token);

        Task<bool> AutoSelectGuideStar(CancellationToken token);
    }
}