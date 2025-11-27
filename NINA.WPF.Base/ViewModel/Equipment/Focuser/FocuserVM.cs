#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyFocuser;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Core.Locale;
using NINA.Core.Enum;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment;
using Nito.AsyncEx;
using System.Linq;
using NINA.Core.Utility.Extensions;
using Newtonsoft.Json.Linq;

namespace NINA.WPF.Base.ViewModel.Equipment.Focuser {

    public class FocuserVM : DockableVM, IFocuserVM {
        private readonly DeviceUpdateTimer updateTimer;
        private readonly IFocuserMediator focuserMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IProgress<ApplicationStatus> progress;
        private double lastFocusedTemperature = -1000;
        private double lastRoundoff = 0;

        public FocuserVM(IProfileService profileService,
                         IFocuserMediator focuserMediator,
                         IApplicationStatusMediator applicationStatusMediator,
                         IDeviceChooserVM focuserChooserVm,
                         IImageGeometryProvider imageGeometryProvider) : base(profileService) {
            Title = Loc.Instance["LblFocuser"];
            ImageGeometry = imageGeometryProvider.GetImageGeometry("FocusSVG");
            HasSettings = true;

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;
            DeviceChooserVM = focuserChooserVm;

            ConnectCommand = new AsyncCommand<bool>(() => Task.Run(ChooseFocuser), (object o) => DeviceChooserVM.SelectedDevice != null);
            CancelConnectCommand = new RelayCommand(CancelChooseFocuser);
            DisconnectCommand = new AsyncCommand<bool>(() => Task.Run(DisconnectDiag));
            RescanDevicesCommand = new AsyncCommand<bool>(async o => { await Rescan(); return true; }, o => !FocuserInfo.Connected);
            _ = RescanDevicesCommand.ExecuteAsync(null);
            MoveFocuserInSmallCommand = new AsyncCommand<int>(() => Task.Run(() => MoveFocuserRelativeInternal((int)Math.Min(-1, Math.Round(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize / -2d)))), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserInLargeCommand = new AsyncCommand<int>(() => Task.Run(() => MoveFocuserRelativeInternal(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize * -5)), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserOutSmallCommand = new AsyncCommand<int>(() => Task.Run(() => MoveFocuserRelativeInternal((int)Math.Max(1, Math.Round(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize / 2d)))), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserOutLargeCommand = new AsyncCommand<int>(() => Task.Run(() => MoveFocuserRelativeInternal(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize * 5)), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserCommand = new AsyncCommand<int>(() => Task.Run(() => MoveFocuserInternal(TargetPosition)), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            HaltFocuserCommand = new RelayCommand((object o) => { try { moveCts?.Cancel(); } catch { } });
            ToggleTempCompCommand = new RelayCommand(ToggleTempComp);

            updateTimer = new DeviceUpdateTimer(
                GetFocuserValues,
                UpdateFocuserValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval,
                "Focuser"
            );

            profileService.ProfileChanged += async (object sender, EventArgs e) => {
                await RescanDevicesCommand.ExecuteAsync(null);
            };

            progress = new Progress<ApplicationStatus>(p => {
                p.Source = this.Title;
                this.applicationStatusMediator.StatusUpdate(p);
            });
        }

        public async Task<IList<string>> Rescan() {
            return await Task.Run(async () => {
                await DeviceChooserVM.GetEquipment();
                return DeviceChooserVM.Devices.Select(x => x.Id).ToList();
            });
        }

        private void ToggleTempComp(object obj) {
            ToggleTempComp((bool)obj);
        }

        public void ToggleTempComp(bool tempComp) {
            if (FocuserInfo.Connected) {
                Focuser.TempComp = tempComp;
                FocuserInfo.TempComp = tempComp;
            }
        }

        private void HaltFocuser() {
            Logger.Info("Halting Focuser");
            if (FocuserInfo?.Connected != true) return;
            try {
                Focuser.Halt();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private CancellationTokenSource moveCts;

        private Task<int> MoveFocuserInternal(int position) {
            moveCts?.Dispose();
            moveCts = new CancellationTokenSource();
            var result = MoveFocuser(position, moveCts.Token);
            BroadcastUserFocused();
            return result;
        }

        private Task<int> MoveFocuserRelativeInternal(int position) {
            moveCts?.Dispose();
            moveCts = new CancellationTokenSource();
            var result = MoveFocuserRelative(position, moveCts.Token);
            BroadcastUserFocused();
            return result;
        }

        public void SetFocusedTemperature(double temp) {
            Logger.Debug($"Resetting last roundoff error");
            Interlocked.Exchange(ref lastRoundoff, 0.0);
            Logger.Debug($"Storing focused temperature - {temp} C");
            Interlocked.Exchange(ref lastFocusedTemperature, temp);
        }

        public async Task<int> MoveFocuserByTemperatureRelative(double temperature, double slope, CancellationToken ct) {
            await ss.WaitAsync(ct);
            try {
                double delta = 0;
                int deltaInt = 0;
                if (lastFocusedTemperature == -1000) {
                    delta = 0;
                    deltaInt = 0;
                    Logger.Info($"Moving Focuser By Temperature - Slope {slope} * ( DeltaT ) °C (relative mode) - lastTemperature initialized to {temperature}");
                } else {
                    delta = lastRoundoff + (temperature - lastFocusedTemperature) * slope;
                    deltaInt = (int)Math.Round(delta);
                    Logger.Info($"Moving Focuser By Temperature - LastRoundoff {lastRoundoff} + Slope {slope} * ( Temperature {temperature} - PrevTemperature {lastFocusedTemperature} ) °C (relative mode) = Delta {delta} / DeltaInt {deltaInt}");
                }
                int pos = Position;
                var result = await MoveFocuserInternal(pos + deltaInt, ct);
                lastFocusedTemperature = temperature;
                lastRoundoff = delta - deltaInt;
                return result;
            } finally {
                ss.Release();
            }
        }

        public async Task<int> MoveFocuser(int position, CancellationToken ct) {
            await ss.WaitAsync(ct);
            try {
                return await MoveFocuserInternal(position, ct);
            } finally {
                ss.Release();
            }
        }

        public async Task<int> MoveFocuserRelative(int offset, CancellationToken ct) {
            await ss.WaitAsync(ct);
            try {
                if (FocuserInfo?.Connected != true) return -1;
                var pos = Position + offset;
                pos = await MoveFocuserInternal(pos, ct);
                return pos;
            } finally {
                ss.Release();
            }
        }

        private async Task<int> MoveFocuserInternal(int position, CancellationToken ct) {
            var pos = -1;

            if (position < 0) {
                Logger.Warning($"Requested to move to a negative position {position}. Moving to 0 instead.");
                position = 0;
            }

            if (position > Focuser.MaxStep) {
                Logger.Warning($"Requested to move to position {position}, which higher than max position. Moving to {Focuser.MaxStep} instead.");
                position = Focuser.MaxStep;
            }

            await Task.Run(async () => {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                // Add a generous timeout of 10 minutes - just to prevent the procedure being stuck
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(10));

                var tempComp = false;
                if (Focuser.TempCompAvailable && Focuser.TempComp) {
                    tempComp = true;
                    ToggleTempComp(false);
                }
                try {
                    // Only register to call Halt() when no timeout happens
                    using (ct.Register(() => HaltFocuser())) {
                        Logger.Info($"Moving Focuser to position {position}");
                        progress.Report(new ApplicationStatus() { Status = string.Format(Loc.Instance["LblFocuserMoveToPosition"], position) });

                        while (Focuser.Position != position) {
                            FocuserInfo.IsMoving = true;
                            timeoutCts.Token.ThrowIfCancellationRequested();
                            await Focuser.Move(position, timeoutCts.Token);
                        }

                        FocuserInfo.Position = this.Position;
                        pos = this.Position;
                        var waitForUpdate = updateTimer.WaitForNextUpdate(timeoutCts.Token);
                        //Wait for focuser to settle
                        if (profileService.ActiveProfile.FocuserSettings.FocuserSettleTime > 0) {
                            FocuserInfo.IsSettling = true;
                            await CoreUtil.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.FocuserSettings.FocuserSettleTime), true, timeoutCts.Token, progress, Loc.Instance["LblSettle"]);
                        }
                        await waitForUpdate;
                        BroadcastFocuserInfo();
                    }
                } catch (OperationCanceledException) {
                    if (!ct.IsCancellationRequested) {
                        Logger.Error("Focuser movement timed out after 10 minutes");
                        Notification.ShowError(string.Format(Loc.Instance["LblFocuserMoveTimeout"], 10));
                    } else {
                        Logger.Info("Focuser move cancelled");
                        throw;
                    }
                } catch (Exception e) {
                    Logger.Error("Focuser move failed", e);
                    Notification.ShowError(Loc.Instance["LblMoveFocuserFailed"]);
                    throw;
                } finally {
                    if (tempComp) {
                        ToggleTempComp(tempComp);
                    }
                    FocuserInfo.IsSettling = false;
                    FocuserInfo.IsMoving = false;
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                }
            }, ct);
            return pos;
        }

        private CancellationTokenSource cancelChooseFocuserCts;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private static IFocuser GetBacklashCompensationFocuser(IProfileService profileService, IFocuser focuser) {
            switch (profileService.ActiveProfile.FocuserSettings.BacklashCompensationModel) {
                case BacklashCompensationModel.ABSOLUTE:
                    return new AbsoluteBacklashCompensationDecorator(profileService, focuser);

                case BacklashCompensationModel.OVERSHOOT:
                    return new OvershootBacklashCompensationDecorator(profileService, focuser);

                default:
                    return focuser;
            }
        }

        private async Task<bool> ChooseFocuser() {
            await ss.WaitAsync();
            try {
                await Disconnect();

                if (DeviceChooserVM.SelectedDevice == null) return false;

                if (DeviceChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.FocuserSettings.Id = DeviceChooserVM.SelectedDevice.Id;
                    profileService.ActiveProfile.FocuserSettings.LastDeviceName = string.Empty;
                    return false;
                }

                if (DeviceChooserVM.SelectedDevice is OfflineDevice) {
                    await Rescan();
                }

                progress.Report(new ApplicationStatus { Status = Loc.Instance["LblConnecting"] });

                var newFocuser = GetBacklashCompensationFocuser(profileService, (IFocuser)DeviceChooserVM.SelectedDevice);
                cancelChooseFocuserCts?.Dispose();
                cancelChooseFocuserCts = new CancellationTokenSource();
                var token = cancelChooseFocuserCts.Token;
                try {
                    var connected = await newFocuser.Connect(cancelChooseFocuserCts.Token);
                    if (connected) {
                        Focuser = newFocuser;
                        token.ThrowIfCancellationRequested();

                        FocuserInfo = new FocuserInfo {
                            Connected = true,
                            IsMoving = Focuser.IsMoving,
                            Name = Focuser.Name,
                            DisplayName = Focuser.DisplayName,
                            Position = Position,
                            StepSize = Focuser.StepSize,
                            TempCompAvailable = Focuser.TempCompAvailable,
                            TempComp = Focuser.TempComp,
                            Temperature = Focuser.Temperature,
                            SupportedActions = Focuser.SupportedActions,
                            Description = Focuser.Description,
                            DriverInfo = Focuser.DriverInfo,
                            DriverVersion = Focuser.DriverVersion,
                            DeviceId = Focuser.Id
                        };

                        Notification.ShowSuccess(Loc.Instance["LblFocuserConnected"]);

                        updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                        _ = updateTimer.Run();

                        TargetPosition = Position;
                        profileService.ActiveProfile.FocuserSettings.Id = Focuser.Id;
                        profileService.ActiveProfile.FocuserSettings.LastDeviceName = Focuser.DisplayName;

                        await (Connected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                        Logger.Info($"Successfully connected Focuser. Id: {Focuser.Id} Name: {Focuser.Name} DisplayName: {Focuser.DisplayName} Driver Version: {Focuser.DriverVersion}");

                        return true;
                    } else {
                        FocuserInfo.Connected = false;
                        Focuser = null;
                        return false;
                    }
                } catch (OperationCanceledException) {
                    if (newFocuser?.Connected == true) { await Disconnect(); }
                    return false;
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                    Logger.Error(ex);
                    if (FocuserInfo.Connected) { await Disconnect(); }
                    FocuserInfo.Connected = false;
                    return false;
                }
            } finally {
                ss.Release();
                progress.Report(new ApplicationStatus { Status = string.Empty });
            }
        }

        private void CancelChooseFocuser(object o) {
            try { cancelChooseFocuserCts?.Cancel(); } catch { }
        }

        private Dictionary<string, object> GetFocuserValues() {
            var focuserValues = new Dictionary<string, object>
            {
                {nameof(FocuserInfo.Connected), focuser?.Connected ?? false},
                {nameof(FocuserInfo.Position), Position},
                {nameof(FocuserInfo.Temperature), focuser?.Temperature ?? double.NaN},
                {nameof(FocuserInfo.IsMoving), focuser?.IsMoving ?? false},
                {nameof(FocuserInfo.TempComp), focuser?.TempComp ?? false}
            };
            return focuserValues;
        }

        private void UpdateFocuserValues(Dictionary<string, object> focuserValues) {
            focuserValues.TryGetValue(nameof(FocuserInfo.Connected), out var o);
            FocuserInfo.Connected = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(FocuserInfo.Position), out o);
            FocuserInfo.Position = (int)(o ?? 0);

            focuserValues.TryGetValue(nameof(FocuserInfo.Temperature), out o);
            FocuserInfo.Temperature = (double)(o ?? double.NaN);

            focuserValues.TryGetValue(nameof(FocuserInfo.IsMoving), out o);
            FocuserInfo.IsMoving = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(FocuserInfo.TempComp), out o);
            FocuserInfo.TempComp = (bool)(o ?? false);

            BroadcastFocuserInfo();
        }

        private FocuserInfo focuserInfo;

        public FocuserInfo FocuserInfo {
            get => focuserInfo ?? (focuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>());
            set {
                focuserInfo = value;
                RaisePropertyChanged();
            }
        }

        private void BroadcastFocuserInfo() {
            focuserMediator.Broadcast(FocuserInfo);
        }

        private void BroadcastUserFocused() {
            focuserMediator.BroadcastUserFocused(FocuserInfo);
        }

        private int targetPosition;

        public int TargetPosition {
            get => targetPosition;
            set {
                targetPosition = value;
                RaisePropertyChanged();
            }
        }

        public int Position => Focuser?.Position ?? 0;

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.Show(Loc.Instance["LblDisconnectFocuser"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public async Task Disconnect() {
            try {
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }
                Focuser?.Disconnect();
                Focuser = null;
                FocuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
                BroadcastFocuserInfo();
                RaisePropertyChanged(nameof(Focuser));
                await (Disconnected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                Logger.Info("Disconnected Focuser");
            } catch (Exception ex) {
                Logger.Error(ex);
            }

        }

        public Task<bool> Connect() {
            return ChooseFocuser();
        }

        public FocuserInfo GetDeviceInfo() {
            return FocuserInfo;
        }

        private IFocuser focuser;

        public event Func<object, EventArgs, Task> Connected;
        public event Func<object, EventArgs, Task> Disconnected;

        public IFocuser Focuser {
            get => focuser;
            private set {
                focuser = value;
                RaisePropertyChanged();
            }
        }

        public string Action(string actionName, string actionParameters = "") {
            return FocuserInfo?.Connected == true ? Focuser.Action(actionName, actionParameters) : null;
        }

        public string SendCommandString(string command, bool raw = true) {
            return FocuserInfo?.Connected == true ? Focuser.SendCommandString(command, raw) : null;
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return FocuserInfo?.Connected == true ? Focuser.SendCommandBool(command, raw) : false;
        }

        public void SendCommandBlind(string command, bool raw = true) {
            if (FocuserInfo?.Connected == true) {
                Focuser.SendCommandBlind(command, raw);
            }
        }
        public IDevice GetDevice() {
            if (Focuser is OvershootBacklashCompensationDecorator overshoot) {
                return overshoot.Focuser;
            } else if (Focuser is AbsoluteBacklashCompensationDecorator absolute) {
                return absolute.Focuser;
            }
            return Focuser;
        }

        public IDeviceChooserVM DeviceChooserVM { get; }
        public IAsyncCommand RescanDevicesCommand { get; }
        public IAsyncCommand ConnectCommand { get; }
        public ICommand CancelConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand MoveFocuserCommand { get; }
        public ICommand MoveFocuserInSmallCommand { get; }
        public ICommand MoveFocuserInLargeCommand { get; }
        public ICommand MoveFocuserOutSmallCommand { get; }
        public ICommand MoveFocuserOutLargeCommand { get; }
        public ICommand HaltFocuserCommand { get; }
        public ICommand ToggleTempCompCommand { get; }
    }
}