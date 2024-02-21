using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK;
using NINA.Equipment.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFilterWheel {
    public class PlayerOneFilterWheel : BaseINPC, IFilterWheel {
        private int id;
        private IProfileService profileService;

        public PlayerOneFilterWheel(int idx, IProfileService profileService) {
            _ = PlayerOneFilterWheelSDK.POAGetPWProperties(idx, out var info);
            this.info = info;
            this.id = info.Handle;
            this.profileService = profileService;
        }

        public short InterfaceVersion => 1;

        public int[] FocusOffsets => this.Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => this.Filters.Select((x) => x.Name).ToArray();

        public IList<string> SupportedActions => new List<string>();
        private object lockObj = new object();
        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                lock (lockObj) {
                    var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                    var positions = info.PositionCount;

                    return new FilterManager().SyncFiltersWithPositions(filtersList, positions);
                }
            }
        }

        public bool HasSetupDialog { get; } = false;

        public string Id => $"{Category}_{Name}_{id}";

        public string Name => $"{info.Name}";
        public string DisplayName => Name;

        public string Category => "Player One";

        private bool _connected = false;
        private PlayerOneFilterWheelSDK.PWProperties info;

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description => "Native driver for PlayerOne filter wheels";

        public string DriverInfo => "Native driver for PlayerOne filter wheels";

        public string DriverVersion => "1.0";

        public short Position {
            get {
                var err = PlayerOneFilterWheelSDK.POAGetCurrentPosition(this.id, out var position);
                if (err == PlayerOneFilterWheelSDK.PWErrors.PW_OK) {
                    return (short)position;
                } else {
                    Logger.Error($"PlayerOne FilterWheel Communication error to get position {err}");
                    return -1;
                }
            }

            set {
                var err = PlayerOneFilterWheelSDK.POAGotoPosition(this.id, value);
                if (err != PlayerOneFilterWheelSDK.PWErrors.PW_OK) {
                    Logger.Error($"PlayerOne FilterWheel Communication error during position change {err}");
                }
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                if (PlayerOneFilterWheelSDK.POAOpenPW(this.id) == PlayerOneFilterWheelSDK.PWErrors.PW_OK) {
                    Connected = true;

                    PlayerOneFilterWheelSDK.POAGetPWPropertiesByHandle(this.id, out var info);
                    this.info = info;

                    PlayerOneFilterWheelSDK.POASetOneWay(this.id, true);

                    Connected = true;
                    return true;
                } else {
                    Logger.Error("Failed to connect to PlayerOne FilterWheel");
                    return false;
                };
            });
        }

        public void Disconnect() {
            _ = PlayerOneFilterWheelSDK.POAClosePW(this.id);
            this.Connected = false;
        }

        public void SetupDialog() {
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}
