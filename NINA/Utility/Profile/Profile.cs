using NINA.Model.MyFilterWheel;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

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
    [KnownType(typeof(SequenceSettings))]
    [KnownType(typeof(TelescopeSettings))]
    [KnownType(typeof(WeatherDataSettings))]
    public class Profile : BaseINPC, IProfile {

        public Profile() {
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
                DataContractSerializer xmlS = new DataContractSerializer(typeof(Profile));
                xmlS.WriteObject(stream, profileToClone);
                stream.Position = 0;
                var newProfile = (Profile)xmlS.ReadObject(stream);
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
        public ISequenceSettings SequenceSettings { get; set; } = new SequenceSettings();

        [DataMember]
        public ITelescopeSettings TelescopeSettings { get; set; } = new TelescopeSettings();

        [DataMember]
        public IWeatherDataSettings WeatherDataSettings { get; set; } = new WeatherDataSettings();
    }
}