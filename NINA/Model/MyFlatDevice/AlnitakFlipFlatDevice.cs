using AlnitakAstrosystemsSDK;
using NINA.Profile;
using NINA.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    public class AlnitakFlatDevice : BaseINPC, IFlatDevice {
        private bool _connected = false;
        private LIBAlnitak.DeviceInfo Info;
        private IProfileService profileService;

        public AlnitakFlatDevice(string flatDevice, IProfileService profileService) {
            this.profileService = profileService;
            string[] flatDeviceInfo;

            flatDeviceInfo = flatDevice.Split(';');
            Info.Id = flatDeviceInfo[0];
        }

        public bool HasSetupDialog => false;

        public string Id => Info.Id;

        public string Name => Info.Model;

        public string Category { get; } = "Alnitak Astrosystems";

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description => string.Format($"{Info.Model} ({Info.Id}) FWRev: {Info.FWrev}");

        public string DriverInfo => string.Empty;

        public string DriverVersion => "1.0";

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                Connected = LIBAlnitak.Ping();
                return Connected;
            });
        }

        public void Disconnect() {
            Connected = false;
            //actually disconnect;
        }

        public void SetupDialog() {
        }
    }
}