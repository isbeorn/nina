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
using NINA.Core.Locale;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using NINA.Core.Utility.Notification;
using NINA.Core.Model;
using NINA.Profile.Interfaces;

namespace NINA.Profile {

    public class ProfileService : BaseINPC, IProfileService {
        private static object lockobj = new object();

        public static string PROFILEFOLDER = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Profiles");

        public ProfileService() {
            saveTimer = new System.Timers.Timer();
            saveTimer.Interval = 1000;
            saveTimer.AutoReset = false;
            saveTimer.Elapsed += SaveTimer_Elapsed;

            Profiles = new AsyncObservableCollection<ProfileMeta>();
        }

        private FileSystemWatcher profileFileWatcher;

        public void CreateWatcher() {
            profileFileWatcher?.Dispose();

            profileFileWatcher = new FileSystemWatcher() {
                Path = PROFILEFOLDER,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.profile",
                EnableRaisingEvents = false
            };

            profileFileWatcher.Created += ProfileFileWatcher_Created;
            profileFileWatcher.Deleted += ProfileFileWatcher_Deleted;

            profileFileWatcher.EnableRaisingEvents = true;
        }

        private void ProfileFileWatcher_Deleted(object sender, FileSystemEventArgs e) {
            lock (lockobj) {
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(e.Name), out var id)) {
                    var toDelete = Profiles.Where(x => x.Id == id).FirstOrDefault();
                    if (toDelete != null) {
                        Profiles.Remove(toDelete);
                    }
                }
            }
        }

        private void ProfileFileWatcher_Created(object sender, FileSystemEventArgs e) {
            lock (lockobj) {
                ProfileMeta info = null;
                var retries = 0;
                do {
                    info = Profile.Peek(Path.Combine(PROFILEFOLDER, e.Name));
                    if (info == null) {
                        Thread.Sleep(TimeSpan.FromMilliseconds(500));
                        retries++;
                    }
                } while (retries < 3 && info == null);

                if (info != null) {
                    Profiles.Add(info);
                } else {
                    var id = Guid.Parse(Path.GetFileNameWithoutExtension(e.Name));
                    Profiles.Add(new ProfileMeta() { Id = id, Location = e.FullPath, LastUsed = DateTime.MinValue, IsActive = false, Name = "UNKOWN" });
                }
            }
        }

        /// <summary>
        /// Timer that will trigger a save after 200ms
        /// When another profile change happens during that time, the duration is reset
        /// This way something like a slider will not spam the harddisk with save operations
        /// </summary>
        private System.Timers.Timer saveTimer;

        /// <summary>
        /// Stop the timer and save the profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            Save();
        }

        public bool TryLoad(string startWithProfileId) {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    if (!Directory.Exists(PROFILEFOLDER)) {
                        Directory.CreateDirectory(PROFILEFOLDER);
                    }

                    //This will migrate the namespaces of existing profiles when NINA was not split into multiple smaller projects
                    MigrateModularizedSolutionNamespaceChange();

                    var loadSpecificProfile = !string.IsNullOrWhiteSpace(startWithProfileId);
                    ProfileWasSpecifiedFromCommandLineArgs = loadSpecificProfile;
                    Directory
                        .GetFiles(PROFILEFOLDER, "*.profile")
                        .Select(Profile.Peek)
                        .Where(p => p != null)
                        .OrderByDescending(x => x.LastUsed)
                        .ToList()
                        .ForEach(Profiles.Add);

                    if (!Profiles.Any() && !loadSpecificProfile) {
                        AddDefaultProfile(Loc.Instance["LblDefault"]);
                    }

                    var selectedProfile =
                        Profiles
                        .Where(p => !loadSpecificProfile || p.Id.ToString() == startWithProfileId)
                        .SkipWhile(p => !SelectProfile(p))
                        .FirstOrDefault();
                    if (selectedProfile != null) {
                        return true;
                    }
                    if (loadSpecificProfile) {
                        return false;
                    }

                    Logger.Debug("All Profiles are in use. Creating a new profile with defaults");
                    var defaultProfile = AddDefaultProfile($"{Loc.Instance["LblDefault"]}-{DateTime.Now:s}");
                    SelectProfile(defaultProfile);
                    return true;
                }
            }
        }

        private void Save() {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    try {
                        ActiveProfile.Save();
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Stop the timer and restart it again
        /// </summary>
        private void TryScheduleSave() {
            if (Monitor.TryEnter(lockobj, 1000)) {
                try {
                    saveTimer.Stop();
                    saveTimer.Start();
                } finally {
                    Monitor.Exit(lockobj);
                }
            }
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "Settings") {
                System.Threading.Tasks.Task.Run(() => TryScheduleSave());
            }
            if (e.PropertyName == nameof(IProfile.Name)) {
                Profiles.Where(x => ActiveProfile.Id == x.Id).First().Name = ActiveProfile.Name;
            }
            if (e.PropertyName == nameof(IProfile.Description)) {
                Profiles.Where(x => ActiveProfile.Id == x.Id).First().Description = ActiveProfile.Description;
            }
        }

        private void RegisterChangedEventHandlers() {
            this.ActiveProfile.PropertyChanged += SettingsChanged;
        }

        private void UnregisterChangedEventHandlers() {
            if (this.ActiveProfile != null) {
                this.ActiveProfile.PropertyChanged -= SettingsChanged;
            }
        }

        public bool ProfileWasSpecifiedFromCommandLineArgs { get; private set; }

        public event EventHandler LocaleChanged;
        
        public void ChangeLocale(CultureInfo language) {
            ActiveProfile.ApplicationSettings.Language = language;

            CultureInfo.DefaultThreadCurrentUICulture = language;
            Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);
            var eventHandler = LocaleChanged;
            if (eventHandler != null) { 
                Application.Current.Dispatcher?.Invoke(eventHandler, this, null);
            }
        }

        public void ChangeLatitude(double latitude) {
            ActiveProfile.AstrometrySettings.Latitude = latitude;
            var eventHandler = LocationChanged;
            if (eventHandler != null) {
                Application.Current.Dispatcher?.Invoke(eventHandler, this, null);
            }
        }

        public void ChangeLongitude(double longitude) {
            ActiveProfile.AstrometrySettings.Longitude = longitude;
            var eventHandler = LocationChanged;
            if (eventHandler != null) {
                Application.Current.Dispatcher?.Invoke(LocationChanged, this, null);
            }
        }

        public void ChangeElevation(double elevation) {
            ActiveProfile.AstrometrySettings.Elevation = elevation;
            var eventHandler = LocationChanged;
            if (eventHandler != null) {
                Application.Current.Dispatcher?.Invoke(eventHandler, this, null);
            }
        }

        public void ChangeHorizon(string horizonFilePath) {
            ActiveProfile.AstrometrySettings.HorizonFilePath = horizonFilePath;

            try {
                if (!string.IsNullOrWhiteSpace(horizonFilePath)) {
                    ActiveProfile.AstrometrySettings.Horizon = CustomHorizon.FromFilePath(horizonFilePath);
                } else {
                    ActiveProfile.AstrometrySettings.HorizonFilePath = string.Empty;
                    ActiveProfile.AstrometrySettings.Horizon = null;
                }
            } catch (Exception ex) {
                ActiveProfile.AstrometrySettings.HorizonFilePath = string.Empty;
                ActiveProfile.AstrometrySettings.Horizon = null;
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblFailedToLoadCustomHorizon"] + ex.Message);
            }

            var eventHandler = HorizonChanged;
            if (eventHandler != null) {
                Application.Current.Dispatcher?.Invoke(eventHandler, this, null);
            }
        }

        public event EventHandler LocationChanged;
        public event EventHandler BeforeProfileChanging;

        public event EventHandler ProfileChanged;

        public event EventHandler HorizonChanged;

        public AsyncObservableCollection<ProfileMeta> Profiles { get; }

        public void Add() {
            AddDefaultProfile($"{Loc.Instance["LblDefault"]}-{DateTime.Now:s}");
        }

        public bool Clone(ProfileMeta profileInfo) {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    if (profileFileWatcher != null) {
                        profileFileWatcher.EnableRaisingEvents = false;
                    }

                    IProfile clone = null;
                    if (profileInfo.Id == ActiveProfile.Id) {
                        clone = Profile.Clone(ActiveProfile);
                    } else {
                        try {
                            var p = Profile.Load(profileInfo.Location);
                            clone = Profile.Clone(p);
                            p.Dispose();
                        } catch (Exception) {
                            //Profile is in use
                            return false;
                        }
                    }

                    if (clone != null) {
                        clone.Save();

                        var info = new ProfileMeta() { Id = clone.Id, Name = clone.Name, Location = clone.Location, Description = clone.Description };
                        Profiles.Add(info);
                        clone.Dispose();

                        if (profileFileWatcher != null) {
                            profileFileWatcher.EnableRaisingEvents = true;
                        }
                    }
                    return true;
                }
            }
        }

        private IProfile activeProfile;

        public IProfile ActiveProfile {
            get => activeProfile;
            private set {
                activeProfile = value;
                Application.Current.Resources["ActiveProfile"] = activeProfile;
                RaisePropertyChanged();
            }
        }

        public bool SelectProfile(ProfileMeta info) {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    try {
                        var eventHandlerBeforeProfileChanging = BeforeProfileChanging;
                        if (eventHandlerBeforeProfileChanging != null) {
                            Application.Current.Dispatcher?.Invoke(eventHandlerBeforeProfileChanging, this, new EventArgs());

                        }
                        var old = activeProfile;
                        var p = Profile.Load(info.Location);

                        UnregisterChangedEventHandlers();
                        if (ActiveProfile != null) {
                            ActiveProfile.Dispose();
                            Profiles.Where(x => x.Id == ActiveProfile.Id).First().IsActive = false;
                        }

                        ActiveProfile = p;
                        info.IsActive = true;

                        System.Threading.Thread.CurrentThread.CurrentUICulture = ActiveProfile.ApplicationSettings.Language;
                        Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);

                        var eventHandlerProfile = ProfileChanged;
                        if (eventHandlerProfile != null) {
                            Application.Current.Dispatcher?.Invoke(eventHandlerProfile, this, new ProfileChangedEventArgs(old, ActiveProfile));

                        }
                        var eventHandlerLocale = LocaleChanged;
                        if (eventHandlerLocale != null) {
                            Application.Current.Dispatcher?.Invoke(eventHandlerLocale, this, null);
                        }
                        var eventHandlerLocation = LocationChanged;
                        if (eventHandlerLocation != null) {
                            Application.Current.Dispatcher?.Invoke(eventHandlerLocation, this, null);
                        }
                        RegisterChangedEventHandlers();
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        return false;
                    }
                    return true;
                }
            }
        }

        public void Release() {
            lock (lockobj) {
                if (ActiveProfile != null) {
                    try {
                        ActiveProfile.Dispose();
                    } catch (Exception) { }
                }
            }
        }

        private ProfileMeta AddDefaultProfile(string name) {
            lock (lockobj) {
                if (profileFileWatcher != null) {
                    profileFileWatcher.EnableRaisingEvents = false;
                }

                using (var p = new Profile(name)) {
                    p.Save();

                    var info = new ProfileMeta() { Id = p.Id, Name = p.Name, Location = p.Location };
                    Profiles.Add(info);

                    if (profileFileWatcher != null) {
                        profileFileWatcher.EnableRaisingEvents = true;
                    }

                    return info;
                }
            }
        }

        public bool RemoveProfile(ProfileMeta info) {
            lock (lockobj) {
                if (!Profile.Remove(info)) {
                    return false;
                } else {
                    Profiles.Remove(info);
                    return true;
                }
            }
        }

        #region Migration

        private void MigrateModularizedSolutionNamespaceChange() {
            foreach (var profileFile in Directory.GetFiles(PROFILEFOLDER)) {
                try {
                    var profile = File.ReadAllText(profileFile);
                    if (profile.Contains("http://schemas.datacontract.org/2004/07/NINA.Utility")) {
                        Logger.Info($"Migrating profile {profileFile}");
                        profile = profile.Replace("http://schemas.datacontract.org/2004/07/NINA.Model.MyFilterWheel", "http://schemas.datacontract.org/2004/07/NINA.Core.Model.Equipment")
                             .Replace("http://schemas.datacontract.org/2004/07/NINA.Model.MyCamera", "http://schemas.datacontract.org/2004/07/NINA.Core.Model.Equipment")
                             .Replace("http://schemas.datacontract.org/2004/07/NINA.Utility", "http://schemas.datacontract.org/2004/07/NINA.Core.Utility.ColorSchema");

                        var backupfolder = PROFILEFOLDER + "_old";
                        if (!Directory.Exists(backupfolder)) {
                            Directory.CreateDirectory(backupfolder);
                        }

                        // Backup old profile
                        var backupDestination = Path.Combine(backupfolder, Path.GetFileName(profileFile));
                        if (!File.Exists(backupDestination)) {
                            try {
                                File.Move(profileFile, backupDestination);
                            } catch (Exception ex) {
                                Logger.Error(ex);
                            }
                        }

                        // Save adjusted profile
                        File.WriteAllText(profileFile, profile);
                    }
                } catch (Exception ex) {
                    Logger.Error($"Failed to migrate profile {profileFile} due to ", ex);
                }
            }
        }

        #endregion Migration

        public static void ActivateInstanceOfNinaReferencingProfile(string startWithProfileId) {
            using (var waitHandle = new EventWaitHandle(false,

                EventResetMode.ManualReset,

                "NINA_ActivateInstance:" + startWithProfileId)) {
                waitHandle.Set();
            }
        }

        public static System.Threading.Tasks.Task ActivateInstanceWatcher(
            IProfileService profileService,
            Window mainWindow
            ) {
            var currentProfile = profileService.ActiveProfile.Id.ToString();
            return System.Threading.Tasks.Task.Factory.StartNew(
                () => {
                    var profileChanged = new ManualResetEventSlim();
                    profileService.ProfileChanged += (x, y) => profileChanged.Set();

                    var activated = new EventWaitHandle(false,
                        EventResetMode.AutoReset,
                        "NINA_ActivateInstance:" + currentProfile);

                    while (true) {
                        var handles = new WaitHandle[] {
                            profileChanged.WaitHandle,
                            activated };
                        var response = WaitHandle.WaitAny(handles, TimeSpan.FromSeconds(1));
                        if (258 == response) // timeout
                        {
                            continue;
                        }

                        if (handles[response] == profileChanged.WaitHandle) {
                            profileChanged.Reset();
                            activated.Dispose();
                            handles[1] = activated = new EventWaitHandle(false,
                                EventResetMode.AutoReset,
                                "NINA_ActivateInstance:" +
                                    profileService.ActiveProfile.Id.ToString());
                        }
                        if (handles[response] == activated) {
                            mainWindow.Dispatcher.Invoke(
                                    () => mainWindow.Activate());
                        }
                    }
                }, System.Threading.Tasks.TaskCreationOptions.LongRunning);
        }
    }

    public class ProfileChangedEventArgs : EventArgs {
        public ProfileChangedEventArgs(IProfile oldProfile, IProfile newProfile) {
            this.OldProfile = oldProfile;
            this.NewProfile = newProfile;
        }

        public IProfile OldProfile { get; }
        public IProfile NewProfile { get; }
    }
}