#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace NINA.Profile {

    internal partial class ProfileService : IProfileService {
        private static object lockobj = new object();

        public static string PROFILEFOLDER = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "Profiles");

        public ProfileService() {
            saveTimer = new System.Timers.Timer();
            saveTimer.Interval = 1000;
            saveTimer.AutoReset = false;
            saveTimer.Elapsed += SaveTimer_Elapsed;

            if (NINA.Properties.Settings.Default.UpdateSettings) {
                NINA.Properties.Settings.Default.Upgrade();
                NINA.Properties.Settings.Default.UpdateSettings = false;
                NINA.Properties.Settings.Default.Save();
            }

            Profiles = new AsyncObservableCollection<ProfileMeta>();

            Load();

            CreateWatcher();
        }

        private FileSystemWatcher profileFileWatcher;

        private void CreateWatcher() {
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

        private void Load() {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    if (!Directory.Exists(PROFILEFOLDER)) {
                        Directory.CreateDirectory(PROFILEFOLDER);
                    }

                    foreach (var file in Directory.GetFiles(PROFILEFOLDER, "*.profile")) {
                        var info = Profile.Peek(file);
                        if (info != null) {
                            Profiles.Add(info);
                        }
                    }
                    if (Profiles.Count == 0) {
                        if (File.Exists(OLDPROFILEFILEPATH)) {
                            MigrateOldProfile();
                        } else {
                            MigrateSettings();
                        }
                    }

                    var l = Profiles.OrderBy(x => x.LastUsed);

                    for (var idx = l.Count() - 1; idx >= 0; idx--) {
                        if (SelectProfile(l.ElementAt(idx))) {
                            return;
                        }
                    }

                    Logger.Debug("All Profiles are in use. Creating a new default profile");
                    var defaultProfile = AddDefaultProfile();
                    SelectProfile(defaultProfile);
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

        private bool saveProfiles = true;

        public void PauseSave() {
            saveProfiles = false;
        }

        public void ResumeSave() {
            saveProfiles = true;
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (saveProfiles && e.PropertyName == "Settings") {
                System.Threading.Tasks.Task.Run(() => TryScheduleSave());
            }
            if (e.PropertyName == nameof(IProfile.Name)) {
                Profiles.Where(x => ActiveProfile.Id == x.Id).First().Name = ActiveProfile.Name;
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

        public event EventHandler LocaleChanged;

        public void ChangeLocale(CultureInfo language) {
            ActiveProfile.ApplicationSettings.Language = language;

            System.Threading.Thread.CurrentThread.CurrentUICulture = language;
            System.Threading.Thread.CurrentThread.CurrentCulture = language;

            Locale.Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);
            LocaleChanged?.Invoke(this, null);
        }

        public void ChangeHemisphere(Hemisphere hemisphere) {
            ActiveProfile.AstrometrySettings.HemisphereType = hemisphere;
            LocationChanged?.Invoke(this, null);
        }

        public void ChangeLatitude(double latitude) {
            var hemisphereType = ActiveProfile.AstrometrySettings.HemisphereType;
            if ((hemisphereType == Hemisphere.SOUTHERN && latitude > 0) || (hemisphereType == Hemisphere.NORTHERN && latitude < 0)) {
                latitude = -latitude;
            }
            ActiveProfile.AstrometrySettings.Latitude = latitude;
            LocationChanged?.Invoke(this, null);
        }

        public void ChangeLongitude(double longitude) {
            ActiveProfile.AstrometrySettings.Longitude = longitude;
            LocationChanged?.Invoke(this, null);
        }

        public event EventHandler LocationChanged;

        public event EventHandler ProfileChanged;

        public AsyncObservableCollection<ProfileMeta> Profiles { get; set; }

        public void Add() {
            AddDefaultProfile();
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

                        var info = new ProfileMeta() { Id = clone.Id, Name = clone.Name, Location = clone.Location };
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

        public IProfile ActiveProfile { get; private set; }

        public bool SelectProfile(ProfileMeta info) {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    try {
                        var p = Profile.Load(info.Location);

                        UnregisterChangedEventHandlers();
                        if (ActiveProfile != null) {
                            ActiveProfile.Dispose();
                            Profiles.Where(x => x.Id == ActiveProfile.Id).First().IsActive = false;
                        }

                        ActiveProfile = p;
                        info.IsActive = true;

                        System.Threading.Thread.CurrentThread.CurrentUICulture = ActiveProfile.ApplicationSettings.Language;
                        System.Threading.Thread.CurrentThread.CurrentCulture = ActiveProfile.ApplicationSettings.Language;
                        Locale.Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);

                        LocaleChanged?.Invoke(this, null);
                        ProfileChanged?.Invoke(this, null);
                        LocationChanged?.Invoke(this, null);
                        RegisterChangedEventHandlers();
                    } catch (Exception ex) {
                        Logger.Debug(ex.Message + Environment.NewLine + ex.StackTrace);
                        return false;
                    }
                    return true;
                }
            }
        }

        private ProfileMeta AddDefaultProfile() {
            lock (lockobj) {
                if (profileFileWatcher != null) {
                    profileFileWatcher.EnableRaisingEvents = false;
                }

                var p = new Profile("Default");
                p.Save();

                var info = new ProfileMeta() { Id = p.Id, Name = p.Name, Location = p.Location };
                Profiles.Add(info);
                p.Dispose();

                if (profileFileWatcher != null) {
                    profileFileWatcher.EnableRaisingEvents = true;
                }

                return info;
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

        public static string OLDPROFILEFILEPATH = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "profiles.settings");

        /// <summary>
        /// Migrate old profile.settings into new separted profile files
        /// Last active profile will get its LastUsed date to DateTime.Now to be selected first.
        /// </summary>
        private void MigrateOldProfile() {
            var s = File.ReadAllText(OLDPROFILEFILEPATH);
            s = s.Replace("NINA.Utility.Profile", "NINA.Profile");
            var tmp = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "migration.profiles");
            File.WriteAllText(tmp, s);

            using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.Read)) {
                var serializer = new DataContractSerializer(typeof(Profiles));
                var obj = serializer.ReadObject(fs);
                var files = (Profiles)obj;

                foreach (Profile p in files.ProfileList) {
                    if (p.Id == files.ActiveProfileId) { p.LastUsed = DateTime.Now; }
                    p.Save();
                    var info = new ProfileMeta() { Id = p.Id, Name = p.Name, Location = p.Location, LastUsed = p.LastUsed };
                    p.Dispose();
                    Profiles.Add(info);
                }
            }
        }

        /// <summary>
        /// Migrates settings from old installations to the latest implementation
        /// </summary>
        private void MigrateSettings() {
            Object updateSettings = Properties.Settings.Default.GetPreviousVersion("UpdateSettings");
            if (updateSettings != null) {
                var p = new Profile("Migrated");

                p.ColorSchemaSettings.ColorSchemaName = Properties.Settings.Default.ColorSchemaType;
                p.ColorSchemaSettings.SecondaryColor = Properties.Settings.Default.SecondaryColor;
                p.ColorSchemaSettings.AltColorSchemaName = Properties.Settings.Default.AlternativeColorSchemaType;
                p.ColorSchemaSettings.AltBackgroundColor = Properties.Settings.Default.AltBackgroundColor;
                p.ColorSchemaSettings.AltBorderColor = Properties.Settings.Default.AltBorderColor;
                p.ColorSchemaSettings.AltButtonBackgroundColor = Properties.Settings.Default.AltButtonBackgroundColor;
                p.ColorSchemaSettings.AltButtonBackgroundSelectedColor = Properties.Settings.Default.AltButtonBackgroundSelectedColor;
                p.ColorSchemaSettings.AltButtonForegroundColor = Properties.Settings.Default.AltButtonForegroundColor;
                p.ColorSchemaSettings.AltButtonForegroundDisabledColor = Properties.Settings.Default.AltButtonForegroundDisabledColor;
                p.ColorSchemaSettings.AltNotificationErrorColor = Properties.Settings.Default.AltNotificationErrorColor;
                p.ColorSchemaSettings.AltNotificationWarningColor = Properties.Settings.Default.AltNotificationWarningColor;
                p.ColorSchemaSettings.AltNotificationErrorTextColor = Properties.Settings.Default.AltNotificationErrorTextColor;
                p.ColorSchemaSettings.AltNotificationWarningTextColor = Properties.Settings.Default.AltNotificationWarningTextColor;
                p.ColorSchemaSettings.AltPrimaryColor = Properties.Settings.Default.AltPrimaryColor;
                p.ColorSchemaSettings.AltSecondaryColor = Properties.Settings.Default.AltSecondaryColor;
                p.ColorSchemaSettings.BackgroundColor = Properties.Settings.Default.BackgroundColor;
                p.ColorSchemaSettings.BorderColor = Properties.Settings.Default.BorderColor;
                p.ColorSchemaSettings.ButtonBackgroundColor = Properties.Settings.Default.ButtonBackgroundColor;
                p.ColorSchemaSettings.ButtonBackgroundSelectedColor = Properties.Settings.Default.ButtonBackgroundSelectedColor;
                p.ColorSchemaSettings.ButtonForegroundColor = Properties.Settings.Default.ButtonForegroundColor;
                p.ColorSchemaSettings.ButtonForegroundDisabledColor = Properties.Settings.Default.ButtonForegroundDisabledColor;
                p.ColorSchemaSettings.NotificationErrorColor = Properties.Settings.Default.NotificationErrorColor;
                p.ColorSchemaSettings.NotificationWarningColor = Properties.Settings.Default.NotificationWarningColor;
                p.ColorSchemaSettings.NotificationErrorTextColor = Properties.Settings.Default.NotificationErrorTextColor;
                p.ColorSchemaSettings.NotificationWarningTextColor = Properties.Settings.Default.NotificationWarningTextColor;
                p.ColorSchemaSettings.PrimaryColor = Properties.Settings.Default.PrimaryColor;

                p.ApplicationSettings.Language = Properties.Settings.Default.Language;
                p.ApplicationSettings.LogLevel = (LogLevelEnum)Properties.Settings.Default.LogLevel;
                p.ApplicationSettings.DevicePollingInterval = Properties.Settings.Default.DevicePollingInterval;
                p.ApplicationSettings.SkyAtlasImageRepository = Properties.Settings.Default.SkyAtlasImageRepository;

                p.AstrometrySettings.EpochType = (Epoch)Properties.Settings.Default.EpochType;
                p.AstrometrySettings.HemisphereType = (Hemisphere)Properties.Settings.Default.HemisphereType;
                p.AstrometrySettings.Latitude = Properties.Settings.Default.Latitude;
                p.AstrometrySettings.Longitude = Properties.Settings.Default.Longitude;

                p.CameraSettings.BulbMode = (CameraBulbModeEnum)Properties.Settings.Default.CameraBulbMode;
                p.CameraSettings.Id = Properties.Settings.Default.CameraId;
                p.CameraSettings.PixelSize = Properties.Settings.Default.CameraPixelSize;
                p.CameraSettings.SerialPort = Properties.Settings.Default.CameraSerialPort;

                p.FilterWheelSettings.FilterWheelFilters = Properties.Settings.Default.FilterWheelFilters;
                p.FilterWheelSettings.Id = Properties.Settings.Default.FilterWheelId;

                p.FocuserSettings.AutoFocusExposureTime = Properties.Settings.Default.FocuserAutoFocusExposureTime;
                p.FocuserSettings.AutoFocusInitialOffsetSteps = Properties.Settings.Default.FocuserAutoFocusInitialOffsetSteps;
                p.FocuserSettings.AutoFocusStepSize = Properties.Settings.Default.FocuserAutoFocusStepSize;
                p.FocuserSettings.FocuserSettleTime = Properties.Settings.Default.FocuserSettleTime;
                p.FocuserSettings.Id = Properties.Settings.Default.FocuserId;
                p.FocuserSettings.UseFilterWheelOffsets = Properties.Settings.Default.FocuserUseFilterWheelOffsets;

                p.FramingAssistantSettings.CameraHeight = Properties.Settings.Default.FramingAssistantCameraHeight;
                p.FramingAssistantSettings.CameraWidth = Properties.Settings.Default.FramingAssistantCameraWidth;
                p.FramingAssistantSettings.FieldOfView = Properties.Settings.Default.FramingAssistantFieldOfView;

                p.GuiderSettings.PHD2ServerPort = Properties.Settings.Default.PHD2ServerPort;
                p.GuiderSettings.PHD2ServerUrl = Properties.Settings.Default.PHD2ServerUrl;
                p.GuiderSettings.DitherPixels = Properties.Settings.Default.DitherPixels;
                p.GuiderSettings.DitherRAOnly = Properties.Settings.Default.DitherRAOnly;
                p.GuiderSettings.SettleTime = Properties.Settings.Default.GuiderSettleTime;

                p.ImageSettings.AnnotateImage = Properties.Settings.Default.AnnotateImage;
                p.ImageSettings.AutoStretchFactor = Properties.Settings.Default.AutoStretchFactor;
                p.ImageSettings.HistogramResolution = Properties.Settings.Default.HistogramResolution;

                p.ImageFileSettings.FilePath = Properties.Settings.Default.ImageFilePath;
                p.ImageFileSettings.FilePattern = Properties.Settings.Default.ImageFilePattern;
                p.ImageFileSettings.FileType = (FileTypeEnum)Properties.Settings.Default.FileType;

                p.MeridianFlipSettings.Enabled = Properties.Settings.Default.AutoMeridianFlip;
                p.MeridianFlipSettings.SettleTime = Properties.Settings.Default.MeridianFlipSettleTime;
                p.MeridianFlipSettings.MinutesAfterMeridian = Properties.Settings.Default.MinutesAfterMeridian;
                p.MeridianFlipSettings.PauseTimeBeforeMeridian = Properties.Settings.Default.PauseTimeBeforeMeridian;
                p.MeridianFlipSettings.Recenter = Properties.Settings.Default.RecenterAfterFlip;

                p.PlateSolveSettings.SearchRadius = Properties.Settings.Default.AnsvrSearchRadius;
                p.PlateSolveSettings.AstrometryAPIKey = Properties.Settings.Default.AstrometryAPIKey;
                p.PlateSolveSettings.PS2Location = Properties.Settings.Default.PS2Location;
                p.PlateSolveSettings.Regions = Properties.Settings.Default.PS2Regions;
                p.PlateSolveSettings.PlateSolverType = (PlateSolverEnum)Properties.Settings.Default.PlateSolverType;
                p.PlateSolveSettings.BlindSolverType = (BlindSolverEnum)Properties.Settings.Default.BlindSolverType;
                p.PlateSolveSettings.CygwinLocation = Properties.Settings.Default.CygwinLocation;

                p.PolarAlignmentSettings.AltitudeDeclination = Properties.Settings.Default.AltitudeDeclination;
                p.PolarAlignmentSettings.AltitudeMeridianOffset = Properties.Settings.Default.AltitudeMeridianOffset;
                p.PolarAlignmentSettings.AzimuthDeclination = Properties.Settings.Default.AzimuthDeclination;
                p.PolarAlignmentSettings.AzimuthMeridianOffset = Properties.Settings.Default.AzimuthMeridianOffset;

                p.SequenceSettings.TemplatePath = Properties.Settings.Default.SequenceTemplatePath;
                p.SequenceSettings.EstimatedDownloadTime = Properties.Settings.Default.EstimatedDownloadTime;

                p.TelescopeSettings.Name = Properties.Settings.Default.TelescopeName;
                p.TelescopeSettings.FocalLength = Properties.Settings.Default.TelescopeFocalLength;
                p.TelescopeSettings.FocalRatio = Properties.Settings.Default.TelescopeFocalRatio;
                p.TelescopeSettings.Id = Properties.Settings.Default.TelescopeId;
                p.TelescopeSettings.SnapPortStart = Properties.Settings.Default.TelescopeSnapPortStart;
                p.TelescopeSettings.SnapPortStop = Properties.Settings.Default.TelescopeSnapPortStop;

                p.WeatherDataSettings.WeatherDataType = (WeatherDataEnum)Properties.Settings.Default.WeatherDataType;
                p.WeatherDataSettings.OpenWeatherMapAPIKey = Properties.Settings.Default.OpenWeatherMapAPIKey;
                p.WeatherDataSettings.OpenWeatherMapUrl = Properties.Settings.Default.OpenWeatherMapUrl;

                p.Save();
                p.Dispose();
                var info = new ProfileMeta() { Id = p.Id, Name = p.Name, Location = p.Location };
                Profiles.Add(info);
            } else {
                AddDefaultProfile();
            }
        }

        #endregion Migration
    }
}