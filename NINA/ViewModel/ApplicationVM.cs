#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Windows;
using System.Windows.Input;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.IO;
using System.Linq;
using NINA.Plugin.Interfaces;
using Nito.AsyncEx;
using System.Diagnostics;
using NINA.Astrometry;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading.Tasks;
using NINA.INDI;

namespace NINA.ViewModel {

    internal partial class ApplicationVM : BaseVM, IApplicationVM, ICameraConsumer {

        public ApplicationVM(IProfileService profileService,
                             ICameraMediator cameraMediator,
                             IApplicationMediator applicationMediator,
                             IImageSaveMediator imageSaveMediator,
                             IPluginLoader pluginProvider,
                             IApplicationDeviceConnectionVM applicationDeviceConnectionVM) : base(profileService) {
            applicationMediator.RegisterHandler(this);
            this.cameraMediator = cameraMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.pluginProvider = pluginProvider;
            this.applicationDeviceConnectionVM = applicationDeviceConnectionVM;
            cameraMediator.RegisterConsumer(this);

            profileService.ProfileChanged += ProfileService_ProfileChanged;
            SubscribeSystemEvents();
        }

        [RelayCommand]
        private void CollapseTabControl() {
            Collapsed = true;
        }
        [RelayCommand]
        private void ExpandTabControl() {
            Collapsed = false;
        }

        [RelayCommand]
        private void CheckASCOMPlatformVersion() {
        }

        [RelayCommand]
        private Task CheckDiskInfo() {
            return Task.Run(() => {
                var sw = Stopwatch.StartNew();
                try {
                    foreach (var drive in DriveInfo.GetDrives()) {
                        try {
                            if (drive.IsReady) {
                                Logger.Info(string.Format("Available Space on Drive {0}: {1} GB", drive.Name, Math.Round(drive.AvailableFreeSpace / (1024d * 1024d * 1024d), 2).ToString(CultureInfo.InvariantCulture)));
                            } else {
                                Logger.Info(string.Format("Drive {0} is not ready", drive.Name));
                            }
                        } catch {
                            Logger.Info(string.Format("Error occurred to retrieve drive info for {0}", drive.Name));
                        }
                    }
                } catch {
                    Logger.Info("Unable to retrieve drive info");
                }
                var elapsed = sw.Elapsed;
            });
        }

        [RelayCommand]
        private void CheckWindowsVersion() {
        }

        public bool Collapsed {
            get => Properties.Settings.Default.CollapsedSidebar;
            set {
                Properties.Settings.Default.CollapsedSidebar = value;
                CoreUtil.SaveSettings(Properties.Settings.Default);
                OnPropertyChanged();
            }
        }

        [RelayCommand]
        private void CheckEphemerisExists(object o) {
            if (!File.Exists(NOVAS.EphemerisLocation)) {
                Logger.Error("Ephemeris file is missing");
                Notification.ShowError(Loc.Instance["LblEphemerisNotFound"]);
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            OnPropertyChanged(nameof(ActiveProfile));
        }

        [RelayCommand]
        private void OpenManual() {
        }

        [RelayCommand]
        private void OpenAbout() {
        }

        [RelayCommand]
        private void ChangeResolution(string resolution) {
        }

        public void ChangeTab(ApplicationTab tab) {
            TabIndex = (int)tab;
        }
        public string Version => CoreUtil.VersionFriendlyName;

        public string Title => CoreUtil.Title;

        private bool _indigoCleanupCompleted = false;

        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
        private readonly ICameraMediator cameraMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IPluginLoader pluginProvider;
        private readonly IDockManagerVM dockManager;
        private readonly IApplicationDeviceConnectionVM applicationDeviceConnectionVM;

        [ObservableProperty]
        private int tabIndex;


        [RelayCommand]
        private static void MaximizeWindow() {
        }

        [RelayCommand]
        private void MinimizeWindow() {
        }

        [RelayCommand]
        private void Exit() {
        }

        private void SubscribeSystemEvents() {
        }

        private void UnsubscribeSystemEvents() {
        }

        [RelayCommand]
        private void Closing() {
            Logger.Info("Application shutting down");
            UnsubscribeSystemEvents();
            try {
                Logger.Debug("Saving dock layout");
            } catch { }

            try {
                Logger.Debug("Shutting down INDI client");
                INDIClient.Instance.Dispose();
            } catch (Exception ex) {
                Logger.Error("Failed to dispose INDI client", ex);
            }

            try {
                Logger.Debug("Disconnecting equipment");
                applicationDeviceConnectionVM.Shutdown();
            } catch { }
            try {
                Logger.Debug("Releasing profile");
                profileService.Release();
            } catch { }
            try {
                Logger.Debug("Saving user.settings");
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
            } catch { }

            try {
                Logger.Debug("Shutting down ImageSaveMediator");
                imageSaveMediator.Shutdown();
            } catch { }

            try {
                Logger.Debug("Closing NOVAS Ephem");
                NOVAS.Shutdown();
            } catch { }

            try {
                foreach (var plugin in pluginProvider.Plugins) {
                    if (plugin.Value) {
                        try {
                            Logger.Debug($"Tearing down plugin {plugin.Key.Name}");
                            AsyncContext.Run(plugin.Key.Teardown);
                        } catch (Exception ex) {
                            Logger.Error($"Failed to teardown plugin {plugin.Key.Name}", ex);
                        }
                    }
                }
            } catch { }

            Logger.CloseAndFlush();
            Notification.Dispose();

            Environment.Exit(0);
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            cameraInfo = deviceInfo;
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
        }
    }
}
