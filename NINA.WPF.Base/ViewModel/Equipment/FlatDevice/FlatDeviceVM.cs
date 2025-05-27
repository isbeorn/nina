#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Extensions;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.WPF.Base.ViewModel.Equipment.FlatDevice {

    public partial class FlatDeviceVM : DockableVM, IFlatDeviceVM, ICameraConsumer {
        private IFlatDeviceSettings flatDeviceSettings;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly DeviceUpdateTimer updateTimer;
        private readonly ICameraMediator cameraMediator;

        public IDeviceChooserVM DeviceChooserVM { get; }

        public FlatDeviceVM(IProfileService profileService,
                            IFlatDeviceMediator flatDeviceMediator,
                            IApplicationStatusMediator applicationStatusMediator,
                            ICameraMediator cameraMediator,
                            IDeviceChooserVM flatDeviceChooserVm,
                            IImageGeometryProvider imageGeometryProvider) : base(profileService) {
            Title = Loc.Instance["LblFlatDevice"];
            ImageGeometry = imageGeometryProvider.GetImageGeometry("LightBulbSVG");
            HasSettings = true;

            this.applicationStatusMediator = applicationStatusMediator;
            this.flatDeviceMediator = flatDeviceMediator;
            this.flatDeviceMediator.RegisterHandler(this);
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            DeviceChooserVM = flatDeviceChooserVm;

            var progress = new Progress<ApplicationStatus>(x => { x.Source = this.Title; applicationStatusMediator.StatusUpdate(x); });

            ConnectCommand = new AsyncCommand<bool>(() => Task.Run(Connect), (object o) => DeviceChooserVM.SelectedDevice != null);
            DisconnectCommand = new AsyncCommand<bool>(() => Task.Run(DisconnectFlatDeviceDialog));
            OpenCoverCommand = new AsyncCommand<bool>(() => Task.Run(() => OpenCover(progress, CancellationToken.None)));
            CloseCoverCommand = new AsyncCommand<bool>(() => Task.Run(() => CloseCover(progress, CancellationToken.None)));
            RescanDevicesCommand =
                new AsyncCommand<bool>(async o => { await Rescan(); return true; }, o => !FlatDeviceInfo.Connected);
            _ = RescanDevicesCommand.ExecuteAsync(null);
            SetBrightnessCommand = new AsyncCommand<bool>(o => Task.Run(() => SetBrightness((int)o, progress, CancellationToken.None)));
            ToggleLightCommand = new AsyncCommand<bool>(o => Task.Run(() => ToggleLight((bool)o, progress, CancellationToken.None)));

            updateTimer = new DeviceUpdateTimer(
                GetFlatDeviceValues,
                UpdateFlatDeviceValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval,
                "FlatDevice"
            );

            flatDeviceSettings = profileService.ActiveProfile.FlatDeviceSettings;
            flatDeviceSettings.PropertyChanged += FlatDeviceSettingsChanged;
            profileService.ProfileChanged += ProfileChanged;
            profileService.ActiveProfile.FilterWheelSettings.PropertyChanged += FilterWheelSettingsChanged;
        }

        public event Func<object, EventArgs, Task> Opened;
        public event Func<object, EventArgs, Task> Closed;
        public event Func<object, FlatDeviceBrightnessChangedEventArgs, Task> BrightnessChanged;
        public event Func<object, EventArgs, Task> LightToggled;

        public async Task<IList<string>> Rescan() {
            return await Task.Run(async () => {
                await DeviceChooserVM.GetEquipment();
                return DeviceChooserVM.Devices.Select(x => x.Id).ToList();
            });
        }

        private void FlatDeviceSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
        }

        private void FilterWheelSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
        }

        private async void ProfileChanged(object sender, EventArgs e) {
            flatDeviceSettings.PropertyChanged -= FlatDeviceSettingsChanged;
            profileService.ActiveProfile.FilterWheelSettings.PropertyChanged += FilterWheelSettingsChanged;
            await RescanDevicesCommand.ExecuteAsync(null);
            flatDeviceSettings = profileService.ActiveProfile.FlatDeviceSettings;
            flatDeviceSettings.PropertyChanged += FlatDeviceSettingsChanged;
        }

        private void BroadcastFlatDeviceInfo() {
            flatDeviceMediator.Broadcast(GetDeviceInfo());
        }

        private int brightness;

        public int Brightness {
            get => brightness;
            set { brightness = value; RaisePropertyChanged(); }
        }

        private bool lightOn;

        public bool LightOn {
            get => lightOn;
            set { lightOn = value; RaisePropertyChanged(); }
        }

        public Task<bool> SetBrightness(int value, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (FlatDevice == null || !FlatDevice.Connected) return Task.FromResult(false);
            return Task.Run(async () => {
                try {
                    var from = FlatDeviceInfo.Brightness;
                    /* a value of 0 now indicates turning the device off */
                    if (value > 0 && value < FlatDevice.MinBrightness) {
                        value = FlatDevice.MinBrightness;
                    }
                    if (value > FlatDevice.MaxBrightness) {
                        value = FlatDevice.MaxBrightness;
                    }
                    if (FlatDevice.Brightness == value) {
                        return true;
                    }
                    Logger.Info($"Setting brightness to {value}");
                    progress?.Report(new ApplicationStatus() { Status = string.Format(Loc.Instance["LblSettingBrightness"], value) });
                    FlatDevice.Brightness = value;
                    var waitForUpdate = updateTimer.WaitForNextUpdate(token);
                    await CoreUtil.Delay(profileService.ActiveProfile.FlatDeviceSettings.SettleTime, token);
                    await waitForUpdate;
                    await (BrightnessChanged?.InvokeAsync(this, new FlatDeviceBrightnessChangedEventArgs(from: from, to: FlatDeviceInfo.Brightness)) ?? Task.CompletedTask);
                    return true;
                } finally {
                    progress?.Report(new ApplicationStatus());
                }
            }, token);
        }

        private Task<bool> SetBrightness(object o, CancellationToken token) {
            if (!int.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)) return Task.FromResult(false);
            return SetBrightness(result, token);
        }

        private readonly SemaphoreSlim ssConnect = new SemaphoreSlim(1, 1);
        private CancellationTokenSource connectFlatDeviceCts;

        public async Task<bool> Connect() {
            await ssConnect.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                var device = DeviceChooserVM.SelectedDevice;
                if (device == null) return false;
                if (device.Id == "No_Device") {
                    profileService.ActiveProfile.FlatDeviceSettings.Id = DeviceChooserVM.SelectedDevice.Id;
                    return false;
                }

                if (DeviceChooserVM.SelectedDevice is OfflineDevice) {
                    await Rescan();
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );
                var newDevice = (IFlatDevice)device;
                connectFlatDeviceCts?.Dispose();
                connectFlatDeviceCts = new CancellationTokenSource();
                var token = connectFlatDeviceCts.Token;
                try {
                    var connected = await newDevice.Connect(connectFlatDeviceCts.Token);
                    if (connected) {
                        this.FlatDevice = newDevice;
                        token.ThrowIfCancellationRequested();
                        FlatDeviceInfo = new FlatDeviceInfo {
                            MinBrightness = newDevice.MinBrightness,
                            MaxBrightness = newDevice.MaxBrightness,
                            Brightness = newDevice.Brightness,
                            Connected = newDevice.Connected,
                            CoverState = newDevice.CoverState,
                            Description = newDevice.Description,
                            DriverInfo = newDevice.DriverInfo,
                            DriverVersion = newDevice.DriverVersion,
                            LightOn = newDevice.LightOn,
                            Name = newDevice.Name,
                            DisplayName = newDevice.DisplayName,
                            DeviceId = newDevice.Id,
                            SupportsOpenClose = newDevice.SupportsOpenClose,
                            SupportsOnOff = newDevice.SupportsOnOff,
                            SupportedActions = newDevice.SupportedActions,
                        };
                        this.Brightness = newDevice.Brightness;

                        Notification.ShowSuccess(Loc.Instance["LblFlatDeviceConnected"]);

                        if (updateTimer != null) {
                            updateTimer.Interval =
                                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            _ = updateTimer.Run();
                        }

                        profileService.ActiveProfile.FlatDeviceSettings.Id = newDevice.Id;

                        await (Connected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                        Logger.Info(
                            $"Successfully connected flat device. Id: {newDevice.Id} Name: {newDevice.Name} DisplayName: {newDevice.DisplayName} Driver Version: {newDevice.DriverVersion}");

                        return true;
                    } else {
                        FlatDeviceInfo.Connected = false;
                        this.FlatDevice = null;
                        return false;
                    }
                } catch (OperationCanceledException) {
                    if (flatDevice?.Connected == true) {
                        await Disconnect();
                    }

                    return false;
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                    Logger.Error(ex);
                    if (FlatDeviceInfo.Connected) { await Disconnect(); }
                    FlatDeviceInfo.Connected = false;
                    return false;
                }
            } finally {
                ssConnect.Release();
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        [RelayCommand]
        private void CancelConnect(object o) {
            try { connectFlatDeviceCts?.Cancel(); } catch { }
        }

        public async Task Disconnect() {
            try {
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }
                FlatDevice?.Disconnect();
                FlatDevice = null;
                FlatDeviceInfo.Reset();
                BroadcastFlatDeviceInfo();
                await (Disconnected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                Logger.Info("Disconnected Flat Device");
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private async Task<bool> DisconnectFlatDeviceDialog() {
            var dialog = MyMessageBox.Show(Loc.Instance["LblFlatDeviceDisconnectQuestion"],
                "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (dialog == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        private readonly SemaphoreSlim ssOpen = new SemaphoreSlim(1, 1);

        public async Task<bool> OpenCover(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await ssOpen.WaitAsync(token);
            try {
                if (FlatDevice.Connected == false) return false;
                if (!FlatDevice.SupportsOpenClose) return false;
                if (FlatDevice.CoverState == CoverState.Open) return true;
                Logger.Info("Opening Flat Device Cover");
                progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblOpeningCover"] });
                var result = await FlatDevice.Open(token);
                var waitForUpdate = updateTimer.WaitForNextUpdate(token);
                await CoreUtil.Delay(profileService.ActiveProfile.FlatDeviceSettings.SettleTime, token);
                await waitForUpdate;
                await (Opened?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                return result;
            } catch (FlatDeviceCoverErrorException) {
                Logger.Error("Flat device reports the cover is in an Error state");
                Notification.ShowError(string.Format(Loc.Instance["LblFlatDeviceCoverError"], FlatDevice.DisplayName));
                return false;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                ssOpen.Release();
                progress?.Report(new ApplicationStatus());
            }
        }

        private readonly SemaphoreSlim ssClose = new SemaphoreSlim(1, 1);

        public async Task<bool> CloseCover(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await ssClose.WaitAsync(token);
            try {
                if (FlatDevice.Connected == false) return false;
                if (!FlatDevice.SupportsOpenClose) return false;
                if (FlatDevice.CoverState == CoverState.Closed) return true;
                Logger.Info("Closing Flat Device Cover");
                progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblClosingCover"] });
                var result = await FlatDevice.Close(token);
                var waitForUpdate = updateTimer.WaitForNextUpdate(token);
                await CoreUtil.Delay(profileService.ActiveProfile.FlatDeviceSettings.SettleTime, token);
                await waitForUpdate;
                await (Closed?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                return result;
            } catch (FlatDeviceCoverErrorException) {
                Logger.Error("Flat device reports the cover is in an Error state");
                Notification.ShowError(string.Format(Loc.Instance["LblFlatDeviceCoverError"], FlatDevice.DisplayName));
                return false;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                ssClose.Release();
                progress?.Report(new ApplicationStatus());
            }
        }

        private IFlatDevice flatDevice;

        public IFlatDevice FlatDevice {
            get => flatDevice;
            private set {
                flatDevice = value;
                RaisePropertyChanged();
            }
        }

        private FlatDeviceInfo flatDeviceInfo;

        public FlatDeviceInfo FlatDeviceInfo {
            get {
                if (flatDeviceInfo != null) return flatDeviceInfo;
                flatDeviceInfo = DeviceInfo.CreateDefaultInstance<FlatDeviceInfo>();
                return flatDeviceInfo;
            }
            set {
                flatDeviceInfo = value;
                RaisePropertyChanged();
            }
        }

        public FlatDeviceInfo GetDeviceInfo() {
            return FlatDeviceInfo;
        }

        private void UpdateFlatDeviceValues(Dictionary<string, object> flatDeviceValues) {
            object o = null;
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Connected), out o);
            flatDeviceInfo.Connected = (bool)(o ?? false);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.CoverState), out o);
            flatDeviceInfo.CoverState = (CoverState)(o ?? CoverState.Unknown);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Brightness), out o);
            flatDeviceInfo.Brightness = (int)(o ?? 0);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.LightOn), out o);
            flatDeviceInfo.LightOn = (bool)(o ?? false);

            BroadcastFlatDeviceInfo();
        }

        private Dictionary<string, object> GetFlatDeviceValues() {
            var flatDeviceValues = new Dictionary<string, object>
            {
                {nameof(FlatDeviceInfo.Connected), FlatDevice?.Connected ?? false},
                {nameof(FlatDeviceInfo.CoverState), FlatDevice?.CoverState ?? CoverState.Unknown},
                {nameof(FlatDeviceInfo.Brightness), FlatDevice?.Brightness ?? 0},
                {nameof(FlatDeviceInfo.LightOn), FlatDevice?.LightOn ?? false},
            };
            return flatDeviceValues;
        }

        public Task<bool> ToggleLight(bool onOff, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (FlatDevice == null || FlatDevice.Connected == false) return Task.FromResult(false);
            return Task.Run(async () => {
                try {
                    if (FlatDevice.LightOn == onOff) {
                        return true;
                    }
                    Logger.Info($"Toggling light to {onOff}");
                    progress?.Report(new ApplicationStatus() { Status = string.Format(Loc.Instance["LblToggleLight"], onOff ? Loc.Instance["LblOn"] : Loc.Instance["LblOff"]) });
                    FlatDevice.LightOn = onOff;
                    var waitForUpdate = updateTimer.WaitForNextUpdate(token);
                    await CoreUtil.Delay(profileService.ActiveProfile.FlatDeviceSettings.SettleTime, token);
                    await waitForUpdate;
                    await (LightToggled?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                    return true;
                } finally {
                    progress?.Report(new ApplicationStatus());
                }
            }, token);
        }

        public string Action(string actionName, string actionParameters = "") {
            return FlatDeviceInfo?.Connected == true ? FlatDevice.Action(actionName, actionParameters) : null;
        }

        public string SendCommandString(string command, bool raw = true) {
            return FlatDeviceInfo?.Connected == true ? FlatDevice.SendCommandString(command, raw) : null;
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return FlatDeviceInfo?.Connected == true ? FlatDevice.SendCommandBool(command, raw) : false;
        }

        public void SendCommandBlind(string command, bool raw = true) {
            if (FlatDeviceInfo?.Connected == true) {
                FlatDevice.SendCommandBlind(command, raw);
            }
        }

        public IDevice GetDevice() {
            return FlatDevice;
        }

        public IAsyncCommand RescanDevicesCommand { get; }
        public IAsyncCommand ConnectCommand { get; }
        public IAsyncCommand DisconnectCommand { get; }
        public IAsyncCommand OpenCoverCommand { get; }
        public IAsyncCommand CloseCoverCommand { get; }
        public ICommand ToggleLightCommand { get; }
        public ICommand SetBrightnessCommand { get; }

        [ObservableProperty]
        private TrainedFlatExposureSetting selectedTrainedExposureSetting;

        [RelayCommand]
        public void AddTrainedSetting() {
            profileService.ActiveProfile.FlatDeviceSettings.AddEmptyTrainedExposureSetting();
        }

        [RelayCommand]
        public void RemoveTrainedSetting(TrainedFlatExposureSetting setting) {
            profileService.ActiveProfile.FlatDeviceSettings.RemoveFlatExposureSetting(setting);
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
        }

        private CameraInfo cameraInfo;

        public event Func<object, EventArgs, Task> Connected;
        public event Func<object, EventArgs, Task> Disconnected;

        public CameraInfo CameraInfo {
            get => cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            CameraInfo = deviceInfo;
        }
    }
}