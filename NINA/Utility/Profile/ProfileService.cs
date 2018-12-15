#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    internal class ProfileService : IProfileService {

        public ProfileService() {
            if (NINA.Properties.Settings.Default.UpdateSettings) {
                NINA.Properties.Settings.Default.Upgrade();
                NINA.Properties.Settings.Default.UpdateSettings = false;
                NINA.Properties.Settings.Default.Save();
            }
            Load();
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "Settings") {
                Save();
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

        public static string PROFILEFILEPATH = Path.Combine(Utility.APPLICATIONTEMPPATH, "profiles.settings");
        public static string PROFILETEMPFILEPATH = Path.Combine(Utility.APPLICATIONTEMPPATH, "profiles.settings.bkp");

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

        public Profiles Profiles { get; set; }

        public void Add() {
            Add(new Profile("Profile" + (Profiles.ProfileList.Count + 1)));
        }

        private void Add(IProfile p) {
            Profiles.Add(p);
            Save();
        }

        public void Clone(Guid id) {
            var p = Profiles.ProfileList.Where((x) => x.Id == id).FirstOrDefault();
            if (p != null) {
                var newProfile = Profile.Clone(p);
                Add(newProfile);
            }
        }

        private static object lockobj = new object();

        private void Save() {
            try {
                lock (lockobj) {
                    var tries = 0;
                    const int maxTries = 10;
                    while (true) {
                        try {
                            Debug.Print("Trying to get file read info");
                            using (var fs = new FileStream(PROFILEFILEPATH, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
                                var serializer = new DataContractSerializer(typeof(Profiles));
                                Debug.Print("Reading file");
                                Profiles profileToWrite = Profiles;
                                if (fs.Length > 0) {
                                    /* Copy file to temp file */
                                    using (var copyStream = new FileStream(PROFILETEMPFILEPATH, FileMode.Create, FileAccess.Write)) {
                                        fs.CopyTo(copyStream);
                                        //Reset filestream position
                                        fs.Position = 0;
                                    }

                                    /* Read profiles from file, replace current profile in file with actual profile */
                                    var obj = serializer.ReadObject(fs);
                                    profileToWrite = (Profiles)obj;

                                    var idx = -1;
                                    for (var i = 0; i < profileToWrite.ProfileList.Count; i++) {
                                        if (Profiles.ActiveProfileId == profileToWrite.ProfileList[i].Id) {
                                            idx = i;
                                            break;
                                        }
                                    }
                                    if (idx >= 0) {
                                        profileToWrite.ProfileList.RemoveAt(idx);
                                        profileToWrite.ProfileList.Insert(idx, Profiles.ActiveProfile);
                                    }

                                    profileToWrite.ActiveProfileId = Profiles.ActiveProfileId;

                                    var excludedIDs = new HashSet<Guid>(profileToWrite.ProfileList.Select(p => p.Id));
                                    var profilesToAdd = this.Profiles.ProfileList.Where(x => !excludedIDs.Contains(x.Id));

                                    foreach (var p in profilesToAdd) {
                                        profileToWrite.Add(p);
                                    }
                                }

                                //Reset filestream content and position
                                fs.Position = 0;
                                fs.SetLength(0);
                                serializer = new DataContractSerializer(typeof(Profiles));
                                Debug.Print("Writing file");
                                serializer.WriteObject(fs, profileToWrite);

                                //Delete Temp file
                                File.Delete(PROFILETEMPFILEPATH);

                                break;
                            }
                        } catch (IOException ex) {
                            if (tries >= maxTries) {
                                Logger.Error(ex);
                                Notification.Notification.ShowError(ex.Message);
                                break;
                            }
                        }

                        System.Threading.Thread.Sleep(200);
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError(ex.Message);

                if (File.Exists(PROFILETEMPFILEPATH)) {
                    //Restore temp file
                    File.Copy(PROFILETEMPFILEPATH, PROFILEFILEPATH, true);
                }
            }
        }

        public IProfile ActiveProfile {
            get {
                return Profiles?.ActiveProfile;
            }
        }

        private void Load() {
            if (File.Exists(PROFILETEMPFILEPATH)) {
                File.Copy(PROFILETEMPFILEPATH, PROFILEFILEPATH, true);
            }

            if (File.Exists(PROFILEFILEPATH)) {
                try {
                    UnregisterChangedEventHandlers();
                    var serializer = new DataContractSerializer(typeof(Profiles));

                    using (FileStream reader = new FileStream(PROFILEFILEPATH, FileMode.Open)) {
                        var obj = serializer.ReadObject(reader);

                        Profiles = (Profiles)obj;
                        foreach (Profile p in Profiles.ProfileList) {
                            p.MatchFilterSettingsWithFilterList();
                        }
                        Profiles.SelectActiveProfile();

                        Locale.Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);
                        LocaleChanged?.Invoke(this, null);
                        ProfileChanged?.Invoke(this, null);
                        LocationChanged?.Invoke(this, null);

                        RegisterChangedEventHandlers();
                    }
                } catch (UnauthorizedAccessException ex) {
                    Logger.Error(ex);
                    System.Windows.MessageBox.Show("Unable to open profile file. " + ex.Message);
                    System.Windows.Application.Current.Shutdown();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    System.Windows.MessageBox.Show("Unable to load profile file. Please restart the application \n" + ex.Message);
                    System.Windows.Application.Current.Shutdown();
                }
            } else {
                MigrateSettings();
            }
        }

        public void SelectProfile(Guid guid) {
            UnregisterChangedEventHandlers();
            Profiles.SelectProfile(guid);
            Save();
            Locale.Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);
            LocaleChanged?.Invoke(this, null);
            ProfileChanged?.Invoke(this, null);
            LocationChanged?.Invoke(this, null);
            RegisterChangedEventHandlers();
        }

        private void LoadDefaultProfile() {
            Profiles = new Profiles();
            Profiles.Add(new Profile("Default"));
            SelectProfile(Profiles.ProfileList[0].Id);
        }

        public void RemoveProfile(Guid id) {
            if (id != ActiveProfile.Id) {
                var p = Profiles.ProfileList.Where((x) => x.Id == id).FirstOrDefault();
                if (p != null) {
                    Profiles.ProfileList.Remove(p);
                    Save();
                }
            }
        }

        public IEnumerable<IProfile> GetProfiles() {
            return Profiles.ProfileList;
        }

        private void MigrateSettings() {
            Profiles = new Profiles();
            Object updateSettings = Properties.Settings.Default.GetPreviousVersion("UpdateSettings");
            if (updateSettings != null) {
                var p = new Profile("Migrated");
                Profiles.Add(p);

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
                p.ApplicationSettings.DatabaseLocation = Properties.Settings.Default.DatabaseLocation;
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

                p.TelescopeSettings.FocalLength = Properties.Settings.Default.TelescopeFocalLength;
                p.TelescopeSettings.Id = Properties.Settings.Default.TelescopeId;
                p.TelescopeSettings.SnapPortStart = Properties.Settings.Default.TelescopeSnapPortStart;
                p.TelescopeSettings.SnapPortStop = Properties.Settings.Default.TelescopeSnapPortStop;

                p.WeatherDataSettings.WeatherDataType = (WeatherDataEnum)Properties.Settings.Default.WeatherDataType;
                p.WeatherDataSettings.OpenWeatherMapAPIKey = Properties.Settings.Default.OpenWeatherMapAPIKey;
                p.WeatherDataSettings.OpenWeatherMapUrl = Properties.Settings.Default.OpenWeatherMapUrl;

                SelectProfile(p.Id);
            } else {
                LoadDefaultProfile();
            }
        }
    }
}