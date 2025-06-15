#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Astrometry;
using System.ComponentModel;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Core.Locale;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using Nito.AsyncEx;
using System.Linq;
using NINA.Core.Enum;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility.Extensions;

namespace NINA.WPF.Base.ViewModel.Equipment.Dome {

    public class DomeVM : DockableVM, IDomeVM, ITelescopeConsumer, ISafetyMonitorConsumer {

        public DomeVM(IProfileService profileService,
                      IDomeMediator domeMediator,
                      IApplicationStatusMediator applicationStatusMediator,
                      ITelescopeMediator telescopeMediator,
                      IDeviceChooserVM domeChooserVM,
                      IDomeFollower domeFollower,
                      ISafetyMonitorMediator safetyMonitorMediator,
                      IApplicationResourceDictionary resourceDictionary,
                      IDeviceUpdateTimerFactory deviceUpdateTimerFactory) : base(profileService) {
            Title = Loc.Instance["LblDome"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["ObservatorySVG"];

            this.domeMediator = domeMediator;
            this.domeMediator.RegisterHandler(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.applicationStatusMediator = applicationStatusMediator;
            this.safetyMonitorMediator = safetyMonitorMediator;
            this.safetyMonitorMediator.RegisterConsumer(this);
            DeviceChooserVM = domeChooserVM;
            this.domeFollower = domeFollower;
            this.domeFollower.PropertyChanged += DomeFollower_PropertyChanged;
            this.progress = new Progress<ApplicationStatus>(p => {
                p.Source = this.Title;
                this.applicationStatusMediator.StatusUpdate(p);
            });

            ConnectCommand = new AsyncCommand<bool>(() => Task.Run(ChooseDome), (object o) => DeviceChooserVM.SelectedDevice != null);
            CancelConnectCommand = new RelayCommand(CancelChooseDome);
            DisconnectCommand = new AsyncCommand<bool>(() => Task.Run(DisconnectDiag));
            RescanDevicesCommand = new AsyncCommand<bool>(async o => { await Rescan(); return true; }, o => !DomeInfo.Connected);
            StopCommand = new AsyncCommand<bool>((o) => Task.Run(() => StopAll(o)));
            OpenShutterCommand = new AsyncCommand<bool>(() => Task.Run(OpenShutterVM));
            CloseShutterCommand = new AsyncCommand<bool>(() => Task.Run(CloseShutterVM));
            SetParkPositionCommand = new RelayCommand(SetParkPosition);
            ParkCommand = new AsyncCommand<bool>(() => Task.Run(ParkVM));
            ManualSlewCommand = new AsyncCommand<bool>(() => Task.Run(() => ManualSlew(TargetAzimuthDegrees)));
            RotateCWCommand = new AsyncCommand<bool>(() => Task.Run(() => RotateRelative(RotateDegrees)));
            RotateCCWCommand = new AsyncCommand<bool>(() => Task.Run(() => RotateRelative(-RotateDegrees)));
            FindHomeCommand = new AsyncCommand<bool>((o) => Task.Run(() => FindHome(o, CancellationToken.None)));
            SyncCommand = new RelayCommand(SyncAzimuth);
            _ = RescanDevicesCommand.ExecuteAsync(null);

            this.updateTimer = deviceUpdateTimerFactory.Create(
                GetDomeValues,
                UpdateDomeValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval,
                "Dome"
            );

            profileService.ProfileChanged += async (object sender, EventArgs e) => {
                await RescanDevicesCommand.ExecuteAsync(null);
            };
        }

        public event EventHandler<EventArgs> Synced;
        public event Func<object, EventArgs, Task> Opened;
        public event Func<object, EventArgs, Task> Closed;
        public event Func<object, EventArgs, Task> Parked;
        public event Func<object, EventArgs, Task> Homed;
        public event Func<object, DomeEventArgs, Task> Slewed;

        public async Task<IList<string>> Rescan() {
            return await Task.Run(async () => {
                await DeviceChooserVM.GetEquipment();
                return DeviceChooserVM.Devices.Select(x => x.Id).ToList();
            });
        }

        private void DomeFollower_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(IDomeFollower.IsFollowing)) {
                if (!this.domeFollower.IsFollowing) {
                    this.FollowEnabled = false;
                }
            }
        }

        private CancellationTokenSource cancelChooseDomeSource;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseDome() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (DeviceChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.DomeSettings.Id = DeviceChooserVM.SelectedDevice.Id;
                    profileService.ActiveProfile.DomeSettings.LastDeviceName = string.Empty;
                    return false;
                }

                if (DeviceChooserVM.SelectedDevice is OfflineDevice) {
                    await Rescan();
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );

                var dome = (IDome)DeviceChooserVM.SelectedDevice;
                cancelChooseDomeSource?.Dispose();
                cancelChooseDomeSource = new CancellationTokenSource();
                var token = cancelChooseDomeSource.Token;
                if (dome != null) {
                    try {
                        var connected = await dome?.Connect(cancelChooseDomeSource.Token);
                        if (connected) {
                            Dome = dome;
                            token.ThrowIfCancellationRequested();

                            DomeInfo = new DomeInfo {
                                Connected = true,
                                Name = Dome.Name,
                                DisplayName = Dome.DisplayName,
                                DeviceId = Dome.Id,
                                Description = Dome.Description,
                                DriverInfo = Dome.DriverInfo,
                                DriverVersion = Dome.DriverVersion,
                                ShutterStatus = Dome.ShutterStatus,
                                DriverCanFollow = Dome.DriverCanFollow,
                                CanSetShutter = Dome.CanSetShutter,
                                CanSetPark = Dome.CanSetPark,
                                CanSetAzimuth = Dome.CanSetAzimuth,
                                CanSyncAzimuth = Dome.CanSyncAzimuth,
                                CanPark = Dome.CanPark,
                                CanFindHome = Dome.CanFindHome,
                                AtPark = Dome.AtPark,
                                AtHome = Dome.AtPark,
                                DriverFollowing = Dome.DriverFollowing,
                                Slewing = Dome.Slewing,
                                Azimuth = Dome.Azimuth,
                                SupportedActions = Dome.SupportedActions,
                            };

                            RaiseAllPropertiesChanged();
                            BroadcastDomeInfo();

                            Notification.ShowSuccess(Loc.Instance["LblDomeConnected"]);

                            _ = updateTimer.Run();

                            profileService.ActiveProfile.DomeSettings.Id = Dome.Id;
                            profileService.ActiveProfile.DomeSettings.LastDeviceName = Dome.DisplayName;

                            await (Connected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                            Logger.Info($"Successfully connected Dome. Id: {Dome.Id} Name: {Dome.Name} DisplayName: {Dome.DisplayName} Driver Version: {Dome.DriverVersion}");

                            return true;
                        } else {
                            DomeInfo.Connected = false;
                            Dome = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (dome?.Connected == true) { await Disconnect(); }
                        return false;
                    }  catch (Exception ex) {
                        Notification.ShowError(ex.Message);
                        Logger.Error(ex);
                        if (DomeInfo.Connected) { await Disconnect(); }
                        DomeInfo.Connected = false;
                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private void CancelChooseDome(object o) {
            try { cancelChooseDomeSource?.Cancel(); } catch { }
        }

        private Dictionary<string, object> GetDomeValues() {
            Dictionary<string, object> domeValues = new Dictionary<string, object> {
                { nameof(DomeInfo.Connected), Dome?.Connected ?? false },
                { nameof(DomeInfo.ShutterStatus), Dome?.ShutterStatus ?? ShutterState.ShutterError },
                { nameof(DomeInfo.DriverCanFollow), Dome?.DriverCanFollow ?? false },
                { nameof(DomeInfo.CanSetShutter), Dome?.CanSetShutter ?? false },
                { nameof(DomeInfo.CanSetPark), Dome?.CanSetPark ?? false },
                { nameof(DomeInfo.CanSetAzimuth), Dome?.CanSetAzimuth ?? false },
                { nameof(DomeInfo.CanSyncAzimuth), Dome?.CanSyncAzimuth ?? false },
                { nameof(DomeInfo.CanPark), Dome?.CanPark ?? false },
                { nameof(DomeInfo.CanFindHome), Dome?.CanFindHome ?? false },
                { nameof(DomeInfo.AtPark), Dome?.AtPark ?? false },
                { nameof(DomeInfo.AtHome), Dome?.AtHome ?? false },
                { nameof(DomeInfo.DriverFollowing), Dome?.DriverFollowing ?? false },
                { nameof(DomeInfo.Slewing), Dome?.Slewing ?? false },
                { nameof(DomeInfo.Azimuth), Dome?.Azimuth ?? double.NaN },
                { nameof(DomeInfo.Altitude), Dome?.Altitude ?? double.NaN }
            };

            return domeValues;
        }

        private void UpdateDomeValues(Dictionary<string, object> domeValues) {
            object o;

            domeValues.TryGetValue(nameof(DomeInfo.Connected), out o);
            DomeInfo.Connected = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.ShutterStatus), out o);
            DomeInfo.ShutterStatus = (ShutterState)(o ?? ShutterState.ShutterError);

            domeValues.TryGetValue(nameof(DomeInfo.DriverCanFollow), out o);
            DomeInfo.DriverCanFollow = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanSetShutter), out o);
            DomeInfo.CanSetShutter = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanSetPark), out o);
            DomeInfo.CanSetPark = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanSetAzimuth), out o);
            DomeInfo.CanSetAzimuth = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanSyncAzimuth), out o);
            DomeInfo.CanSyncAzimuth = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanPark), out o);
            DomeInfo.CanPark = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanFindHome), out o);
            DomeInfo.CanFindHome = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.AtPark), out o);
            DomeInfo.AtPark = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.AtHome), out o);
            DomeInfo.AtHome = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.DriverFollowing), out o);
            DomeInfo.DriverFollowing = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.Slewing), out o);
            DomeInfo.Slewing = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.Azimuth), out o);
            DomeInfo.Azimuth = (double)(o ?? double.NaN);

            domeValues.TryGetValue(nameof(DomeInfo.Altitude), out o);
            DomeInfo.Altitude = (double)(o ?? double.NaN);

            DomeInfo.ApplicationFollowing = domeFollower.IsFollowing;

            BroadcastDomeInfo();
        }

        private DomeInfo domeInfo;

        public DomeInfo DomeInfo {
            get {
                if (domeInfo == null) {
                    domeInfo = DeviceInfo.CreateDefaultInstance<DomeInfo>();
                }
                return domeInfo;
            }
            set {
                domeInfo = value;
                RaisePropertyChanged();
            }
        }

        public DomeInfo GetDeviceInfo() {
            return DomeInfo;
        }

        private void BroadcastDomeInfo() {
            domeMediator.Broadcast(DomeInfo);
        }

        public Task<bool> Connect() {
            return ChooseDome();
        }

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.Show(Loc.Instance["LblDomeDisconnect"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
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
                Dome?.Disconnect();
                Dome = null;
                DomeInfo.Reset();
                BroadcastDomeInfo();
                RaiseAllPropertiesChanged();
                await (Disconnected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                Logger.Info("Disconnected Dome Device");
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private IDome dome;

        public IDome Dome {
            get => dome;
            private set {
                dome = value;
                RaisePropertyChanged();
            }
        }

        public IDeviceChooserVM DeviceChooserVM { get; private set; }

        private Task<bool> OpenShutterVM() {
            return OpenShutter(CancellationToken.None);
        }

        public async Task<bool> OpenShutter(CancellationToken cancellationToken) {
            if (DomeInfo.Connected) {
                if (Dome.CanSetShutter) {

                    // 0. Check if the shutter/roof is moving toward the open state, and wait.
                    if (DomeInfo.ShutterStatus == ShutterState.ShutterOpening) {
                        Logger.Info("Dome shutter already opening - waiting for it to complete.");
                        while (DomeInfo.ShutterStatus == ShutterState.ShutterOpening && !cancellationToken.IsCancellationRequested) {
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        }
                        Logger.Debug($"Dome shutter no longer opening, shutter state is now {DomeInfo.ShutterStatus}");

                        // Since the shutter was in motion before calling this function, do not continue to send another movement command should movement stop
                        // but the shutter not be fully Open.  It could be under external control and stopped for a reason.
                        if (DomeInfo.ShutterStatus == ShutterState.ShutterOpen) {
                            return true;
                        } else {
                            Logger.Error($"Dome shutter stopped opening, but is not open.  Shutter state is now {DomeInfo.ShutterStatus}");
                            return false;
                        }
                    }

                    // 1. Check if the shutter/roof is already open
                    if (DomeInfo.ShutterStatus == ShutterState.ShutterOpen) {
                        return true;
                    }

                    // 2. Refuse to open if the safety monitor device signals an unsafe condition.
                    // Optionally, a disconnected safety monitor can be a failure of this test.
                    if (SafetyMonitorInfo.Connected) {
                        if (!SafetyMonitorInfo.IsSafe) {
                            Logger.Error("Cannot open dome shutter due to unsafe conditions");
                            Notification.ShowError(Loc.Instance["LblDomeRefuseOpeningWhenUnsafeWarning"]);
                            return false;
                        }
                    } else {
                        if (profileService.ActiveProfile.DomeSettings.RefuseUnsafeShutterOpenSansSafetyDevice) {
                            Logger.Error("Dome shutter ordered to open but the safety monitor device is disconnected! Not opening as this might be dangerous! (RefuseUnsafeShutterOpenSansSafetyDevice=true)");
                            Notification.ShowError(Loc.Instance["LblSafetyDeviceDisconnectedOnShutterMoveError"]);
                            return false;
                        }
                    }

                    // 3. Refuse to open the shutter/roof if RefuseUnsafeShutterMove is enabled and the mount is not parked. A disconnected mount is considered a failure of this test because mount state cannot be determined.
                    // We do not proactively park the mount because doing so might drive the OTA into a closed roof. The user will need to sort out this situation.
                    if (profileService.ActiveProfile.DomeSettings.RefuseUnsafeShutterMove) {
                        if (TelescopeInfo.Connected) {
                            if (!TelescopeInfo.AtPark) {
                                Logger.Error("Dome shutter ordered to open but the mount is unparked! Not opening as this might be dangerous! (RefuseUnsafeShutterMove=true)");
                                Notification.ShowError(Loc.Instance["LblMountUnparkedOnShutterMoveError"]);
                                return false;
                            }
                        } else {
                            Logger.Error("Dome shutter ordered to open but the mount is disconnected! Not opening as this might be dangerous! (RefuseUnsafeShutterMove=true)");
                            Notification.ShowError(Loc.Instance["LblMountDisconnectedOnShutterMoveError"]);
                            return false;
                        }
                    }

                    // 4. Park the DOME if ParkDomeBeforeShutterMove is enabled.
                    // Some domes require this to align power contacts to operate the shutter. It should be the last thing to do before moving the shutter/roof
                    if (profileService.ActiveProfile.DomeSettings.ParkDomeBeforeShutterMove) {
                        if (!DomeInfo.AtPark) {
                            Logger.Info("Dome shutter ordered to open. Disabling following and parking dome first");
                            FollowEnabled = false;
                            await Park(cancellationToken);
                        }
                    }

                    // 5. Open the shutter/roof
                    try {
                        Logger.Info($"Opening dome shutter. Shutter state after opening {DomeInfo.ShutterStatus}");
                        progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblDomeShutterOpen"] });
                        await Dome.OpenShutter(cancellationToken); 
                        var waitForUpdate = updateTimer.WaitForNextUpdate(cancellationToken);
                        await CoreUtil.Wait(TimeSpan.FromSeconds(this.profileService.ActiveProfile.DomeSettings.SettleTimeSeconds), true, cancellationToken, progress, Loc.Instance["LblSettle"]); 
                        await waitForUpdate;
                        Logger.Info($"Opened dome shutter. Shutter state after opening {DomeInfo.ShutterStatus}");
                        await (Opened?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                        return true;
                    } finally {
                        progress.Report(new ApplicationStatus() { Status = string.Empty });
                    }
                } else {
                    Logger.Warning("Cannot open shutter. Dome does not support it.");
                    return false;
                }
            } else {
                Logger.Error("Dome shutter ordered to open but the dome is disconnected!");
                Notification.ShowError(Loc.Instance["LblDomeNotConnectedError"]);
                return false;
            }
        }

        private Task<bool> CloseShutterVM() {
            return CloseShutter(CancellationToken.None);
        }

        public async Task<bool> CloseShutter(CancellationToken cancellationToken) {
            if (DomeInfo.Connected) {
                if (Dome.CanSetShutter) {

                    // 0. Check if the shutter/roof is moving toward the closed state, and wait.
                    if (DomeInfo.ShutterStatus == ShutterState.ShutterClosing) {
                        Logger.Info("Dome shutter already closing - waiting for it to complete.");
                        while (DomeInfo.ShutterStatus == ShutterState.ShutterClosing && !cancellationToken.IsCancellationRequested) {
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        }
                        Logger.Debug($"Dome shutter no longer closing, shutter state is now {DomeInfo.ShutterStatus}");

                        // Since the shutter was in motion before calling this function, do not continue to send another movement command should movement stop
                        // but the shutter not be fully Closed.  It could be under external control and stopped for a reason.
                        if (DomeInfo.ShutterStatus == ShutterState.ShutterClosed) {
                            return true;
                        } else {
                            Logger.Error($"Dome shutter stopped closing, but is not closed.  Shutter state is now {DomeInfo.ShutterStatus}");
                            return false;
                        }
                    }
                    // 1. Check if the shutter/roof is already closed
                    if (DomeInfo.ShutterStatus == ShutterState.ShutterClosed) {
                        return true;
                    }

                    // 2. Park the mount if ParkMountBeforeShutterMove is enabled. A disconnected mount is considered a failure here.
                    if (profileService.ActiveProfile.DomeSettings.ParkMountBeforeShutterMove) {
                        if (TelescopeInfo.Connected) {
                            if (!TelescopeInfo.AtPark) {
                                Logger.Info($"Dome shutter ordered to close. Disabling following and parking mount first. (ParkMountBeforeShutterMove=true)");
                                FollowEnabled = false;
                                await telescopeMediator.ParkTelescope(progress, cancellationToken);
                            }
                        } else {
                            Logger.Error("Dome shutter ordered to close but the mount is disconnected! Not close as this might be dangerous! (ParkMountBeforeShutterMove=true)");
                            Notification.ShowError(Loc.Instance["LblMountDisconnectedOnShutterMoveError"]);
                            return false;
                        }
                    }

                    // 3. If RefuseUnsafeShutterMove is enabled, refuse to close the shutter/roof if the mount is not parked A disconnected mount is considered a failure of this test because mount state cannot be determined.
                    // This is a fail-safe for any cases where we reach this point and the mount is not, or not yet, parked. This can be a driver bug or something else causing the mount to unpark.
                    if (profileService.ActiveProfile.DomeSettings.RefuseUnsafeShutterMove) {
                        if (TelescopeInfo.Connected) {
                            if (!TelescopeInfo.AtPark) {
                                Logger.Error("Dome shutter ordered to close but the mount is unparked! Not closing it as this might be dangerous! (RefuseUnsafeShutterMove=true)");
                                Notification.ShowError(Loc.Instance["LblMountUnparkedOnShutterMoveError"]);
                                return false;
                            }
                        } else {
                            Logger.Error("Dome shutter ordered to close but the mount is disconnected! Not close as this might be dangerous! (RefuseUnsafeShutterMove=true)");
                            Notification.ShowError(Loc.Instance["LblMountDisconnectedOnShutterMoveError"]);
                            return false;
                        }
                    }

                    // 4. Park the DOME if ParkDomeBeforeShutterMove is enabled.
                    // Some domes require this to align power contacts to operate the shutter. It should be the last thing to do before moving the shutter/roof
                    if (profileService.ActiveProfile.DomeSettings.ParkDomeBeforeShutterMove) {
                        if (!DomeInfo.AtPark) {
                            Logger.Info("Dome shutter ordered to close. Disabling following and parking dome first");
                            FollowEnabled = false;
                            await Park(cancellationToken);
                        }
                    }

                    // 5. Close the shutter/roof
                    try {
                        Logger.Info($"Closing dome shutter. Shutter state before closing {DomeInfo.ShutterStatus}");
                        progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblDomeShutterClose"] });
                        await Dome.CloseShutter(cancellationToken);
                        var waitForUpdate = updateTimer.WaitForNextUpdate(cancellationToken);
                        await CoreUtil.Wait(TimeSpan.FromSeconds(this.profileService.ActiveProfile.DomeSettings.SettleTimeSeconds), true, cancellationToken, progress, Loc.Instance["LblSettle"]);
                        await waitForUpdate;
                        Logger.Info($"Closed dome shutter. Shutter state after closing {DomeInfo.ShutterStatus}");
                        await (Closed?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                        return true;
                    } finally {
                        progress.Report(new ApplicationStatus() { Status = string.Empty });
                    }
                } else {
                    Logger.Warning("Cannot close shutter. Dome does not support it.");
                    return false;
                }
            } else {
                Logger.Error("Dome shutter ordered to close but the dome is disconnected!");
                Notification.ShowError(Loc.Instance["LblDomeNotConnectedError"]);
                return false;
            }
        }

        private Task<bool> ParkVM() {
            return Park(CancellationToken.None);
        }

        public async Task<bool> Park(CancellationToken cancellationToken) {
            if (Dome.CanPark) {
                Logger.Info("Parking dome");
                await DisableFollowing(cancellationToken);
                if (profileService.ActiveProfile.DomeSettings.FindHomeBeforePark && Dome.CanFindHome) {
                    Logger.Info("Finding home before parking");
                    await Dome.FindHome(cancellationToken);
                }
                await Dome.Park(cancellationToken);
                await updateTimer.WaitForNextUpdate(cancellationToken);
                Logger.Info("Park complete");
                await (Parked?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                return true;
            } else {
                Logger.Error("Cannot park shutter. Dome does not support it.");
                return false;
            }
        }

        public async Task WaitForDomeSynchronization(CancellationToken cancellationToken) {
            await this.domeFollower.WaitForDomeSynchronization(cancellationToken);
        }

        private async Task<bool> StopAll(object p) {
            Logger.Info("Stopping all dome movement");
            try {
                await this.domeFollower.Stop();
            } catch (Exception ex) {
                Logger.Error("Stopping dome follower failed", ex);
            }
            try {
                await Dome?.StopAll();
            } catch (Exception ex) {
                Logger.Error("Stopping all Dome actions failed", ex);
            }
            FollowEnabled = false;
            return true;
        }

        private void SetParkPosition(object p) {
            Dome?.SetPark();
        }

        private double targetAzimuthDegrees;

        public double TargetAzimuthDegrees {
            get => targetAzimuthDegrees;

            set {
                targetAzimuthDegrees = value;
                RaisePropertyChanged();
            }
        }

        public double RotateDegrees {
            get => profileService.ActiveProfile.DomeSettings.RotateDegrees;

            set {
                if (profileService.ActiveProfile.DomeSettings.RotateDegrees != value) {
                    profileService.ActiveProfile.DomeSettings.RotateDegrees = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool CanSyncAzimuth {
            get {
                if (Dome?.Connected != true || TelescopeInfo?.Connected != true) {
                    return false;
                }
                return Dome.CanSyncAzimuth;
            }
        }

        public async Task<bool> SlewToAzimuth(double degrees, CancellationToken token) {
            if (Dome?.Connected == true) {
                try {
                    var from = DomeInfo.Azimuth;
                    Logger.Info($"Slewing dome to azimuth {degrees}°");
                    progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSlew"] });
                    await Dome?.SlewToAzimuth(degrees, token);
                    var waitForUpdate = updateTimer.WaitForNextUpdate(token);
                    await CoreUtil.Wait(TimeSpan.FromSeconds(this.profileService.ActiveProfile.DomeSettings.SettleTimeSeconds), true, token, progress, Loc.Instance["LblSettle"]);
                    await waitForUpdate;
                    await (Slewed?.InvokeAsync(this, new DomeEventArgs(from: from, to: degrees)) ?? Task.CompletedTask);
                    return true;
                } finally {
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                }
            }
            return false;
        }

        private async Task<bool> ManualSlew(double degrees) {
            if (Dome.CanSetAzimuth) {
                this.FollowEnabled = false;
                Logger.Info($"Manually slewing dome to azimuth {degrees}°");
                return await SlewToAzimuth(degrees, CancellationToken.None);
            } else {
                return false;
            }
        }

        private async Task<bool> RotateRelative(double degrees) {
            if (Dome.CanSetAzimuth) {
                this.FollowEnabled = false;
                var targetAzimuth = AstroUtil.EuclidianModulus(this.Dome.Azimuth + degrees, 360.0);
                Logger.Info($"Rotating dome relatively by {degrees}°");
                return await SlewToAzimuth(targetAzimuth, CancellationToken.None);
            } else {
                return false;
            }
        }

        public Task<bool> FindHome(CancellationToken ct) {
            return FindHome(null, ct);
        }

        private async Task<bool> FindHome(object obj, CancellationToken ct) {
            Logger.Info("Finding dome home position");
            await DisableFollowing(ct);
            ct.ThrowIfCancellationRequested();

            await Dome?.FindHome(ct);
            await updateTimer.WaitForNextUpdate(ct);
            ct.ThrowIfCancellationRequested();

            Logger.Info("Dome home find complete");
            await (Homed?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
            return true;
        }

        private void SyncAzimuth(object obj) {
            if (CanSyncAzimuth) {
                var calculatedTargetCoordinates = this.domeFollower.GetSynchronizedDomeCoordinates(TelescopeInfo);
                if(calculatedTargetCoordinates != null) { 
                    Dome.SyncToAzimuth(calculatedTargetCoordinates.Azimuth.Degree);
                    try { Synced?.Invoke(this, new EventArgs()); } catch(Exception ex) {  Logger.Error(ex); }
                }
            }
        }

        private bool followEnabled;

        public bool FollowEnabled {
            get {
                if (Dome?.Connected == true) {
                    return followEnabled;
                } else {
                    return false;
                }
            }
            set {
                if (followEnabled != value) {
                    followEnabled = value;
                    OnFollowChanged(followEnabled);
                    RaisePropertyChanged();
                }
            }
        }

        private void OnFollowChanged(bool followEnabled) {
            if (followEnabled && Dome?.Connected == true) {
                this.domeFollower.Start();
                Logger.Info($"Dome following enabled");
            } else {
                this.domeFollower.Stop();
                Logger.Info($"Dome following stopped");
            }
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            TelescopeInfo = deviceInfo;
        }

        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();

        public TelescopeInfo TelescopeInfo {
            get => telescopeInfo;
            private set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        private SafetyMonitorInfo safetyMonitorInfo = DeviceInfo.CreateDefaultInstance<SafetyMonitorInfo>();

        public SafetyMonitorInfo SafetyMonitorInfo {
            get => safetyMonitorInfo;
            private set {
                safetyMonitorInfo = value;
                RaisePropertyChanged();
            }
        }

        public void Dispose() {
            this.telescopeMediator?.RemoveConsumer(this);
            this.telescopeMediator = null;
            this.safetyMonitorMediator?.RemoveConsumer(this);
            this.safetyMonitorMediator = null;
        }

        public async Task<bool> EnableFollowing(CancellationToken cancellationToken) {
            if (!Dome.Connected) {
                return false;
            }

            FollowEnabled = true;
            while (Dome.Slewing && !cancellationToken.IsCancellationRequested) {
                await Task.Delay(1000, cancellationToken);
            }
            await updateTimer.WaitForNextUpdate(cancellationToken);
            return FollowEnabled;
        }

        public async Task<bool> DisableFollowing(CancellationToken cancellationToken) {
            if (!Dome.Connected) {
                return false;
            }

            FollowEnabled = false;
            await updateTimer.WaitForNextUpdate(cancellationToken);
            return true;
        }

        private object lockObj = new object();
        private Task closeShutterTask;

        public void UpdateDeviceInfo(SafetyMonitorInfo deviceInfo) {
            SafetyMonitorInfo = deviceInfo;
            if (Dome?.Connected == true && profileService.ActiveProfile.DomeSettings.CloseOnUnsafe) {
                //Close dome when state switches from safe to unsafe
                if (deviceInfo.Connected && !deviceInfo.IsSafe && Dome?.ShutterStatus == ShutterState.ShutterOpen) {
                    lock (lockObj) {
                        if (closeShutterTask == null || closeShutterTask.IsCompleted) {
                            closeShutterTask = Task.Run(async () => {
                                Logger.Warning("Closing dome shutter due to unsafe conditions");
                                Notification.ShowWarning(Loc.Instance["LblDomeCloseOnUnsafeWarning"]);
                                await CloseShutter(CancellationToken.None);
                            });
                        }
                    }
                }
            }
        }

        public async Task<bool> SyncToScopeCoordinates(Coordinates coordinates, PierSide sideOfPier, CancellationToken cancellationToken) {
            return await this.domeFollower.SyncToScopeCoordinates(coordinates, sideOfPier, cancellationToken);
        }

        public string Action(string actionName, string actionParameters) {
            if (Dome?.Connected == true) {
                return Dome.Action(actionName, actionParameters);
            } else {
                Notification.ShowError(Loc.Instance["LblTelescopeNotConnectedForCommand"] + ": " + actionName);
                return null;
            }
        }

        public string SendCommandString(string command, bool raw = true) {
            return Dome?.Connected == true ? Dome.SendCommandString(command, raw) : null;
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return Dome?.Connected == true ? Dome.SendCommandBool(command, raw) : false;
        }

        public void SendCommandBlind(string command, bool raw = true) {
            if (Dome?.Connected == true) {
                Dome.SendCommandBlind(command, raw);
            }
        }
        public IDevice GetDevice() {
            return Dome;
        }

        private readonly IDeviceUpdateTimer updateTimer;
        private readonly IDomeMediator domeMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IDomeFollower domeFollower;
        private readonly IProgress<ApplicationStatus> progress;
        private ITelescopeMediator telescopeMediator;
        private ISafetyMonitorMediator safetyMonitorMediator;

        public event Func<object, EventArgs, Task> Connected;
        public event Func<object, EventArgs, Task> Disconnected;

        public IAsyncCommand ConnectCommand { get; private set; }
        public IAsyncCommand RescanDevicesCommand { get; private set; }
        public ICommand CancelConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand OpenShutterCommand { get; private set; }
        public ICommand CloseShutterCommand { get; private set; }
        public ICommand ParkCommand { get; private set; }
        public ICommand SetParkPositionCommand { get; private set; }
        public ICommand ManualSlewCommand { get; private set; }
        public ICommand FindHomeCommand { get; private set; }
        public ICommand RotateCWCommand { get; private set; }
        public ICommand RotateCCWCommand { get; private set; }
        public ICommand SyncCommand { get; private set; }
    }
}