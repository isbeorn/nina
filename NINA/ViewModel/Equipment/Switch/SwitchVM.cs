#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MySwitch;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.ViewModel.Interfaces;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.Switch {

    internal class SwitchVM : DockableVM, ISwitchVM {

        public SwitchVM(IProfileService profileService, IApplicationStatusMediator applicationStatusMediator, ISwitchMediator switchMediator) : base(profileService) {
            Title = "LblSwitch";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SwitchesSVG"];
            SwitchChooserVM = new SwitchChooserVM(profileService);
            SwitchChooserVM.GetEquipment();

            this.applicationStatusMediator = applicationStatusMediator;
            this.switchMediator = switchMediator;
            this.switchMediator.RegisterHandler(this);

            ConnectCommand = new AsyncCommand<bool>(Connect);
            DisconnectCommand = new AsyncCommand<bool>(async () => { await Disconnect(); return true; });
            CancelConnectCommand = new RelayCommand((object o) => CancelConnect());
            RefreshDevicesCommand = new RelayCommand((object o) => RefreshDevices(), o => !(SwitchHub?.Connected == true));

            updateTimer = new DeviceUpdateTimer(
                 GetSwitchValues,
                 UpdateSwitchValues,
                 profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
             );

            SetSwitchValueCommand = new AsyncCommand<bool>(SetSwitchValue);
        }

        private async Task<bool> SetSwitchValue(object arg) {
            var aSwitch = (IWritableSwitch)arg;

            await aSwitch.SetValue();

            await aSwitch.Poll();

            var timeOut = TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval * 4);
            bool success = true;
            while (aSwitch.Value != aSwitch.TargetValue) {
                var elapsed = await Utility.Utility.Wait(TimeSpan.FromMilliseconds(500));
                timeOut = timeOut - elapsed;
                if (timeOut.TotalMilliseconds <= 0) {
                    success = false;
                    break;
                }
            }

            if (!success) {
                var notification = string.Format(Locale.Loc.Instance["LblTimeoutToSetSwitchValue"], aSwitch.TargetValue, aSwitch.Id, aSwitch.Name, aSwitch.Value);
                Notification.ShowError(notification);
                Logger.Error(notification);
            }
            return success;
        }

        public IAsyncCommand SetSwitchValueCommand { get; private set; }

        private ISwitchHub switchHub;

        public ISwitchHub SwitchHub {
            get => switchHub;
            set {
                switchHub = value;
                RaisePropertyChanged();
            }
        }

        public ICollection<IWritableSwitch> WritableSwitches { get; private set; } = new AsyncObservableCollection<IWritableSwitch>();

        private IWritableSwitch selectedWritableSwitch;

        public IWritableSwitch SelectedWritableSwitch {
            get => selectedWritableSwitch;
            set {
                selectedWritableSwitch = value;
                RaisePropertyChanged();
            }
        }

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

        private SwitchInfo switchInfo;

        public SwitchInfo SwitchInfo {
            get {
                if (switchInfo == null) {
                    switchInfo = DeviceInfo.CreateDefaultInstance<SwitchInfo>();
                }
                return switchInfo;
            }
            set {
                switchInfo = value;
                RaisePropertyChanged();
            }
        }

        private void CancelConnect() {
            connectSwitchCts?.Cancel();
        }

        private void RefreshDevices() {
            SwitchChooserVM.GetEquipment();
        }

        public SwitchChooserVM SwitchChooserVM { get; set; }

        public IApplicationStatusMediator applicationStatusMediator { get; private set; }

        private ISwitchMediator switchMediator;
        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);
        private DeviceUpdateTimer updateTimer;
        private CancellationTokenSource connectSwitchCts;

        public async Task<bool> Connect() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (SwitchChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.SwitchSettings.Id = SwitchChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );

                var switchHub = (ISwitchHub)SwitchChooserVM.SelectedDevice;
                connectSwitchCts?.Dispose();
                connectSwitchCts = new CancellationTokenSource();
                if (switchHub != null) {
                    try {
                        var connected = await switchHub?.Connect(connectSwitchCts.Token);
                        connectSwitchCts.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.SwitchHub = switchHub;

                            SwitchInfo = new SwitchInfo {
                                Connected = true,
                                Name = switchHub.Name,
                                Description = switchHub.Description,
                                DriverInfo = switchHub.DriverInfo,
                                DriverVersion = switchHub.DriverVersion
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblSwitchConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            profileService.ActiveProfile.SwitchSettings.Id = switchHub.Id;

                            WritableSwitches.Clear();
                            foreach (var s in SwitchHub.Switches) {
                                if (s is IWritableSwitch) {
                                    WritableSwitches.Add((IWritableSwitch)s);
                                }
                            }
                            SelectedWritableSwitch = WritableSwitches.FirstOrDefault();
                            RaisePropertyChanged(nameof(WritableSwitches));
                            BroadcastSwitchInfo();

                            Logger.Info($"Successfully connected Switch. Id: {switchHub.Id} Name: {switchHub.Name} Driver Version: {switchHub.DriverVersion}");

                            return true;
                        } else {
                            Notification.ShowError($"Unable to connect to {SwitchChooserVM.SelectedDevice.Name}");
                            SwitchInfo.Connected = false;
                            this.SwitchHub = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (SwitchInfo.Connected) { await Disconnect(); }
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
            if (SwitchInfo.Connected) {
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }
                SwitchHub?.Disconnect();
                WritableSwitches.Clear();
                SwitchHub = null;
                SwitchInfo = DeviceInfo.CreateDefaultInstance<SwitchInfo>();
                BroadcastSwitchInfo();
                Logger.Info("Disconnected Switch");
            }
        }

        public SwitchInfo GetDeviceInfo() {
            return SwitchInfo;
        }

        public IAsyncCommand ConnectCommand { get; set; }
        public ICommand CancelConnectCommand { get; set; }
        public ICommand DisconnectCommand { get; set; }
        public ICommand RefreshDevicesCommand { get; set; }
    }
}