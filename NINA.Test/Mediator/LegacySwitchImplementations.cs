using NINA.Core.Model;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.Test.Mediator {
    internal sealed class LegacySwitchMediator : DeviceMediator<ISwitchVM, ISwitchConsumer, SwitchInfo>, ISwitchMediator {
        [Obsolete("Use SetSwitchValueByIdentity. Index-based writes are retained only when identity support is unavailable.")]
        public Task SetSwitchValue(short switchIndex, double value, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return handler.SetSwitchValue(switchIndex, value, progress, ct);
        }
    }

    internal sealed class LegacySwitchVM : ISwitchVM {
        private readonly SwitchInfo info;

        internal LegacySwitchVM(SwitchInfo? info = null) {
            this.info = info ?? new SwitchInfo();
        }

        public int NumericWrites { get; private set; }

        public short LastNumericIndex { get; private set; }

        [Obsolete("Use SetSwitchValueByIdentity. Index-based writes are retained only when identity support is unavailable.")]
        public Task SetSwitchValue(short switchIndex, double value, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            NumericWrites++;
            LastNumericIndex = switchIndex;
            return Task.CompletedTask;
        }

        public SwitchInfo GetDeviceInfo() => info;

        public Task<IList<string>> Rescan() => Task.FromResult<IList<string>>(Array.Empty<string>());

        public Task<bool> Connect() => Task.FromResult(false);

        public Task Disconnect() => Task.CompletedTask;

        public IDevice GetDevice() => throw new NotSupportedException();

        public string Action(string actionName, string actionParameters) => throw new NotSupportedException();

        public string SendCommandString(string command, bool raw = true) => throw new NotSupportedException();

        public bool SendCommandBool(string command, bool raw = true) => throw new NotSupportedException();

        public void SendCommandBlind(string command, bool raw = true) => throw new NotSupportedException();

        public event Func<object, EventArgs, Task> Connected {
            add { }
            remove { }
        }

        public event Func<object, EventArgs, Task> Disconnected {
            add { }
            remove { }
        }

        public event PropertyChangedEventHandler? PropertyChanged {
            add { }
            remove { }
        }

        public bool CanClose { get; set; }

        public string ContentId => "LegacySwitchVM";

        public ICommand HideCommand => null!;

        public GeometryGroup ImageGeometry { get; set; } = null!;

        public bool IsClosed { get; set; }

        public bool HasSettings { get; set; }

        public bool IsVisible { get; set; }

        public bool IsTool => false;

        public string Title { get; set; } = string.Empty;

        public void Hide(object o) {
            IsVisible = false;
        }
    }
}
