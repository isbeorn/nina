#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MySwitch;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using NINA.Core.Model;
using NINA.Core.Locale;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Core.Utility.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.WPF.Base.ViewModel.Equipment.Switch {

    public partial class SwitchVM : DockableVM, ISwitchVM {

        public SwitchVM(IProfileService profileService,
                        IApplicationStatusMediator applicationStatusMediator,
                        ISwitchMediator switchMediator,
                        IDeviceChooserVM deviceChooserVM) : base(profileService) {
            Title = Loc.Instance["LblSwitch"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SwitchesSVG"];
            HasSettings = true;
            DeviceChooserVM = deviceChooserVM;
            writableSwitches = new AsyncObservableCollection<IWritableSwitch>();
            readonlySwitches = new AsyncObservableCollection<ISwitch>();
            switchInfo = DeviceInfo.CreateDefaultInstance<SwitchInfo>();

            this.applicationStatusMediator = applicationStatusMediator;
            this.switchMediator = switchMediator;
            this.switchMediator.RegisterHandler(this);

            ConnectCommand = new AsyncCommand<bool>(() => Task.Run(Connect), (object o) => DeviceChooserVM.SelectedDevice != null);
            DisconnectCommand = new AsyncCommand<bool>(async () => { await Task.Run(Disconnect); return true; });
            CancelConnectCommand = new RelayCommand((object o) => CancelConnect());
            RescanDevicesCommand = new AsyncCommand<bool>(async o => { await Rescan(); return true; }, o => !SwitchInfo.Connected);
            _ = RescanDevicesCommand.ExecuteAsync(null);

            updateTimer = new DeviceUpdateTimer(
                 GetSwitchValues,
                 UpdateSwitchValues,
                 profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval,
                 "Switch"
             );

            SetSwitchValueCommand = new AsyncCommand<bool>((p) => Task.Run(() => SetSwitchValue(p)));
            ToggleBooleanSwitchValueCommand = new AsyncCommand<bool>(async o => {
                if (o is IWritableSwitch ws) {
                    ws.TargetValue = ws.Value == 0 ? 1 : 0;
                    await SetSwitchValueCommand.ExecuteAsync(o);
                }
                return true;
            });

            profileService.ProfileChanged += async (object sender, EventArgs e) => {
                await RescanDevicesCommand.ExecuteAsync(null);
            };
        }

        public async Task<IList<string>> Rescan() {
            return await Task.Run(async () => {
                await DeviceChooserVM.GetEquipment();
                return DeviceChooserVM.Devices.Select(x => x.Id).ToList();
            });
        }

        private static double SWITCHTOLERANCE = 0.00001;

        private async Task<bool> SetSwitchValue(object arg) {
            await SetSwitchValue((IWritableSwitch)arg, CancellationToken.None);
            return true;
        }

        private void EnsureLiveSwitch(ISwitchHub hub, IWritableSwitch target) {
            if (hub == null || !ReferenceEquals(SwitchHub, hub) || !hub.Connected) {
                throw new InvalidOperationException(Loc.Instance["LblSwitchNotConnected"]);
            }

            if (target == null || !WritableSwitches.Contains(target)) {
                throw new InvalidOperationException(Loc.Instance["LblHubSwitchTargetInvalid"]);
            }
        }

        private async Task SetSwitchValue(IWritableSwitch target, CancellationToken token) {
            var hub = SwitchHub;
            token.ThrowIfCancellationRequested();
            EnsureLiveSwitch(hub, target);
            var requestedValue = target.TargetValue;
            target.SetValue();
            await CoreUtil.Wait(TimeSpan.FromMilliseconds(50), token);
            EnsureLiveSwitch(hub, target);
            if (!target.Poll()) {
                throw new InvalidOperationException(Loc.Instance["LblHubSwitchPollFailed"]);
            }

            EnsureLiveSwitch(hub, target);
            var timeOut = TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval * 4);
            while (Math.Abs(target.Value - requestedValue) > SWITCHTOLERANCE) {
                var elapsed = await CoreUtil.Wait(TimeSpan.FromMilliseconds(500), token);
                EnsureLiveSwitch(hub, target);
                if (!target.Poll()) {
                    throw new InvalidOperationException(Loc.Instance["LblHubSwitchPollFailed"]);
                }

                EnsureLiveSwitch(hub, target);
                timeOut -= elapsed;
                if (timeOut.TotalMilliseconds <= 0 && Math.Abs(target.Value - requestedValue) > SWITCHTOLERANCE) {
                    var notification = string.Format(Loc.Instance["LblTimeoutToSetSwitchValue"], requestedValue, target.Id, target.Name, target.Value);
                    Notification.ShowError(notification);
                    Logger.Error(notification);
                    throw new TimeoutException(notification);
                }
            }
        }

        public async Task SetSwitchValueByIdentity(string deviceId, short channelId, double value, IProgress<ApplicationStatus> progress, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            try {
                var hub = SwitchHub;
                if (hub?.Connected != true) {
                    throw new InvalidOperationException(Loc.Instance["LblSwitchNotConnected"]);
                }

                var matches = hub.Switches.Where(s => s is ICompositeSwitchIdentity identity
                    ? string.Equals(identity.DeviceId, deviceId, StringComparison.Ordinal) && identity.ChannelId == channelId
                    : string.Equals(hub.Id, deviceId, StringComparison.Ordinal) && s.Id == channelId).ToList();
                if (string.IsNullOrEmpty(deviceId) || matches.Count != 1 || !(matches[0] is IWritableSwitch target)
                    || !WritableSwitches.Contains(target)) {
                    throw new InvalidOperationException(Loc.Instance["LblHubSwitchTargetInvalid"]);
                }

                EnsureLiveSwitch(hub, target);
                target.TargetValue = value;
                await SetSwitchValue(target, token);
            } catch (NotSupportedException ex) {
                // Reserve NotSupportedException for an unavailable identity API, never a failed driver write.
                throw new InvalidOperationException(ex.Message, ex);
            }
        }

        [Obsolete("Use SetSwitchValueByIdentity. Index-based writes are retained only when identity support is unavailable.")]
        public Task SetSwitchValue(short switchIndex, double value, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            ct.ThrowIfCancellationRequested();
            if (switchIndex >= 0 && this.WritableSwitches.Count > switchIndex) {
                var writableSwitch = this.WritableSwitches[switchIndex];
                EnsureLiveSwitch(SwitchHub, writableSwitch);
                writableSwitch.TargetValue = value;
                return SetSwitchValue(writableSwitch, ct);
            } else {
                Logger.Error($"No switch found for index {switchIndex}");
                return Task.CompletedTask;
            }
        }

        public IAsyncCommand SetSwitchValueCommand { get; private set; }
        public IAsyncCommand ToggleBooleanSwitchValueCommand { get; private set; }

        [ObservableProperty]
        private ISwitchHub switchHub;

        [ObservableProperty]
        private IList<IWritableSwitch> writableSwitches;

        [ObservableProperty]
        private IList<ISwitch> readonlySwitches;

        [ObservableProperty]
        private IWritableSwitch selectedWritableSwitch;

        [ObservableProperty]
        private SwitchInfo switchInfo;

        private Dictionary<string, object> GetSwitchValues() {
            var hub = SwitchHub;
            if (hub?.Connected == true) {
                foreach (var s in hub.Switches) {
                    if (!hub.Connected) {
                        break;
                    }

                    s.Poll();
                }
            }

            (hub as IEquipmentHub)?.RefreshConnectionStatuses();

            return new Dictionary<string, object> {
                { nameof(SwitchInfo.Connected), ReferenceEquals(SwitchHub, hub) && hub?.Connected == true }
            };
        }

        private void UpdateSwitchValues(Dictionary<string, object> rotatorValues) {
            object o = null;
            rotatorValues.TryGetValue(nameof(SwitchInfo.Connected), out o);
            SwitchInfo.Connected = (bool)(o ?? false);

            BroadcastSwitchInfo();
        }

        private void BroadcastSwitchInfo() {
            switchMediator.Broadcast(GetDeviceInfo());
        }       

        private void CancelConnect() {
            try { connectSwitchCts?.Cancel(); } catch { }
        }

        public IDeviceChooserVM DeviceChooserVM { get; set; }

        public IApplicationStatusMediator applicationStatusMediator { get; private set; }

        private ISwitchMediator switchMediator;
        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);
        private DeviceUpdateTimer updateTimer;
        private CancellationTokenSource connectSwitchCts;

        public event Func<object, EventArgs, Task> Connected;
        public event Func<object, EventArgs, Task> Disconnected;

        public async Task<bool> Connect() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (DeviceChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.SwitchSettings.Id = DeviceChooserVM.SelectedDevice.Id;
                    profileService.ActiveProfile.SwitchSettings.LastDeviceName = string.Empty;
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

                var switchHub = (ISwitchHub)DeviceChooserVM.SelectedDevice;
                try { connectSwitchCts?.Dispose(); } catch { }
                connectSwitchCts = new CancellationTokenSource();
                if (switchHub != null) {
                    try {
                        var token = connectSwitchCts.Token;
                        var connected = await switchHub?.Connect(token);
                        if (connected) {
                            this.SwitchHub = switchHub;
                            token.ThrowIfCancellationRequested();

                            WritableSwitches = new AsyncObservableCollection<IWritableSwitch>();
                            ReadonlySwitches = new AsyncObservableCollection<ISwitch>();
                            foreach (var s in SwitchHub.Switches) {
                                token.ThrowIfCancellationRequested();
                                if (s is IWritableSwitch ws) {
                                    WritableSwitches.Add(ws);
                                } else {
                                    ReadonlySwitches.Add(s);
                                }
                            }
                            token.ThrowIfCancellationRequested();
                            SelectedWritableSwitch = WritableSwitches.FirstOrDefault();

                            SwitchInfo = new SwitchInfo {
                                Connected = true,
                                Name = switchHub.Name,
                                DisplayName = switchHub.DisplayName,
                                Description = switchHub.Description,
                                DriverInfo = switchHub.DriverInfo,
                                DriverVersion = switchHub.DriverVersion,
                                WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(WritableSwitches),
                                ReadonlySwitches = new ReadOnlyCollection<ISwitch>(ReadonlySwitches),
                                DeviceId = switchHub.Id,
                                SupportedActions = switchHub.SupportedActions,
                            };
                            token.ThrowIfCancellationRequested();

                            RaisePropertyChanged(nameof(WritableSwitches));
                            token.ThrowIfCancellationRequested();

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            _ = updateTimer.Run();

                            profileService.ActiveProfile.SwitchSettings.Id = switchHub.Id;
                            profileService.ActiveProfile.SwitchSettings.LastDeviceName = switchHub.DisplayName;
                            Notification.ShowSuccess(Loc.Instance["LblSwitchConnected"]);

                            BroadcastSwitchInfo();

                            await (Connected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                            Logger.Info($"Successfully connected Switch. Id: {switchHub.Id} Name: {switchHub.Name} DisplayName: {switchHub.DisplayName} Driver Version: {switchHub.DriverVersion}");

                            return true;
                        } else {
                            SwitchInfo.Connected = false;
                            this.SwitchHub = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        await Disconnect();
                        return false;
                    } catch (Exception ex) {
                        Notification.ShowError(ex.Message);
                        Logger.Error(ex);
                        if (SwitchInfo.Connected) { await Disconnect(); }
                        SwitchInfo.Connected = false;
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

        public async Task Disconnect() {
            try {
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }
                SwitchHub?.Disconnect();
                WritableSwitches = new AsyncObservableCollection<IWritableSwitch>();
                ReadonlySwitches = new AsyncObservableCollection<ISwitch>();
                SwitchHub = null;
                SwitchInfo.Reset();
                BroadcastSwitchInfo();
                await (Disconnected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                Logger.Info("Disconnected Switch");
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public SwitchInfo GetDeviceInfo() {
            return SwitchInfo;
        }

        public string Action(string actionName, string actionParameters = "") {
            return SwitchInfo?.Connected == true ? SwitchHub.Action(actionName, actionParameters) : null;
        }

        public string SendCommandString(string command, bool raw = true) {
            return SwitchInfo?.Connected == true ? SwitchHub.SendCommandString(command, raw) : null;
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return SwitchInfo?.Connected == true ? SwitchHub.SendCommandBool(command, raw) : false;
        }

        public void SendCommandBlind(string command, bool raw = true) {
            if (SwitchInfo?.Connected == true) {
                SwitchHub.SendCommandBlind(command, raw);
            }
        }
        public IDevice GetDevice() {
            return SwitchHub;
        }

        public IAsyncCommand ConnectCommand { get; set; }
        public ICommand CancelConnectCommand { get; set; }
        public ICommand DisconnectCommand { get; set; }
        public IAsyncCommand RescanDevicesCommand { get; set; }
    }
}