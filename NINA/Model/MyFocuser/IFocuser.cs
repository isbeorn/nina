using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {

    internal interface IFocuser : IDevice {
        bool IsMoving { get; }
        int MaxIncrement { get; }
        int MaxStep { get; }
        int Position { get; }
        double StepSize { get; }
        bool TempCompAvailable { get; }
        bool TempComp { get; set; }
        double Temperature { get; }

        Task Move(int position, CancellationToken ct);

        void Halt();
    }
}