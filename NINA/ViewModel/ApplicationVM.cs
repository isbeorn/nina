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
using NINA.Utility;
using NINA.View.About;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Management;
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
using NINA.WPF.Base.View;

namespace NINA.ViewModel {

    internal partial class ApplicationVM : BaseVM, IApplicationVM, ICameraConsumer {

        public ApplicationVM(IProfileService profileService,
                             ProjectVersion projectVersion,
                             ICameraMediator cameraMediator,
                             IApplicationMediator applicationMediator,
                             IImageSaveMediator imageSaveMediator,
                             IPluginLoader pluginProvider,
                             IDockManagerVM dockManagerVM,
                             IApplicationDeviceConnectionVM applicationDeviceConnectionVM) : base(profileService) {
            applicationMediator.RegisterHandler(this);
            this.projectVersion = projectVersion;
            this.cameraMediator = cameraMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.pluginProvider = pluginProvider;
            this.dockManager = dockManagerVM;
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
            try {
                var version = ASCOMInteraction.GetPlatformVersion();
                Logger.Info($"ASCOM Platform {version} installed");
                var recommendedVersion = new Version("6.6.2.4195");
                if (version < recommendedVersion) {
                    Logger.Error($"Outdated ASCOM Platform detected. Current: {version} - Minimum Required: {recommendedVersion}");
                    Notification.ShowWarning(Loc.Instance["LblASCOMPlatformOutdated"]);
                }
            } catch (Exception) {
                Logger.Info($"No ASCOM Platform installed");
            }
        }

        [RelayCommand]
        private void CheckWindowsVersion() {
            try {
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { 
                    // Minimum support Windows version is (curently) Windows 10 1507
                    var minimumVersion = new Version(10, 0, 10240);

                    Regex versionRegex = new Regex(@"\d+(\.\d+)+");
                    Match match = versionRegex.Match(RuntimeInformation.OSDescription);
                    if (match.Success) {
                        var osVersion = new Version(match.Value);
                        if (osVersion < minimumVersion) {
                            Logger.Error($"Windows version {RuntimeInformation.OSDescription} below supported version {minimumVersion}");
                            Notification.ShowError(string.Format(Loc.Instance["LblYourWindowsIsTooOld"], RuntimeInformation.OSDescription));
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
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
                Notification.ShowError(Loc.Instance["LblEphemerisNotFound"]);
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            OnPropertyChanged(nameof(ActiveProfile));
        }

        [RelayCommand]
        private void OpenManual() {
            Browser browser = new Browser();
            browser.MinWidth = 1280;
            browser.MinHeight = 720;
            if(File.Exists(CoreUtil.DocumentationLocalPage)) {
                browser.Source = new Uri(CoreUtil.DocumentationLocalPage);
            } else {
                browser.Source = new Uri(CoreUtil.DocumentationPage);
            }
            browser.Margin = new Thickness(2,0,2,2);
            var service = new WindowServiceFactory().Create();
            service.Show(browser, Title + " - " + Loc.Instance["LblDocumentation"], ResizeMode.CanResize, WindowStyle.ToolWindow);
        }

        [RelayCommand]
        private void OpenAbout() {
            AboutPageView window = new AboutPageView();
            window.Width = 1280;
            window.Height = 720;
            var service = new WindowServiceFactory().Create();
            service.Show(window, Title + " - " + Loc.Instance["LblAbout"], ResizeMode.NoResize, WindowStyle.ToolWindow);
        }

        [RelayCommand]
        private void ChangeResolution(string resolution) {
            var split = resolution.Split("x");
            if (split.Length == 2) {
                var width = double.Parse(split[0], CultureInfo.InvariantCulture);
                var height = double.Parse(split[1], CultureInfo.InvariantCulture);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Width = width;
                Application.Current.MainWindow.Height = height;
            }
        }

        public void ChangeTab(ApplicationTab tab) {
            TabIndex = (int)tab;
        }

        public string Version => projectVersion.ToString();

        public string Title => NINA.Core.Utility.CoreUtil.Title;

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
            if (Application.Current.MainWindow.WindowState == WindowState.Maximized) {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            } else {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
        }

        [RelayCommand]
        private void MinimizeWindow() {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        [RelayCommand]
        private void Exit() {
            Sequencer.ISequenceNavigationVM vm = ((Interfaces.IMainWindowVM)Application.Current.MainWindow.DataContext).SequenceNavigationVM;
            if (vm.Initialized) {
                if (vm.Sequence2VM.Sequencer.MainContainer.AskHasChanged(vm.Sequence2VM.Sequencer.MainContainer.Name)) {
                    return;
                }
                if (((SimpleSequenceVM)vm.SimpleSequenceVM).AskHasChanged()) {
                    return;
                }
                if (cameraInfo.Connected) {
                    var diag = MyMessageBox.Show(Loc.Instance["LblCameraConnectedOnExit"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                    if (diag != MessageBoxResult.OK) {
                        return;
                    }
                }
            }

            Application.Current.Shutdown();
        }

        private void SubscribeSystemEvents() {
            try {
                Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                Microsoft.Win32.SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            } catch { }
        }

        private void UnsubscribeSystemEvents() {
            try {
                Microsoft.Win32.SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                Microsoft.Win32.SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
            } catch { }
        }

        private void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e) {
            switch (e.Reason) {
                case Microsoft.Win32.SessionEndReasons.SystemShutdown:
                    Logger.Info("The operating system is shutting down.");
                    break;
                case Microsoft.Win32.SessionEndReasons.Logoff:
                    Logger.Info("The user is logging off and ending the current user session. The operating system continues to run.");
                    break;
            }
        }

        private void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e) {
            switch (e.Mode) {
                case Microsoft.Win32.PowerModes.Resume:
                    Logger.Info("The operating system is about to resume from a suspended state.");
                    break;
                case Microsoft.Win32.PowerModes.StatusChange:
                    Logger.Info("A power mode status notification event has been raised by the operating system. This might indicate a weak or charging battery, a transition between AC power and battery, or another change in the status of the system power supply.");
                    break;
                case Microsoft.Win32.PowerModes.Suspend:
                    Logger.Info("The operating system is about to be suspended.");
                    break;
            }
        }

        [RelayCommand]
        private void Closing() {
            UnsubscribeSystemEvents();
            try {
                Logger.Debug("Saving dock layout");
                dockManager.SaveAvalonDockLayout();
            } catch { }
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

        private readonly ProjectVersion projectVersion;


    }
}