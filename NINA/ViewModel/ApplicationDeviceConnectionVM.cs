#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Utility;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NINA.ViewModel.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using Nito.AsyncEx;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class ApplicationDeviceConnectionVM : BaseVM, IApplicationDeviceConnectionVM {
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IWeatherDataMediator weatherDataMediator;
        private readonly ISwitchMediator switchMediator;
        private readonly ISafetyMonitorMediator safetyMonitorMediator;
        private readonly IUsbDeviceWatcher usbDeviceWatcher;

        public ApplicationDeviceConnectionVM(IProfileService profileService,
                                             ICameraMediator camMediator,
                                             ITelescopeMediator teleMediator,
                                             IFocuserMediator focMediator,
                                             IFilterWheelMediator fwMediator,
                                             IRotatorMediator rotMediator,
                                             IFlatDeviceMediator flatdMediator,
                                             IGuiderMediator guidMediator,
                                             ISwitchMediator swMediator,
                                             IWeatherDataMediator weatherMediator,
                                             IDomeMediator domMediator,
                                             ISafetyMonitorMediator safetyMonitorMediator,
                                             IPluginLoader pluginLoader,
                                             IUsbDeviceWatcher usbDeviceWatcher) : base(profileService) {
            cameraMediator = camMediator;
            telescopeMediator = teleMediator;
            focuserMediator = focMediator;
            filterWheelMediator = fwMediator;
            rotatorMediator = rotMediator;
            flatDeviceMediator = flatdMediator;
            guiderMediator = guidMediator;
            switchMediator = swMediator;
            domeMediator = domMediator;
            weatherDataMediator = weatherMediator;
            this.safetyMonitorMediator = safetyMonitorMediator;

            this.usbDeviceWatcher = usbDeviceWatcher;
            
            _ = Task.Run(async () => {
                await pluginLoader.Load();
                this.usbDeviceWatcher.Start();
                this.usbDeviceWatcher.DeviceInserted += UsbDeviceWatcher_DeviceInserted;
                this.usbDeviceWatcher.DeviceRemoved += UsbDeviceWatcher_DeviceRemoved;
                Initialized = true;
            });

            ConnectAllDevicesCommand = new AsyncCommand<bool>(async () => {
                var diag = MyMessageBox.Show(Loc.Instance["LblConnectAll"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    return await Task<bool>.Run(async () => {
                        try {
                            Logger.Debug("Connecting to camera");
                            await Task.Run(cameraMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Filter Wheel");
                            await Task.Run(filterWheelMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Telescope");
                            await Task.Run(telescopeMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Focuser");
                            await Task.Run(focuserMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Rotator");
                            await Task.Run(rotatorMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Guider");
                            await Task.Run(guiderMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Flat Device");
                            await Task.Run(flatDeviceMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Weather Data");
                            await Task.Run(weatherDataMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Switch");
                            await Task.Run(switchMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Dome");
                            await Task.Run(domeMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Safety Monitor");
                            await Task.Run(safetyMonitorMediator.Connect);
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        AllConnected = true;
                        return true;
                    });
                } else {
                    AllConnected = false;
                    return false;
                }
            }, (object o) => Initialized);

            DisconnectAllDevicesCommand = new AsyncCommand<bool>(async () => {
                var diag = MyMessageBox.Show(Loc.Instance["LblDisconnectAll"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    await DisconnectEquipment();
                    AllConnected = false;
                    return true;
                }
                return false;
            }, (object o) => Initialized);
        }

        private void UsbDeviceWatcher_DeviceInserted(object sender, UsbDeviceEventArgs e) {
            if (e?.DeviceInfo == null) return;
            Logger.Info($"New USB device detected {e.DeviceInfo}");
        }

        private void UsbDeviceWatcher_DeviceRemoved(object sender, UsbDeviceEventArgs e) {
            if (e?.DeviceInfo == null) return;
            Logger.Info($"USB Device disconnected {e.DeviceInfo}");
        }

        private object lockObj = new object();
        private bool initialized;
        public bool Initialized {
            get {
                lock (lockObj) {
                    return initialized;
                }
            }
            private set {
                lock (lockObj) {
                    initialized = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool allConnected = false;

        public bool AllConnected {
            get => allConnected;
            set {
                allConnected = value;
                RaisePropertyChanged();
            }
        }

        public void Shutdown() {
            this.usbDeviceWatcher.DeviceInserted -= UsbDeviceWatcher_DeviceInserted;
            this.usbDeviceWatcher.DeviceRemoved -= UsbDeviceWatcher_DeviceRemoved;
            usbDeviceWatcher.Stop();
            AsyncContext.Run(DisconnectEquipment);
            try {
                NINA.Equipment.SDK.CameraSDKs.AtikSDK.AtikCameraDll.Shutdown();
            } catch (Exception) { }
        }

        public async Task DisconnectEquipment() {
            try {
                await guiderMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await domeMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await flatDeviceMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await cameraMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await telescopeMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await filterWheelMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await focuserMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await rotatorMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await switchMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await weatherDataMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                await safetyMonitorMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public ICommand ConnectAllDevicesCommand { get; private set; }
        public ICommand DisconnectAllDevicesCommand { get; private set; }
    }
}