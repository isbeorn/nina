using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;

namespace NINA.ViewModel.Equipment.FlatDevice {

    internal class FlatDeviceVM : DockableVM, IFlatDeviceVM, IFilterWheelConsumer {
        private IFlatDevice _flatDevice;
        private IFlatDeviceSettings _flatDeviceSettings;
        private readonly IApplicationStatusMediator _applicationStatusMediator;
        private readonly IFilterWheelMediator _filterWheelMediator;
        private readonly IFlatDeviceMediator _flatDeviceMediator;
        private readonly DeviceUpdateTimer _updateTimer;

        public FlatDeviceVM(IProfileService profileService, IFlatDeviceMediator flatDeviceMediator, IApplicationStatusMediator applicationStatusMediator, IFilterWheelMediator filterWheelMediator) : base(profileService) {
            _applicationStatusMediator = applicationStatusMediator;
            _filterWheelMediator = filterWheelMediator;
            _filterWheelMediator.RegisterConsumer(this);
            _flatDeviceMediator = flatDeviceMediator;
            _flatDeviceMediator.RegisterHandler(this);

            ConnectCommand = new AsyncCommand<bool>(Connect);
            CancelConnectCommand = new RelayCommand(CancelConnectFlatDevice);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectFlatDeviceDialog());
            OpenCoverCommand = new AsyncCommand<bool>(OpenCover);
            CloseCoverCommand = new AsyncCommand<bool>(CloseCover);
            RefreshFlatDeviceListCommand =
                new RelayCommand(RefreshFlatDeviceList, o => _flatDevice?.Connected != true);
            SetBrightnessCommand = new RelayCommand(SetBrightness);
            ToggleLightCommand = new RelayCommand(ToggleLight);
            ClearValuesCommand = new RelayCommand(ClearWizardTrainedValues);

            FlatDeviceChooserVM = new FlatDeviceChooserVM(profileService);
            FlatDeviceChooserVM.GetEquipment();

            _updateTimer = new DeviceUpdateTimer(
                GetFlatDeviceValues,
                UpdateFlatDeviceValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            _flatDeviceSettings = profileService.ActiveProfile.FlatDeviceSettings;
            _flatDeviceSettings.PropertyChanged += SettingsChanged;
            profileService.ProfileChanged += (object sender, EventArgs e) => {
                _flatDeviceSettings.PropertyChanged -= SettingsChanged;
                RefreshFlatDeviceList(null);
                _flatDeviceSettings = profileService.ActiveProfile.FlatDeviceSettings;
                _flatDeviceSettings.PropertyChanged += SettingsChanged;
            };
        }

        private void BroadcastFlatDeviceInfo() {
            _flatDeviceMediator.Broadcast(GetDeviceInfo());
        }

        private double _brightness;

        public double Brightness {
            get => _brightness;

            set { _brightness = value; RaisePropertyChanged(); }
        }

        public bool LightOn { get; set; }

        public void SetBrightness(double value) {
            Brightness = value;
            SetBrightness(null);
        }

        public void SetBrightness(object o) {
            if (_flatDevice == null || !_flatDevice.Connected) return;
            _flatDevice.Brightness = Brightness;
        }

        private readonly SemaphoreSlim ssConnect = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _connectFlatDeviceCts;

        public async Task<bool> Connect() {
            await ssConnect.WaitAsync();
            try {
                await Disconnect();
                if (_updateTimer != null) {
                    await _updateTimer.Stop();
                }

                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null) return false;
                if (device.Id == "No_Device") {
                    profileService.ActiveProfile.FlatDeviceSettings.Id = FlatDeviceChooserVM.SelectedDevice.Id;
                    return false;
                }

                _applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );
                var flatDevice = (IFlatDevice)device;
                _connectFlatDeviceCts?.Dispose();
                _connectFlatDeviceCts = new CancellationTokenSource();
                try {
                    var connected = await flatDevice.Connect(_connectFlatDeviceCts.Token);
                    _connectFlatDeviceCts.Token.ThrowIfCancellationRequested();
                    if (connected) {
                        _flatDevice = flatDevice;
                        FlatDeviceInfo = new FlatDeviceInfo {
                            MinBrightness = flatDevice.MinBrightness,
                            MaxBrightness = flatDevice.MaxBrightness,
                            Brightness = flatDevice.Brightness,
                            Connected = flatDevice.Connected,
                            CoverState = flatDevice.CoverState,
                            Description = flatDevice.Description,
                            DriverInfo = flatDevice.DriverInfo,
                            DriverVersion = flatDevice.DriverVersion,
                            LightOn = flatDevice.LightOn,
                            Name = flatDevice.Name,
                            SupportsOpenClose = flatDevice.SupportsOpenClose
                        };
                        this.Brightness = flatDevice.Brightness;

                        Notification.ShowSuccess(Loc.Instance["LblFlatDeviceConnected"]);

                        if (_updateTimer != null) {
                            _updateTimer.Interval =
                                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            _updateTimer.Start();
                        }

                        profileService.ActiveProfile.FlatDeviceSettings.Id = flatDevice.Id;

                        Logger.Info($"Successfully connected Flatdevice. Id: {flatDevice.Id} Name: {flatDevice.Name} Driver Version: {flatDevice.DriverVersion}");

                        return true;
                    } else {
                        FlatDeviceInfo.Connected = false;
                        _flatDevice = null;
                        return false;
                    }
                } catch (OperationCanceledException) {
                    if (FlatDeviceInfo.Connected) {
                        await Disconnect();
                    }

                    return false;
                }
            } finally {
                ssConnect.Release();
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

        public async Task Disconnect() {
            if (!FlatDeviceInfo.Connected) return;
            if (_updateTimer != null) {
                await _updateTimer.Stop();
            }
            _flatDevice?.Disconnect();
            _flatDevice = null;
            FlatDeviceInfo = DeviceInfo.CreateDefaultInstance<FlatDeviceInfo>();
            BroadcastFlatDeviceInfo();
            Logger.Info("Disconnected Flat Device");
        }

        private async Task<bool> DisconnectFlatDeviceDialog() {
            var dialog = MyMessageBox.MyMessageBox.Show(Loc.Instance["LblFlatDeviceDisconnectQuestion"],
                "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (dialog == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        private readonly SemaphoreSlim ssOpen = new SemaphoreSlim(1, 1);

        public async Task<bool> OpenCover() {
            await ssOpen.WaitAsync();
            try {
                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null || device.Id == "No_Device") return false;
                var flatDevice = (IFlatDevice)device;
                if (flatDevice.Connected == false) return false;
                if (!flatDevice.SupportsOpenClose) return false;
                return await flatDevice.Open(new CancellationToken());
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                ssOpen.Release();
            }
        }

        private readonly SemaphoreSlim ssClose = new SemaphoreSlim(1, 1);

        public async Task<bool> CloseCover() {
            await ssClose.WaitAsync();
            try {
                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null || device.Id == "No_Device") return false;
                var flatDevice = (IFlatDevice)device;
                if (flatDevice.Connected == false) return false;
                if (!flatDevice.SupportsOpenClose) return false;
                return await flatDevice.Close(new CancellationToken());
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                ssClose.Release();
            }
        }

        private FlatDeviceInfo _flatDeviceInfo;
        private FilterWheelInfo _filterWheelInfo;

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

        public IFlatDeviceChooserVM FlatDeviceChooserVM { get; set; }

        private void UpdateFlatDeviceValues(Dictionary<string, object> flatDeviceValues) {
            object o = null;
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Connected), out o);
            _flatDeviceInfo.Connected = (bool)(o ?? false);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.CoverState), out o);
            _flatDeviceInfo.CoverState = (CoverState)(o ?? CoverState.Unknown);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Brightness), out o);
            _flatDeviceInfo.Brightness = (double)(o ?? 0.0);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.MinBrightness), out o);
            _flatDeviceInfo.MinBrightness = (int)(o ?? 0);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.MaxBrightness), out o);
            _flatDeviceInfo.MaxBrightness = (int)(o ?? 0);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.LightOn), out o);
            _flatDeviceInfo.LightOn = (bool)(o ?? false);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.SupportsOpenClose), out o);
            _flatDeviceInfo.SupportsOpenClose = (bool)(o ?? false);

            BroadcastFlatDeviceInfo();
        }

        private Dictionary<string, object> GetFlatDeviceValues() {
            var flatDeviceValues = new Dictionary<string, object>
            {
                {nameof(FlatDeviceInfo.Connected), _flatDevice?.Connected ?? false},
                {nameof(FlatDeviceInfo.CoverState), _flatDevice?.CoverState ?? CoverState.Unknown},
                {nameof(FlatDeviceInfo.Brightness), _flatDevice?.Brightness ?? 0.0},
                {nameof(FlatDeviceInfo.MinBrightness), _flatDevice?.MinBrightness ?? 0},
                {nameof(FlatDeviceInfo.MaxBrightness), _flatDevice?.MaxBrightness ?? 0},
                {nameof(FlatDeviceInfo.LightOn), _flatDevice?.LightOn ?? false},
                {nameof(FlatDeviceInfo.SupportsOpenClose), _flatDevice?.SupportsOpenClose ?? false}
            };
            return flatDeviceValues;
        }

        public void ToggleLight(object o) {
            if (_flatDevice == null || _flatDevice.Connected == false) return;
            _flatDevice.LightOn = (bool)o;
        }

        public void UpdateDeviceInfo(FilterWheelInfo info) {
            if (_filterWheelInfo == info) return;
            _filterWheelInfo = info;
            UpdateWizardTrainedValues();
            RaisePropertyChanged(nameof(WizardTrainedValues));
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            UpdateWizardTrainedValues();
            RaisePropertyChanged(nameof(WizardTrainedValues));
        }

        private void UpdateWizardTrainedValues() {
            WizardTrainedValues = new DataTable();
            var filters = _filterWheelMediator.GetAllFilters() ?? new List<FilterInfo> { new FilterInfo() };

            var binningModes = new List<BinningMode>();
            var gains = new List<short>();
            WizardTrainedValues.Columns.Add($"{Loc.Instance["LblBinning"]}\n{Loc.Instance["LblGain"]}", typeof(string));
            binningModes.AddRange(profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings());
            gains.AddRange(profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains());

            var keys = new List<(BinningMode binning, short gain)>();
            foreach (var binningMode in binningModes.Distinct().OrderBy(mode => mode.Name)) {
                foreach (var gain in gains.Distinct().OrderBy(s => s)) {
                    WizardTrainedValues.Columns.Add($"{binningMode}\n{gain}", typeof(string));
                    keys.Add((binningMode, gain));
                }
            }

            foreach (var filter in filters) {
                var row = new List<object> { filter.Name ?? Loc.Instance["LblNoFilterwheel"] };
                row.AddRange(keys.Select(
                    key => profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                        new FlatDeviceFilterSettingsKey(filter.Name, key.binning, key.gain))).Select(
                    info => info != null ? $"{info.Time,3:0.0}s @ {info.Brightness,3:P0}" : "-"));
                WizardTrainedValues.Rows.Add(row.ToArray());
            }
        }

        public DataTable WizardTrainedValues { get; private set; }

        private void ClearWizardTrainedValues(object o) {
            profileService.ActiveProfile.FlatDeviceSettings.ClearBrightnessInfo();
        }

        public ICommand RefreshFlatDeviceListCommand { get; }
        public IAsyncCommand ConnectCommand { get; }
        public ICommand CancelConnectCommand { get; }
        public IAsyncCommand DisconnectCommand { get; }
        public IAsyncCommand OpenCoverCommand { get; }
        public IAsyncCommand CloseCoverCommand { get; }
        public ICommand ToggleLightCommand { get; }
        public ICommand SetBrightnessCommand { get; }
        public ICommand ClearValuesCommand { get; }

        public void Dispose() {
            _filterWheelMediator.RemoveConsumer(this);
        }
    }
}