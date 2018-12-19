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

using NINA.Model.MyFilterWheel;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

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
    public class Profile : BaseINPC, IProfile {

        private const int PROFILESAVE_DELAY = 1000;

        public Profile() {
            Initialize();
        }

        private void Initialize() {
            if (RotatorSettings == null) {
                RotatorSettings = new RotatorSettings();
            }

            ApplicationSettings.PropertyChanged += SettingsChanged;
            AstrometrySettings.PropertyChanged += SettingsChanged;
            CameraSettings.PropertyChanged += SettingsChanged;
            ColorSchemaSettings.PropertyChanged += SettingsChanged;
            FilterWheelSettings.PropertyChanged += SettingsChanged;
            FocuserSettings.PropertyChanged += SettingsChanged;
            FramingAssistantSettings.PropertyChanged += SettingsChanged;
            GuiderSettings.PropertyChanged += SettingsChanged;
            ImageFileSettings.PropertyChanged += SettingsChanged;
            ImageSettings.PropertyChanged += SettingsChanged;
            MeridianFlipSettings.PropertyChanged += SettingsChanged;
            PlateSolveSettings.PropertyChanged += SettingsChanged;
            PolarAlignmentSettings.PropertyChanged += SettingsChanged;
            RotatorSettings.PropertyChanged += SettingsChanged;
            SequenceSettings.PropertyChanged += SettingsChanged;
            TelescopeSettings.PropertyChanged += SettingsChanged;
            WeatherDataSettings.PropertyChanged += SettingsChanged;
            if (FlatWizardSettings == null) {
                FlatWizardSettings = new FlatWizardSettings();
            }
            FlatWizardSettings.PropertyChanged += SettingsChanged;
        }

        [OnDeserialized]
        private void SetValuesOnDeserialized(StreamingContext context) {
            Initialize();
        }

        private void SettingsChanged(object sender, PropertyChangedEventArgs e) {
            DelayedPropertyChanged("Settings", TimeSpan.FromMilliseconds(PROFILESAVE_DELAY));
        }

        public Profile(string name) : this() {
            this.Name = name;
        }

        /// <summary>
        /// Called by the profile manager after deserializing, so the filter info object reference is
        /// matching with the fw list again There should be a better solution to this...
        /// </summary>
        public void MatchFilterSettingsWithFilterList() {
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

        [DataMember]
        public string Name { get; set; } = "Default";

        private bool isActive;

        public bool IsActive {
            get {
                return isActive;
            }
            set {
                isActive = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public IApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings();

        [DataMember]
        public IAstrometrySettings AstrometrySettings { get; set; } = new AstrometrySettings();

        [DataMember]
        public ICameraSettings CameraSettings { get; set; } = new CameraSettings();

        [DataMember]
        public IColorSchemaSettings ColorSchemaSettings { get; set; } = new ColorSchemaSettings();

        [DataMember]
        public IFilterWheelSettings FilterWheelSettings { get; set; } = new FilterWheelSettings();

        [DataMember]
        public IFocuserSettings FocuserSettings { get; set; } = new FocuserSettings();

        [DataMember]
        public IFramingAssistantSettings FramingAssistantSettings { get; set; } = new FramingAssistantSettings();

        [DataMember]
        public IGuiderSettings GuiderSettings { get; set; } = new GuiderSettings();

        [DataMember]
        public IImageFileSettings ImageFileSettings { get; set; } = new ImageFileSettings();

        [DataMember]
        public IImageSettings ImageSettings { get; set; } = new ImageSettings();

        [DataMember]
        public IMeridianFlipSettings MeridianFlipSettings { get; set; } = new MeridianFlipSettings();

        [DataMember]
        public IPlateSolveSettings PlateSolveSettings { get; set; } = new PlateSolveSettings();

        [DataMember]
        public IPolarAlignmentSettings PolarAlignmentSettings { get; set; } = new PolarAlignmentSettings();

        [DataMember]
        public IRotatorSettings RotatorSettings { get; set; } = new RotatorSettings();

        [DataMember]
        public ISequenceSettings SequenceSettings { get; set; } = new SequenceSettings();

        [DataMember]
        public ITelescopeSettings TelescopeSettings { get; set; } = new TelescopeSettings();

        [DataMember]
        public IWeatherDataSettings WeatherDataSettings { get; set; } = new WeatherDataSettings();

        [DataMember]
        public IFlatWizardSettings FlatWizardSettings { get; set; }
    }
}