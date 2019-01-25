using NINA.Utility;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1998

namespace NINA.Model.MyGuider {

    public class DummyGuider : BaseINPC, IGuider {
        public string Name => "Dummy";

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public double PixelScale { get; set; }
        public string State => "Being a dummy";
        public IGuideStep GuideStep { get; }

        public async Task<bool> Connect() {
            Connected = true;

            return Connected;
        }

        public async Task<bool> AutoSelectGuideStar() {
            return true;
        }

        public bool Disconnect() {
            Connected = false;

            return Connected;
        }

        public async Task<bool> Pause(bool pause, CancellationToken ct) {
            return true;
        }

        public async Task<bool> StartGuiding(CancellationToken ct) {
            return true;
        }

        public async Task<bool> StopGuiding(CancellationToken ct) {
            return true;
        }

        public async Task<bool> Dither(CancellationToken ct) {
            return true;
        }
    }
}