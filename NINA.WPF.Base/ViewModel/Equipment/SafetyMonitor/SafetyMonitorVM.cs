#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;
using Nito.AsyncEx;
using NINA.Core.Utility.Extensions;

namespace NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor {

    public class SafetyMonitorVM : DockableVM, ISafetyMonitorVM {
        private ISafetyMonitorMediator safetyMonitorMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private DeviceUpdateTimer updateTimer;
        private CancellationTokenSource connectCts;
        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        public SafetyMonitorVM(IProfileService profileService,
                               ISafetyMonitorMediator safetyMonitorMediator,
                               IApplicationStatusMediator applicationStatusMediator,
                               IDeviceChooserVM deviceChooserVM) : base(profileService) {
            Title = Loc.Instance["LblSafetyMonitor"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ShieldSVG"];

            this.safetyMonitorMediator = safetyMonitorMediator;
            this.safetyMonitorMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;
            this.DeviceChooserVM = deviceChooserVM;

            ConnectCommand = new AsyncCommand<bool>(() => Task.Run(Connect), (object o) => DeviceChooserVM.SelectedDevice != null);
            CancelConnectCommand = new RelayCommand(CancelConnect);
            DisconnectCommand = new AsyncCommand<bool>(() => Task.Run(DisconnectDiag));
            RescanDevicesCommand = new AsyncCommand<bool>(async o => { await Rescan(); return true; }, o => !SafetyMonitorInfo.Connected);
            _ = RescanDevicesCommand.ExecuteAsync(null);

            updateTimer = new DeviceUpdateTimer(
                GetMonitorValues,
                UpdateMonitorValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += async (object sender, EventArgs e) => {
                await RescanDevicesCommand.ExecuteAsync(null);
            };
        }

        public event EventHandler<IsSafeEventArgs> IsSafeChanged;

        public async Task<IList<string>> Rescan() {
            return await Task.Run(async () => {
                await DeviceChooserVM.GetEquipment();
                return DeviceChooserVM.Devices.Select(x => x.Id).ToList();
            });
        }
        public IDeviceChooserVM DeviceChooserVM { get; set; }

        private void UpdateMonitorValues(Dictionary<string, object> monitorValues) {
            object o = null;
            monitorValues.TryGetValue(nameof(SafetyMonitorInfo.Connected), out o);
            SafetyMonitorInfo.Connected = (bool)(o ?? false);

            bool isSafe = SafetyMonitorInfo.IsSafe;
            monitorValues.TryGetValue(nameof(SafetyMonitorInfo.IsSafe), out o);
            SafetyMonitorInfo.IsSafe = (bool)(o ?? false);
            if (SafetyMonitorInfo.IsSafe != isSafe) {
                Logger.Info("SafetyMonitorInfo state changed to " + (isSafe ? "Unsafe" : "Safe"));
                try {
                    IsSafeChanged?.Invoke(this, new IsSafeEventArgs(SafetyMonitorInfo.IsSafe));
                } catch (Exception ex) {
                    Logger.Error(ex);
                }                    
            }

            BroadcastMonitorInfo();
        }

        private void BroadcastMonitorInfo() {
            safetyMonitorMediator.Broadcast(GetDeviceInfo());
        }

        private Dictionary<string, object> GetMonitorValues() {
            Dictionary<string, object> safetyMonitorValues = new Dictionary<string, object>();
            safetyMonitorValues.Add(nameof(SafetyMonitorInfo.Connected), SafetyMonitor?.Connected ?? false);
            safetyMonitorValues.Add(nameof(SafetyMonitorInfo.IsSafe), SafetyMonitor?.IsSafe ?? false);

            return safetyMonitorValues;
        }

        public async Task<bool> Connect() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (DeviceChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.SafetyMonitorSettings.Id = DeviceChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );

                var sm = (ISafetyMonitor)DeviceChooserVM.SelectedDevice;
                connectCts?.Dispose();
                connectCts = new CancellationTokenSource();
                var token = connectCts.Token;
                if (sm != null) {
                    try {
                        var connected = await sm?.Connect(connectCts.Token);
                        if (connected) {
                            this.SafetyMonitor = sm;
                            token.ThrowIfCancellationRequested();

                            SafetyMonitorInfo = new SafetyMonitorInfo {
                                Connected = true,
                                IsSafe = sm.IsSafe,
                                Name = sm.Name,
                                DisplayName = sm.DisplayName,
                                Description = sm.Description,
                                DriverInfo = sm.DriverInfo,
                                DriverVersion = sm.DriverVersion,
                                DeviceId = sm.Id,
                                SupportedActions = sm.SupportedActions,
                            };

                            Notification.ShowSuccess(Loc.Instance["LblSafetyMonitorConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            _ = updateTimer.Run();

                            profileService.ActiveProfile.SafetyMonitorSettings.Id = sm.Id;

                            await (Connected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                            Logger.Info($"Successfully connected Safety Monitor. Id: {sm.Id} Name: {sm.Name} DisplayName: {sm.DisplayName} Driver Version: {sm.DriverVersion}");

                            return true;
                        } else {
                            SafetyMonitorInfo.Connected = false;
                            this.SafetyMonitor = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (sm?.Connected == true) { await Disconnect(); }
                        return false;
                    } catch (Exception ex) {
                        Notification.ShowError(ex.Message);
                        Logger.Error(ex);
                        if (SafetyMonitorInfo.Connected) { await Disconnect(); }
                        SafetyMonitorInfo.Connected = false;
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

        private void CancelConnect(object o) {
            try { connectCts?.Cancel(); } catch { }
        }

        public async Task Disconnect() {
            try {
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }
                SafetyMonitor?.Disconnect();
                SafetyMonitor = null;
                SafetyMonitorInfo.Reset();
                BroadcastMonitorInfo();
                await (Disconnected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                Logger.Info("Disconnected Safety Monitor");
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public string Action(string actionName, string actionParameters = "") {
            return SafetyMonitorInfo?.Connected == true ? SafetyMonitor.Action(actionName, actionParameters) : null;
        }

        public string SendCommandString(string command, bool raw = true) {
            return SafetyMonitorInfo?.Connected == true ? SafetyMonitor.SendCommandString(command, raw) : null;
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return SafetyMonitorInfo?.Connected == true ? SafetyMonitor.SendCommandBool(command, raw) : false;
        }

        public void SendCommandBlind(string command, bool raw = true) {
            if (SafetyMonitorInfo?.Connected == true) {
                SafetyMonitor.SendCommandBlind(command, raw);
            }
        }

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.Show(Loc.Instance["LblDisconnectSafetyMonitor"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public SafetyMonitorInfo GetDeviceInfo() {
            return SafetyMonitorInfo;
        }
        
        private ISafetyMonitor safetyMonitor;
        public ISafetyMonitor SafetyMonitor {
            get => safetyMonitor;
            private set {
                safetyMonitor = value;
                RaisePropertyChanged();
            }
        }

        private SafetyMonitorInfo safetyMonitorInfo;

        public event Func<object, EventArgs, Task> Connected;
        public event Func<object, EventArgs, Task> Disconnected;

        public SafetyMonitorInfo SafetyMonitorInfo {
            get {
                if (safetyMonitorInfo == null) {
                    safetyMonitorInfo = DeviceInfo.CreateDefaultInstance<SafetyMonitorInfo>();
                }
                return safetyMonitorInfo;
            }
            set {
                safetyMonitorInfo = value;
                RaisePropertyChanged();
            }
        }
        public IDevice GetDevice() {
            return SafetyMonitor;
        }

        public IAsyncCommand ConnectCommand { get; private set; }
        public ICommand CancelConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public IAsyncCommand RescanDevicesCommand { get; private set; }
    }
}