using NINA.Profile;
using NINA.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1998

namespace NINA.Model.MyGuider {

    public class DummyGuider : BaseINPC, IGuider {
        private IProfileService profileService;

        public DummyGuider(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Name => Locale.Loc.Instance["LblNoGuider"];

        public string Id => "No_Guider";

        private bool _connected;

        public event EventHandler<IGuideStep> GuideEvent;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public double PixelScale { get; set; }
        public string State => string.Empty;

        public async Task<bool> Connect() {
            profileService.ActiveProfile.GuiderSettings.GuiderName = Id;

            Connected = false;

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