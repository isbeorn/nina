using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.FlatDevice {

    internal class FlatDeviceVM : DockableVM, IFlatDeviceVM {
        private IFlatDevice _flatDevice;
        private FlatDeviceChooserVM _flatDeviceChooserVm;
        private IApplicationStatusMediator _applicationStatusMediator;
        private IFlatDeviceMediator _flatDeviceMediator;
        private DeviceUpdateTimer _updateTimer;

        public FlatDeviceVM(IProfileService profileService, IFlatDeviceMediator flatDeviceMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            _applicationStatusMediator = applicationStatusMediator;
            _flatDeviceMediator = flatDeviceMediator;

            ConnectCommand = new AsyncCommand<bool>(() => Connect());
            CancelConnectCommand = new RelayCommand(CancelConnectFlatDevice);
            DisconnectCommand = new RelayCommand(DisconnectFlatDeviceDialog);
            RefreshFlatDeviceListCommand =
                new RelayCommand(RefreshFlatDeviceList, o => _flatDevice?.Connected != true);

            _updateTimer = new DeviceUpdateTimer(
                GetFlatDeviceValues,
                UpdateFlatDeviceValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => { RefreshFlatDeviceList(null); };
        }

        private void BroadcastFlatDeviceInfo() {
            _flatDeviceMediator.Broadcast(GetDeviceInfo());
        }

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _connectFlatDeviceCts;

        public async Task<bool> Connect() {
            await ss.WaitAsync();
            try {
                Disconnect();
                _updateTimer?.Stop();

                if (FlatDeviceChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.FlatDeviceSettings.Id = FlatDeviceChooserVM.SelectedDevice.Id;
                    return false;
                }

                _applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );
                var flatDevice = (IFlatDevice)FlatDeviceChooserVM.SelectedDevice;
                _connectFlatDeviceCts?.Dispose();
                _connectFlatDeviceCts = new CancellationTokenSource();
                if (flatDevice != null) {
                    try {
                        var connected = await flatDevice.Connect(_connectFlatDeviceCts.Token);
                        _connectFlatDeviceCts.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            _flatDevice = flatDevice;
                            FlatDeviceInfo = new FlatDeviceInfo {
                                Brightness = flatDevice.Brightness,
                                Connected = flatDevice.Connected,
                                CoverState = flatDevice.CoverState,
                                Description = flatDevice.Description,
                                DriverInfo = flatDevice.DriverInfo,
                                DriverVersion = flatDevice.DriverVersion,
                                LightOn = flatDevice.LightOn,
                                Name = flatDevice.Name
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblFlatDeviceConnected"]);

                            _updateTimer.Interval =
                                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            _updateTimer.Start();

                            profileService.ActiveProfile.FlatDeviceSettings.Id = flatDevice.Id;
                            return true;
                        } else {
                            FlatDeviceInfo.Connected = false;
                            _flatDevice = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (FlatDeviceInfo.Connected) {
                            Disconnect();
                        }

                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                _applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private void CancelConnectFlatDevice(object o) {
            _connectFlatDeviceCts?.Cancel();
        }

        public void Disconnect() {
            if (!FlatDeviceInfo.Connected) return;
            _flatDevice?.Disconnect();
            _flatDevice = null;
            FlatDeviceInfo = DeviceInfo.CreateDefaultInstance<FlatDeviceInfo>();
            BroadcastFlatDeviceInfo();
        }

        private void DisconnectFlatDeviceDialog(object obj) {
            var dialog = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblDisconnectFlatDeviceQuestion"],
                "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (dialog == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        private FlatDeviceInfo _flatDeviceInfo;

        public FlatDeviceInfo FlatDeviceInfo {
            get {
                if (_flatDeviceInfo != null) return _flatDeviceInfo;
                _flatDeviceInfo = DeviceInfo.CreateDefaultInstance<FlatDeviceInfo>();
                return _flatDeviceInfo;
            }
            set {
                _flatDeviceInfo = value;
                RaisePropertyChanged();
            }
        }

        public FlatDeviceInfo GetDeviceInfo() {
            return _flatDeviceInfo;
        }

        private void RefreshFlatDeviceList(object obj) {
            FlatDeviceChooserVM.GetEquipment();
        }

        public FlatDeviceChooserVM FlatDeviceChooserVM {
            get {
                if (_flatDeviceChooserVm != null) return _flatDeviceChooserVm;
                _flatDeviceChooserVm = new FlatDeviceChooserVM(profileService);
                _flatDeviceChooserVm.GetEquipment();

                return _flatDeviceChooserVm;
            }
            set => _flatDeviceChooserVm = value;
        }

        private void UpdateFlatDeviceValues(Dictionary<string, object> flatDeviceValues) {
            object o = null;
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Connected), out o);
            _flatDeviceInfo.Connected = (bool)(o ?? false);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.CoverState), out o);
            _flatDeviceInfo.CoverState = (CoverState)(o ?? CoverState.Unknown);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Brightness), out o);
            _flatDeviceInfo.Brightness = (int)(o ?? 0);
            BroadcastFlatDeviceInfo();
        }

        private Dictionary<string, object> GetFlatDeviceValues() {
            var flatDeviceValues = new Dictionary<string, object>();
            flatDeviceValues.Add(nameof(FlatDeviceInfo.Connected), _flatDevice?.Connected ?? false);
            flatDeviceValues.Add(nameof(FlatDeviceInfo.CoverState), _flatDevice?.CoverState ?? CoverState.Unknown);
            flatDeviceValues.Add(nameof(FlatDeviceInfo.Brightness), _flatDevice?.Brightness ?? 0);
            return flatDeviceValues;
        }

        public ICommand RefreshFlatDeviceListCommand { get; private set; }
        public IAsyncCommand ConnectCommand { get; private set; }
        public RelayCommand CancelConnectCommand { get; private set; }
        public RelayCommand DisconnectCommand { get; private set; }
    }
}