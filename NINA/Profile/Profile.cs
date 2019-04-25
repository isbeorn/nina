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

using NINA.Model.MyFilterWheel;
using NINA.Utility;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    [KnownType(typeof(ApplicationSettings))]
    [KnownType(typeof(AstrometrySettings))]
    [KnownType(typeof(CameraSettings))]
    [KnownType(typeof(ColorSchemaSettings))]
    [KnownType(typeof(FilterWheelSettings))]
    [KnownType(typeof(FocuserSettings))]
    [KnownType(typeof(FramingAssistantSettings))]
    [KnownType(typeof(GuiderSettings))]
    [KnownType(typeof(ImageFileSettings))]
    [KnownType(typeof(ImageSettings))]
    [KnownType(typeof(MeridianFlipSettings))]
    [KnownType(typeof(PlateSolveSettings))]
    [KnownType(typeof(PolarAlignmentSettings))]
    [KnownType(typeof(RotatorSettings))]
    [KnownType(typeof(SequenceSettings))]
    [KnownType(typeof(TelescopeSettings))]
    [KnownType(typeof(WeatherDataSettings))]
    [KnownType(typeof(FlatWizardSettings))]
    [KnownType(typeof(PlanetariumSettings))]
    public class Profile : BaseINPC, IProfile {

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
            ApplicationSettings = new ApplicationSettings();
            AstrometrySettings = new AstrometrySettings();
            CameraSettings = new CameraSettings();
            ColorSchemaSettings = new ColorSchemaSettings();
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
            PolarAlignmentSettings = new PolarAlignmentSettings();
            RotatorSettings = new RotatorSettings();
            SequenceSettings = new SequenceSettings();
            TelescopeSettings = new TelescopeSettings();
            WeatherDataSettings = new WeatherDataSettings();
        }

        /// <summary>
        /// Register propertychanged handlers after deserialization is complete
        /// </summary>
        private void RegisterHandlers() {
            ApplicationSettings.PropertyChanged += SettingsChanged;
            AstrometrySettings.PropertyChanged += SettingsChanged;
            CameraSettings.PropertyChanged += SettingsChanged;
            ColorSchemaSettings.PropertyChanged += SettingsChanged;
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
            PolarAlignmentSettings.PropertyChanged += SettingsChanged;
            RotatorSettings.PropertyChanged += SettingsChanged;
            SequenceSettings.PropertyChanged += SettingsChanged;
            TelescopeSettings.PropertyChanged += SettingsChanged;
            WeatherDataSettings.PropertyChanged += SettingsChanged;
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

        public Guid Id { get; set; } = Guid.NewGuid();[DataMember]

        public DateTime LastUsed { get; set; } = DateTime.MinValue;

        private string name = "Default";

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

        [IgnoreDataMember]
        public string Location {
            get {
                return Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "Profiles", $"{Id}.profile");
            }
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
        public IPolarAlignmentSettings PolarAlignmentSettings { get; set; }

        [DataMember]
        public IRotatorSettings RotatorSettings { get; set; }

        [DataMember]
        public ISequenceSettings SequenceSettings { get; set; }

        [DataMember]
        public ITelescopeSettings TelescopeSettings { get; set; }

        [DataMember]
        public IWeatherDataSettings WeatherDataSettings { get; set; }

        [DataMember]
        public IFlatWizardSettings FlatWizardSettings { get; set; }

        [DataMember]
        public IPlanetariumSettings PlanetariumSettings { get; set; }

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
                var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                var serializer = new DataContractSerializer(typeof(Profile));
                var obj = serializer.ReadObject(fs);
                var p = (Profile)obj;
                p.MatchFilterSettingsWithFilterList();
                p.fs = fs;

                p.LastUsed = DateTime.Now;
                p.Save();

                return p;
            }
        }

        /// <summary>
        /// Save via deserialization of the current profile state
        /// </summary>
        public void Save() {
            using (MyStopWatch.Measure()) {
                if (fs == null) {
                    // When profile is in memory only yet
                    fs = new FileStream(Location, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                } else {
                    fs.Position = 0;
                }
                fs.SetLength(0);
                var serializer = new DataContractSerializer(typeof(Profile));
                serializer.WriteObject(fs, this);
                fs.Flush();
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
                try {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        var serializer = new DataContractSerializer(typeof(Profile));
                        var obj = serializer.ReadObject(fs);
                        var p = (Profile)obj;
                        return new ProfileMeta() {
                            Id = p.Id,
                            Name = p.Name,
                            Location = p.Location,
                            LastUsed = p.LastUsed
                        };
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
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