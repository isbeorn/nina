#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.ViewModel.Interfaces;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Imaging;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.Sequencer;
using NINA.ViewModel.ImageHistory;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;
using NINA.Core.Utility;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Profile;
using System.Linq;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.Plugin.Interfaces;
using System.Diagnostics;
using System.Threading;
using NINA.Core.Utility.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Xml.Linq;

namespace NINA.ViewModel {

    internal partial class DockManagerVM : BaseVM, IDockManagerVM {

        public DockManagerVM(IProfileService profileService,
                             ICameraVM cameraVM,
                             ISequenceNavigationVM sequenceNavigationVM,
                             IThumbnailVM thumbnailVM,
                             ISwitchVM switchVM,
                             IFilterWheelVM filterWheelVM,
                             IFocuserVM focuserVM,
                             IRotatorVM rotatorVM,
                             IWeatherDataVM weatherDataVM,
                             IDomeVM domeVM,
                             IAnchorableSnapshotVM snapshotVM,
                             IAnchorablePlateSolverVM plateSolverVM,
                             ITelescopeVM telescopeVM,
                             IGuiderVM guiderVM,
                             IFocusTargetsVM focusTargetsVM,
                             IAutoFocusToolVM autoFocusToolVM,
                             IImageHistoryVM imageHistoryVM,
                             IImageControlVM imageControlVM,
                             IImageStatisticsVM imageStatisticsVM,
                             IFlatDeviceVM flatDeviceVM,
                             ISafetyMonitorVM safetyMonitorVM,
                             IPluginLoader pluginProvider) : base(profileService) {
            LoadAvalonDockLayoutCommand = new AsyncCommand<bool>((object o) => Task.Run(() => InitializeAvalonDockLayout(o)));
            ResetDockLayoutCommand = new RelayCommand(ResetDockLayout, (object o) => _dockmanager != null);
            BackupDockLayoutCommand = new RelayCommand(BackupDockLayout, (object o) => _dockmanager != null);
            RestoreDockLayoutFromFileCommand = new RelayCommand(RestoreDockLayoutFromFile);

            var initAnchorables = new List<IDockableVM>();
            var initAnchorableInfoPanels = new List<IDockableVM>();
            var initAnchorableTools = new List<IDockableVM>();

            initAnchorables.Add(imageControlVM);
            initAnchorables.Add(cameraVM);
            initAnchorables.Add(filterWheelVM);
            initAnchorables.Add(focuserVM);
            initAnchorables.Add(rotatorVM);
            initAnchorables.Add(telescopeVM);
            initAnchorables.Add(guiderVM);
            initAnchorables.Add(switchVM);
            initAnchorables.Add(weatherDataVM);
            initAnchorables.Add(domeVM);

            initAnchorables.Add(sequenceNavigationVM);
            initAnchorables.Add(imageStatisticsVM);
            initAnchorables.Add(imageHistoryVM);

            initAnchorables.Add(snapshotVM);
            initAnchorables.Add(thumbnailVM);
            initAnchorables.Add(plateSolverVM);
            initAnchorables.Add(autoFocusToolVM);
            initAnchorables.Add(focusTargetsVM);
            initAnchorables.Add(flatDeviceVM);
            initAnchorables.Add(safetyMonitorVM);

            initAnchorableInfoPanels.Add(imageControlVM);
            initAnchorableInfoPanels.Add(cameraVM);
            initAnchorableInfoPanels.Add(filterWheelVM);
            initAnchorableInfoPanels.Add(focuserVM);
            initAnchorableInfoPanels.Add(rotatorVM);
            initAnchorableInfoPanels.Add(telescopeVM);
            initAnchorableInfoPanels.Add(guiderVM);
            initAnchorableInfoPanels.Add(sequenceNavigationVM);
            initAnchorableInfoPanels.Add(switchVM);
            initAnchorableInfoPanels.Add(weatherDataVM);
            initAnchorableInfoPanels.Add(domeVM);
            initAnchorableInfoPanels.Add(imageStatisticsVM);
            initAnchorableInfoPanels.Add(imageHistoryVM);
            initAnchorableInfoPanels.Add(flatDeviceVM);
            initAnchorableInfoPanels.Add(safetyMonitorVM);

            initAnchorableTools.Add(snapshotVM);
            initAnchorableTools.Add(thumbnailVM);
            initAnchorableTools.Add(plateSolverVM);
            initAnchorableTools.Add(autoFocusToolVM);
            initAnchorableTools.Add(focusTargetsVM);

            profileService.BeforeProfileChanging += ProfileService_BeforeProfileChanging; ;
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            isLocked = profileService.ActiveProfile.DockPanelSettings.IsLocked;

            Task.Run(async () => {
                await pluginProvider.Load();
                foreach (var dockable in pluginProvider.DockableVMs) {
                    initAnchorables.Add(dockable);
                    if (dockable.IsTool) {
                        initAnchorableTools.Add(dockable);
                    } else {
                        initAnchorableInfoPanels.Add(dockable);
                    }
                }
                Anchorables = initAnchorables;
                AnchorableInfoPanels = initAnchorableInfoPanels;
                AnchorableTools = initAnchorableTools;
                Initialized = true;
            });
        }

        private void ProfileService_BeforeProfileChanging(object sender, EventArgs e) {
            try {
                SaveAvalonDockLayout();
            } catch { }
        }

        private void RestoreDockLayoutFromFile(object obj) {
            try {
                var dialog = OptionsVM.GetFilteredFileDialog("", "DockBackup.dock.config", "Dock Config|*.dock.config");
                if (dialog.ShowDialog() == true) {
                    if(File.Exists(dialog.FileName)) {
                        lock(lockObj) {
                            _dockloaded = false;
                            File.Copy(dialog.FileName, GetDockConfigPath(profileService.ActiveProfile.Id), true);
                            Notification.ShowInformation(Loc.Instance["LblDockLayoutRestored"]);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblRestoreDockLayoutFromFileFailed"]);
            }
        }

        private void BackupDockLayout(object obj) {
            try {
                Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.InitialDirectory = "";
                dialog.FileName = "DockBackup.dock.config";
                dialog.Title = Loc.Instance["LblBackupDockLayout"];
                dialog.DefaultExt = ".dock.config";
                dialog.Filter = "Dock Config|*.dock.config";
                dialog.OverwritePrompt = true;

                if (dialog.ShowDialog().Value) {
                    if (Directory.Exists(Path.GetDirectoryName(dialog.FileName))) {
                        lock (lockObj) {
                            var serializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                            serializer.Serialize(dialog.FileName);
                            Notification.ShowInformation(Loc.Instance["LblBackupDockLayoutSuccessful"]);
                        }
                    }
                    
                }
            } catch(Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblBackupDockLayoutFailed"]);
            }
        }

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

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            lock (lockObj) {
                _dockloaded = false;
                pendingDockLayout = null;
            }
        }

        private void ResetDockLayout(object arg) {
            if (MyMessageBox.Show(Loc.Instance["LblResetDockLayoutConfirmation"], Loc.Instance["LblResetDockLayout"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                lock (lockObj) {
                    _dockloaded = false;

                    foreach (var item in Anchorables) {
                        item.IsVisible = false;
                    }

                    LoadDefaultLayout();
                    SaveAvalonDockLayout();
                    Notification.ShowInformation(Loc.Instance["LblDockLayoutReset"]);
                }
            }
        }

        [ObservableProperty]
        private bool isLocked = false;

        partial void OnIsLockedChanged(bool value) {
            profileService.ActiveProfile.DockPanelSettings.IsLocked = value;
        }

        private List<IDockableVM> _anchorables;

        public List<IDockableVM> Anchorables {
            get {
                if (_anchorables == null) {
                    _anchorables = new List<IDockableVM>();
                }
                return _anchorables;
            }
            private set {
                _anchorables = value;
                RaisePropertyChanged();
            }
        }

        private List<IDockableVM> _anchorableTools;

        public List<IDockableVM> AnchorableTools {
            get {
                if (_anchorableTools == null) {
                    _anchorableTools = new List<IDockableVM>();
                }
                return _anchorableTools;
            }
            private set {
                _anchorableTools = value;
                RaisePropertyChanged();
            }
        }

        private List<IDockableVM> _anchorableInfoPanels;

        public List<IDockableVM> AnchorableInfoPanels {
            get {
                if (_anchorableInfoPanels == null) {
                    _anchorableInfoPanels = new List<IDockableVM>();
                }
                return _anchorableInfoPanels;
            }
            private set {
                _anchorableInfoPanels = value;
                RaisePropertyChanged();
            }
        }

        private AvalonDock.DockingManager _dockmanager;
        private bool _dockloaded = false;
        private string pendingDockLayout;
        private object lockObj = new object();
        private Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        public static string GetDockConfigPath(Guid profileId) {
            if(Properties.Settings.Default.SingleDockLayout) {
                return Path.Combine(ProfileService.PROFILEFOLDER, $"GLOBAL.dock.config");
            } else {
                return Path.Combine(ProfileService.PROFILEFOLDER, $"{profileId}.dock.config");
            }
            
        }

        public async Task<bool> InitializeAvalonDockLayout(object o) {
            lock (lockObj) {
                if (Initialized && _dockloaded) { return true; }
            }

            while (!Initialized) {
                await Task.Delay(100);
            };

            await _dispatcher.BeginInvoke(new Action(() => {
                lock (lockObj) {
                    if (!_dockloaded) {
                        _dockmanager = (AvalonDock.DockingManager)o;

                        var profileId = profileService.ActiveProfile.Id;
                        var profilePath = GetDockConfigPath(profileId);
                        if (pendingDockLayout != null) {
                            var layout = pendingDockLayout;
                            pendingDockLayout = null;
                            try {
                                Logger.Info("Initializing imaging tab layout from a deferred sequencer instruction");
                                DeserializeAvalonDockLayout(layout);
                                SaveAvalonDockLayout();
                            } catch (Exception ex) {
                                Logger.Error("Failed to load deferred imaging tab layout. Loading default Layout!", ex);
                                LoadDefaultLayout();
                            }
                        } else if (File.Exists(profilePath)) {
                            try {
                                Logger.Info($"Initializing imaging tab layout from {profilePath}");
                                DeserializeAvalonDockLayout(File.ReadAllText(profilePath));
                            } catch (Exception ex) {
                                Logger.Error("Failed to load imaging tab layout. Loading default Layout!", ex);
                                LoadDefaultLayout();
                            }
                        } else if (!Properties.Settings.Default.SingleDockLayout && File.Exists(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "avalondock.config"))) {
                            try {
                                Logger.Info("Migrating imaging tab layout from old path");
                                DeserializeAvalonDockLayout(File.ReadAllText(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "avalondock.config")));
                            } catch (Exception ex) {
                                Logger.Error("Failed to load imaging tab layout. Loading default Layout!", ex);
                                LoadDefaultLayout();
                            }
                        } else {
                            LoadDefaultLayout();
                        }
                    }
                }
            }), DispatcherPriority.Normal);
            return true;
        }

        public async Task LoadImagingLayout(string filePath, CancellationToken token) {
            var layout = await File.ReadAllTextAsync(filePath, token);
            ValidateAvalonDockLayout(layout);
            token.ThrowIfCancellationRequested();

            await _dispatcher.InvokeAsync(() => {
                lock (lockObj) {
                    if (_dockmanager == null) {
                        pendingDockLayout = layout;
                        return;
                    }

                    var currentLayout = SerializeAvalonDockLayout();
                    try {
                        DeserializeAvalonDockLayout(layout);
                        SaveAvalonDockLayout();
                    } catch {
                        try {
                            DeserializeAvalonDockLayout(currentLayout);
                            SaveAvalonDockLayout();
                        } catch (Exception rollbackException) {
                            _dockloaded = false;
                            Logger.Error("Failed to restore the previous imaging tab layout after a load failure", rollbackException);
                        }
                        throw;
                    }
                }
            }, DispatcherPriority.Normal, token);
        }

        private static void ValidateAvalonDockLayout(string layout) {
            var document = XDocument.Parse(layout);
            if (document.Root?.Name.LocalName != "LayoutRoot") {
                throw new InvalidDataException("The selected file does not contain an AvalonDock LayoutRoot.");
            }
        }

        private string SerializeAvalonDockLayout() {
            var serializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
            using (var writer = new StringWriter()) {
                serializer.Serialize(writer);
                return writer.ToString();
            }
        }

        private void DeserializeAvalonDockLayout(string layout) {
            foreach (var item in Anchorables) {
                item.IsVisible = false;
            }

            var serializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
            var retryRequired = false;
            var dupeCheck = new HashSet<string>();
            serializer.LayoutSerializationCallback += (s, args) => {
                if (args?.Model == null) {
                    args.Cancel = true;
                    return;
                }

                if (!dupeCheck.Add(args.Model.ContentId)) {
                    Logger.Trace($"Duplicate entry detected for content id: {args.Model.ContentId}");
                    args.Cancel = true;
                    return;
                }

                if (args.Content == null) {
                    var context = Anchorables.FirstOrDefault(x => x.ContentId == args.Model.ContentId);
                    if (context == null) {
                        Logger.Debug($"Content not found for content id: {args.Model.ContentId}");
                        args.Cancel = true;
                    } else {
                        Logger.Trace($"Manually setting content for id: {args.Model.ContentId}");
                        args.Content = context;
                        retryRequired = true;
                    }
                }
            };

            using (var reader = new StringReader(layout)) {
                serializer.Deserialize(reader);
            }

            if (retryRequired) {
                Logger.Debug("The dock did not succeed to load. Trying again");
                foreach (var item in Anchorables) {
                    item.IsVisible = false;
                }

                dupeCheck.Clear();
                var retrySerializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                retrySerializer.LayoutSerializationCallback += (s, args) => {
                    if (args?.Model == null) {
                        args.Cancel = true;
                        return;
                    }

                    if (!dupeCheck.Add(args.Model.ContentId)) {
                        Logger.Trace($"Duplicate entry detected for content id: {args.Model.ContentId}");
                        args.Cancel = true;
                        return;
                    }

                    if (args.Content is IDockableVM dockable) {
                        dockable.IsVisible = true;
                        args.Content = dockable;
                    }
                };

                using (var reader = new StringReader(layout)) {
                    retrySerializer.Deserialize(reader);
                }
            }

            _dockloaded = true;
        }

        private void LoadDefaultLayout() {
            DeserializeAvalonDockLayout(Properties.Resources.avalondock);
        }

        public void SaveAvalonDockLayout() {
            lock (lockObj) {
                if (_dockloaded) {
                    var serializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);

                    var profileId = profileService.ActiveProfile.Id;
                    var profilePath = GetDockConfigPath(profileId);
                    serializer.Serialize(profilePath);
                }
            }
        }

        public IAsyncCommand LoadAvalonDockLayoutCommand { get; private set; }
        public ICommand ResetDockLayoutCommand { get; }
        public ICommand BackupDockLayoutCommand { get; private set; }
        public ICommand RestoreDockLayoutFromFileCommand { get; private set; }        
    }
}
