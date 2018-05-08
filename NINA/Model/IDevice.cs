using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model {

    internal interface IDevice : INotifyPropertyChanged {
        bool HasSetupDialog { get; }
        string Id { get; }
        string Name { get; }

        Task<bool> Connect(CancellationToken token);

        void Disconnect();

        void SetupDialog();
    }
}