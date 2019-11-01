using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    public interface IFlatDevice : IDevice {
        CoverState CoverState { get; }

        int MaxBrightness { get; }

        int MinBrightness { get; }

        Task<bool> Open(CancellationToken ct);

        Task<bool> Close(CancellationToken ct);

        bool LightOn { get; set; }

        int Brightness { get; set; }

        string PortName { get; set; }

        bool SupportsOpenClose { get; }
    }

    public enum CoverState { Unknown, NeitherOpenNorClosed, Closed, Open };
}