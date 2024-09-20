#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
                 profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
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
            var aSwitch = (IWritableSwitch)arg;

            await aSwitch.SetValue();

            await aSwitch.Poll();

            var timeOut = TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval * 4);
            bool success = true;
            while (Math.Abs(aSwitch.Value - aSwitch.TargetValue) > SWITCHTOLERANCE) {
                var elapsed = await CoreUtil.Wait(TimeSpan.FromMilliseconds(500));
                timeOut = timeOut - elapsed;
                if (timeOut.TotalMilliseconds <= 0) {
                    success = false;
                    break;
                }
            }

            if (!success) {
                var notification = string.Format(Loc.Instance["LblTimeoutToSetSwitchValue"], aSwitch.TargetValue, aSwitch.Id, aSwitch.Name, aSwitch.Value);
                Notification.ShowError(notification);
                Logger.Error(notification);
            }
            return success;
        }

        public Task SetSwitchValue(short switchIndex, double value, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (this.WritableSwitches.Count > switchIndex) {
                var writableSwitch = this.WritableSwitches[switchIndex];
                writableSwitch.TargetValue = value;
                return SetSwitchValue(writableSwitch);
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
            Dictionary<string, object> switchValues = new Dictionary<string, object>();
            switchValues.Add(nameof(SwitchInfo.Connected), SwitchHub?.Connected ?? false);

            var tasks = new List<Task>();
            foreach (var s in SwitchHub.Switches) {
                tasks.Add(s.Poll());
            }
            AsyncContext.Run(async () => await Task.WhenAll(tasks));
            return switchValues;
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
                    return false;
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
                            BroadcastSwitchInfo();
                            token.ThrowIfCancellationRequested();

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            _ = updateTimer.Run();

                            profileService.ActiveProfile.SwitchSettings.Id = switchHub.Id;
                            Notification.ShowSuccess(Loc.Instance["LblSwitchConnected"]);

                            await (Connected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                            Logger.Info($"Successfully connected Switch. Id: {switchHub.Id} Name: {switchHub.Name} DisplayName: {switchHub.DisplayName} Driver Version: {switchHub.DriverVersion}");

                            return true;
                        } else {
                            Notification.ShowError($"Unable to connect to {DeviceChooserVM.SelectedDevice.Name}");
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