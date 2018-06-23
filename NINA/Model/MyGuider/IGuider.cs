using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    internal interface IGuider : INotifyPropertyChanged {
        bool Connected { get; }
        double PixelScale { get; set; }
        string State { get; }
        IGuideStep GuideStep { get; }

        Task<bool> Connect();

        Task<bool> AutoSelectGuideStar();

        bool Disconnect();

        Task<bool> Pause(bool pause, CancellationToken ct);

        Task<bool> StartGuiding(CancellationToken ct);

        Task<bool> StopGuiding(CancellationToken ct);

        Task<bool> Dither(CancellationToken ct);
    }

    public interface IGuideEvent {
        string Event { get; }
        string TimeStamp { get; }
        string Host { get; }
        int Inst { get; }
    }

    public interface IGuiderAppState {
        string State { get; }
    }

    public interface IGuideStep : IGuideEvent {
        double Frame { get; }
        double Time { get; }
        double TimeRA { get; }
        double TimeDec { get; }
        string Mount { get; }
        double Dx { get; }
        double Dy { get; }
        double RADistanceRaw { get; set; }
        double DecDistanceRaw { get; set; }
        double RADistanceGuide { get; set; }
        double DecDistanceGuide { get; set; }
        double RADistanceRawDisplay { get; set; }
        double DecDistanceRawDisplay { get; set; }
        double RADistanceGuideDisplay { get; set; }
        double DecDistanceGuideDisplay { get; set; }
        double RADuration { get; }
        string RADirection { get; }
        double DECDuration { get; }
        string DecDirection { get; }
        double StarMass { get; }
        double SNR { get; }
        double AvgDist { get; }
        bool RALimited { get; }
        bool DecLimited { get; }
        double ErrorCode { get; }

        IGuideStep Clone();
    }
}