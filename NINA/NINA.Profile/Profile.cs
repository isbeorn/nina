#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using NINA.Core.Model.Equipment;
using System.Text;
using NINA.Core.Locale;
using System.Xml;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    [KnownType(typeof(ApplicationSettings))]
    [KnownType(typeof(AstrometrySettings))]
    [KnownType(typeof(CameraSettings))]
    [KnownType(typeof(ColorSchemaSettings))]
    [KnownType(typeof(DomeSettings))]
    [KnownType(typeof(FilterWheelSettings))]
    [KnownType(typeof(FocuserSettings))]
    [KnownType(typeof(FramingAssistantSettings))]
    [KnownType(typeof(GuiderSettings))]
    [KnownType(typeof(ImageFileSettings))]
    [KnownType(typeof(ImageSettings))]
    [KnownType(typeof(MeridianFlipSettings))]
    [KnownType(typeof(PlateSolveSettings))]
    [KnownType(typeof(RotatorSettings))]
    [KnownType(typeof(FlatDeviceSettings))]
    [KnownType(typeof(SequenceSettings))]
    [KnownType(typeof(TelescopeSettings))]
    [KnownType(typeof(WeatherDataSettings))]
    [KnownType(typeof(FlatWizardSettings))]
    [KnownType(typeof(PlanetariumSettings))]
    [KnownType(typeof(SwitchSettings))]
    [KnownType(typeof(SnapShotControlSettings))]
    [KnownType(typeof(SafetyMonitorSettings))]
    [KnownType(typeof(PluginSettings))]
    [KnownType(typeof(GnssSettings))]
    [KnownType(typeof(DockPanelSettings))]
    [KnownType(typeof(AlpacaSettings))]
    [KnownType(typeof(ImageHistorySettings))]
    public class Profile : SerializableINPC, IProfile {

        /// <summary>
        /// Exclusive locked filestream to read and write the profile
        /// </summary>
        private FileStream fs;

        public Profile() {
            SetDefaultValues();
            RegisterHandlers();
        }

        public Profile(string name) : this() {
            this.Name = name;
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        private void SetValuesOnDeserialized(StreamingContext context) {
            RegisterHandlers();
        }

        /// <summary>
        /// Setting default values prior to deserialization
        /// </summary>
        private void SetDefaultValues() {
            this.Description = string.Empty;
            ApplicationSettings = new ApplicationSettings();
            AstrometrySettings = new AstrometrySettings();
            CameraSettings = new CameraSettings();
            ColorSchemaSettings = new ColorSchemaSettings();
            DomeSettings = new DomeSettings();
            FilterWheelSettings = new FilterWheelSettings();
            FlatWizardSettings = new FlatWizardSettings();
            FocuserSettings = new FocuserSettings();
            FramingAssistantSettings = new FramingAssistantSettings();
            GuiderSettings = new GuiderSettings();
            ImageFileSettings = new ImageFileSettings();
            ImageSettings = new ImageSettings();
            MeridianFlipSettings = new MeridianFlipSettings();
            PlanetariumSettings = new PlanetariumSettings();
            PlateSolveSettings = new PlateSolveSettings();
            RotatorSettings = new RotatorSettings();
            FlatDeviceSettings = new FlatDeviceSettings();
            SequenceSettings = new SequenceSettings();
            SwitchSettings = new SwitchSettings();
            TelescopeSettings = new TelescopeSettings();
            WeatherDataSettings = new WeatherDataSettings();
            SnapShotControlSettings = new SnapShotControlSettings();
            SafetyMonitorSettings = new SafetyMonitorSettings();
            PluginSettings = new PluginSettings();
            GnssSettings = new GnssSettings();
            DockPanelSettings = new DockPanelSettings();
            AlpacaSettings = new AlpacaSettings();
            ImageHistorySettings = new ImageHistorySettings();
        }

        /// <summary>
        /// Register propertychanged handlers after deserialization is complete
        /// </summary>
        private void RegisterHandlers() {
            ApplicationSettings.PropertyChanged += SettingsChanged;
            AstrometrySettings.PropertyChanged += SettingsChanged;
            CameraSettings.PropertyChanged += SettingsChanged;
            ColorSchemaSettings.PropertyChanged += SettingsChanged;
            DomeSettings.PropertyChanged += SettingsChanged;
            FilterWheelSettings.PropertyChanged += SettingsChanged;
            FlatWizardSettings.PropertyChanged += SettingsChanged;
            FocuserSettings.PropertyChanged += SettingsChanged;
            FramingAssistantSettings.PropertyChanged += SettingsChanged;
            GuiderSettings.PropertyChanged += SettingsChanged;
            ImageFileSettings.PropertyChanged += SettingsChanged;
            ImageSettings.PropertyChanged += SettingsChanged;
            MeridianFlipSettings.PropertyChanged += SettingsChanged;
            PlanetariumSettings.PropertyChanged += SettingsChanged;
            PlateSolveSettings.PropertyChanged += SettingsChanged;
            RotatorSettings.PropertyChanged += SettingsChanged;
            FlatDeviceSettings.PropertyChanged += SettingsChanged;
            SequenceSettings.PropertyChanged += SettingsChanged;
            SwitchSettings.PropertyChanged += SettingsChanged;
            TelescopeSettings.PropertyChanged += SettingsChanged;
            WeatherDataSettings.PropertyChanged += SettingsChanged;
            SnapShotControlSettings.PropertyChanged += SettingsChanged;
            SafetyMonitorSettings.PropertyChanged += SettingsChanged;
            PluginSettings.PropertyChanged += SettingsChanged;
            GnssSettings.PropertyChanged += SettingsChanged;
            DockPanelSettings.PropertyChanged += SettingsChanged;
            AlpacaSettings.PropertyChanged += SettingsChanged;
            ImageHistorySettings.PropertyChanged += SettingsChanged;
        }

        /// <summary>
        /// Call Propertychanged using the name "Settings" to notify the profile manager that a save should be triggered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged("Settings");
        }

        /// <summary>
        /// Called by the load after deserializing, so the filter info object reference is
        /// matching with the fw list again
        /// </summary>
        private void MatchFilterSettingsWithFilterList() {
            if (this.PlateSolveSettings.Filter != null) {
                this.PlateSolveSettings.Filter = GetFilterFromList(this.PlateSolveSettings.Filter);
            }
        }

        private FilterInfo GetFilterFromList(FilterInfo filterToMatch) {
            var filter = this.FilterWheelSettings.FilterWheelFilters.Where((f) => f.Name == filterToMatch.Name).FirstOrDefault();
            if (filter == null) {
                filter = this.FilterWheelSettings.FilterWheelFilters.Where((f) => f.Position == filterToMatch.Position).FirstOrDefault();
                if (filter == null) {
                }
            }
            return filter;
        }

        [DataMember]
        public Guid Id { get; set; } = Guid.NewGuid();

        [DataMember]
        public DateTime LastUsed { get; set; } = DateTime.MinValue;

        private string name = Loc.Instance["LblDefault"];

        [DataMember]
        public string Name {
            get => name;
            set {
                if (name != value) {
                    name = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("Settings");
                }
            }
        }

        private string description = string.Empty;

        [DataMember]
        public string Description {
            get => description;
            set {
                if (description != value) {
                    description = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("Settings");
                }
            }
        }

        [IgnoreDataMember]
        public string Location {
            get => Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Profiles", $"{Id}.profile");
            protected set { }
        }

        [DataMember]
        public IApplicationSettings ApplicationSettings { get; set; }

        [DataMember]
        public IAstrometrySettings AstrometrySettings { get; set; }

        [DataMember]
        public ICameraSettings CameraSettings { get; set; }

        [DataMember]
        public IColorSchemaSettings ColorSchemaSettings { get; set; }

        [DataMember]
        public IDomeSettings DomeSettings { get; set; }

        [DataMember]
        public IFilterWheelSettings FilterWheelSettings { get; set; }

        [DataMember]
        public IFocuserSettings FocuserSettings { get; set; }

        [DataMember]
        public IFramingAssistantSettings FramingAssistantSettings { get; set; }

        [DataMember]
        public IGuiderSettings GuiderSettings { get; set; }

        [DataMember]
        public IImageFileSettings ImageFileSettings { get; set; }

        [DataMember]
        public IImageSettings ImageSettings { get; set; }

        [DataMember]
        public IMeridianFlipSettings MeridianFlipSettings { get; set; }

        [DataMember]
        public IPlateSolveSettings PlateSolveSettings { get; set; }

        [DataMember]
        public IRotatorSettings RotatorSettings { get; set; }

        [DataMember]
        public IFlatDeviceSettings FlatDeviceSettings { get; set; }

        [DataMember]
        public ISequenceSettings SequenceSettings { get; set; }

        [DataMember]
        public ISwitchSettings SwitchSettings { get; set; }

        [DataMember]
        public ITelescopeSettings TelescopeSettings { get; set; }

        [DataMember]
        public IWeatherDataSettings WeatherDataSettings { get; set; }

        [DataMember]
        public IFlatWizardSettings FlatWizardSettings { get; set; }

        [DataMember]
        public IPlanetariumSettings PlanetariumSettings { get; set; }

        [DataMember]
        public ISnapShotControlSettings SnapShotControlSettings { get; set; }

        [DataMember]
        public ISafetyMonitorSettings SafetyMonitorSettings { get; set; }

        [DataMember]
        public IPluginSettings PluginSettings { get; set; }

        [DataMember]
        public IGnssSettings GnssSettings { get; set; }

        [DataMember]
        public IDockPanelSettings DockPanelSettings { get; set; }

        [DataMember]
        public IAlpacaSettings AlpacaSettings { get; set; }

        [DataMember]
        public IImageHistorySettings ImageHistorySettings { get; set; }

        /// <summary>
        /// Deep Clone an existing profile, create a new Id and append "Copy" to the name
        /// </summary>
        /// <param name="profileToClone"></param>
        /// <returns>Deeply cloned profile</returns>
        public static IProfile Clone(IProfile profileToClone) {
            using (MemoryStream stream = new MemoryStream()) {
                DataContractSerializer dcs = new DataContractSerializer(typeof(Profile));
                dcs.WriteObject(stream, profileToClone);
                stream.Position = 0;
                var newProfile = (Profile)dcs.ReadObject(stream);
                newProfile.Name = newProfile.Name + " Copy";
                newProfile.Id = Guid.NewGuid();
                return newProfile;
            }
        }

        /// <summary>
        /// Read profile file for the given path with a write lock
        /// Deserialize the profile into a new Profile instance
        /// Set LastUsed time for profile to Now and save it
        /// </summary>
        /// <param name="path">path to profile to load</param>
        /// <returns>loaded Profile instance</returns>
        public static IProfile Load(string path) {
            using (MyStopWatch.Measure()) {
                var journal = path + ".journal";
                var backup = path + ".bkp";

                static IProfile LoadProfile(string filePath) {
                    FileStream fs = null;
                    try {
                        fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                        var serializer = new DataContractSerializer(typeof(Profile));
                        var obj = serializer.ReadObject(fs);

                        var p = (Profile)obj;
                        p.MatchFilterSettingsWithFilterList();
                        p.fs = fs;

                        p.LastUsed = DateTime.Now;
                        p.Save();

                        return p;
                    } catch (Exception) {
                        fs?.Dispose();
                        throw;
                    }
                }

                try {
                    return LoadProfile(path);
                } catch (IOException ex) when (IsFileInUse(ex)) {
                    throw;
                } catch (Exception ex) {
                    Logger.Error($"Profile failed to load at {path} ", ex);

                    // Try to restore profile from journal file first
                    if (File.Exists(journal)) {
                        try {
                            Logger.Info($"Restoring profile from journal {journal}");
                            File.Move(journal, path, true);
                            return LoadProfile(path);
                        } catch (Exception journalEx) {
                            Logger.Error("Profile restore from journal failed", journalEx);
                        }
                    }

                    // If journal file did not work, try to restore from backup
                    if (File.Exists(backup)) {
                        try {
                            Logger.Info($"Restoring profile from backup {backup}");
                            File.Copy(backup, path, true);
                            return LoadProfile(path);
                        } catch (Exception backupEx) {
                            Logger.Error("Profile restore from backup failed", backupEx);
                        }
                    }

                    throw;
                }
            }
        }

        private static bool IsFileInUse(IOException ex) {
            const int ERROR_SHARING_VIOLATION = 32;
            const int ERROR_LOCK_VIOLATION = 33;

            int hresult = ex.HResult & 0xFFFF;

            return hresult == ERROR_SHARING_VIOLATION || hresult == ERROR_LOCK_VIOLATION;
        }

        /// <summary>
        /// Save via deserialization of the current profile state
        /// </summary>
        public void Save() {
            using (MyStopWatch.Measure()) {
                var journal = Location + ".journal";
                var backup = Location + ".bkp";
                try {
                    // Create or reset file stream
                    if (fs == null) {
                        // When profile is in memory only yet
                        fs = new FileStream(Location, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                    } else {
                        fs.Position = 0;
                    }

                    // save profile to journal file
                    using (var journalFs = new FileStream(journal, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        var serializer = new DataContractSerializer(typeof(Profile));
                        serializer.WriteObject(journalFs, this);
                        journalFs.Flush(true);
                    }

                    // make a backup of original file
                    if (fs?.Length > 0) {
                        fs.Flush(true);
                        File.Copy(Location, backup, true);
                    }

                    // release file lock and move journal to actual file
                    fs?.Close();
                    fs?.Dispose();
                    File.Replace(journal, Location, backup);

                    // re-open file for lock
                    fs = new FileStream(Location, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                } catch (Exception ex) {
                    Logger.Error(ex);

                    // On failure delete the journal
                    if (File.Exists(journal)) {
                        try { File.Delete(journal); } catch { }
                    }

                    // Re-open the file lock in case of failure
                    if (fs == null || !fs.CanWrite) {
                        fs = new FileStream(Location, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    }
                }

            }
        }

        public void Dispose() {
            fs?.Dispose();
        }

        /// <summary>
        /// Peek into the profile file and return meta info
        /// </summary>
        /// <param name="path">path to the profile</param>
        /// <returns>Meta Info of the profile</returns>
        public static ProfileMeta Peek(string path) {
            using (MyStopWatch.Measure()) {
                var journal = path + ".journal";
                var backup = path + ".bkp";

                static ProfileMeta LoadProfileMeta(string filePath) {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        var serializer = new DataContractSerializer(typeof(ProfileMetaProxy), new DataContractSerializerSettings {
                            RootName = new XmlDictionary().Add("Profile"),
                            RootNamespace = new XmlDictionary().Add("http://schemas.datacontract.org/2004/07/NINA.Profile")
                        });
                        var obj = serializer.ReadObject(fs);
                        var p = (ProfileMetaProxy)obj;
                        return new ProfileMeta() {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description,
                            Location = filePath,
                            LastUsed = p.LastUsed
                        };
                    }
                }

                try {
                    return LoadProfileMeta(path);
                } catch (Exception ex) {
                    Logger.Error($"Profile failed to load at {path} ", ex);

                    // Try to restore profile from journal file first
                    if (File.Exists(journal)) {
                        try {
                            Logger.Info($"Restoring profile from journal {journal}");
                            File.Move(journal, path, true);
                            return LoadProfileMeta(path);
                        } catch (Exception journalEx) {
                            Logger.Error("Profile restore from journal failed", journalEx);
                        }
                    }

                    // If journal file did not work, try to restore from backup
                    if (File.Exists(backup)) {
                        try {
                            Logger.Info($"Restoring profile from backup {backup}");
                            File.Copy(backup, path, true);
                            return LoadProfileMeta(path);
                        } catch (Exception backupEx) {
                            Logger.Error("Profile restore from backup failed", backupEx);
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Delete a profile for the given location
        /// </summary>
        /// <param name="info">Profile meta info containing the profile location to delete</param>
        /// <returns>1: success; 0: failed - most likely profile is in use</returns>
        public static bool Remove(ProfileMeta info) {
            using (MyStopWatch.Measure()) {
                try {
                    File.Delete(info.Location);
                } catch (Exception ex) {
                    Logger.Debug(ex.Message + Environment.NewLine + ex.StackTrace);
                    return false;
                }
                return true;
            }
        }
    }
}