using NINA.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    internal class AlnitakFlipFlatSimulator : BaseINPC, IFlatDevice {

        public AlnitakFlipFlatSimulator() {
            CoverState = CoverState.NeitherOpenNorClosed;
        }

        public bool HasSetupDialog => false;
        public string Id => null;
        public string Name => "Flip-Flat Simulator";
        public string Category => "Alnitak Astrosystems";

        public bool Connected { get; private set; }

        public string Description => $"{Name} on port COM99. Firmware version: 200";
        public string DriverInfo => "Simulates an Alnitak FlipFlat.";
        public string DriverVersion => "1.0";

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;

            return Task.Run(() => Connected, token);
        }

        public void Disconnect() {
            Connected = false;
        }

        public void SetupDialog() {
        }

        public CoverState CoverState { get; private set; }

        public int MaxBrightness => 255;
        public int MinBrightness => 0;

        public async Task<bool> Open(CancellationToken ct) {
            if (!Connected) await Task.Run(() => false, ct);
            return await Task.Run(() => {
                _lightOn = false;
                CoverState = CoverState.NeitherOpenNorClosed;
                Thread.Sleep(2000);
                CoverState = CoverState.Open;
                return true;
            }, ct);
        }

        public async Task<bool> Close(CancellationToken ct) {
            if (!Connected) await Task.Run(() => false, ct);
            return await Task.Run(() => {
                CoverState = CoverState.NeitherOpenNorClosed;
                Thread.Sleep(2000);
                CoverState = CoverState.Closed;
                return true;
            }, ct);
        }

        private bool _lightOn;

        public bool LightOn {
            get {
                if (!Connected) {
                    return false;
                }

                return CoverState == CoverState.Closed && _lightOn;
            }
            set {
                if (!Connected) return;
                if (CoverState != CoverState.Closed) return;
                _lightOn = value;
                RaisePropertyChanged();
            }
        }

        private int _brightness;

        public int Brightness {
            get => !Connected ? 0 : _brightness;
            set {
                if (Connected) {
                    if (value < MinBrightness) {
                        value = MinBrightness;
                    }
                    if (value > MaxBrightness) {
                        value = MaxBrightness;
                    }
                    _brightness = value;
                }
                RaisePropertyChanged();
            }
        }

        public bool SupportsOpenClose => true;
    }
}