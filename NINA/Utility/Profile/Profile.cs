using NINA.Model.MyFilterWheel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(Profile))]
    public class Profile : BaseINPC {
        public Profile() {
            this.name = "Default";
        }

        public Profile(string name) : this() {
            this.name = name;
        }
        
        /// <summary>
        /// Called by the profile manager after deserializing, so the filter info object reference is matching with the fw list again
        /// There should be a better solution to this...
        /// </summary>
        public void MatchFilterSettingsWithFilterList() {
            if(this.PlateSolveSettings.Filter != null) { 
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

        private Guid id = Guid.NewGuid();
        [XmlElement(nameof(Id))]
        public Guid Id {
            get {
                return id;
            }
            set {
                id = value;
            }
        }

        private string name;
        [XmlElement(nameof(Name))]
        public string Name {
            get {
                return name;
            }
            set {
                name = value;
            }
        }


        private bool isActive;
        [XmlIgnore()]
        public bool IsActive {
            get {
                return isActive;
            }
            set {
                isActive = value;
                RaisePropertyChanged();
            }
        }

        private ApplicationSettings applicationSettings = new ApplicationSettings();
        [XmlElement(nameof(ApplicationSettings))]
        public ApplicationSettings ApplicationSettings {
            get {
                return applicationSettings;
            }
            set {
                applicationSettings = value;
            }
        }

        private AstrometrySettings astrometrySettings = new AstrometrySettings();
        [XmlElement(nameof(AstrometrySettings))]
        public AstrometrySettings AstrometrySettings {
            get {
                return astrometrySettings;
            }
            set {
                astrometrySettings = value;
            }
        }

        private CameraSettings cameraSettings = new CameraSettings();
        [XmlElement(nameof(CameraSettings))]
        public CameraSettings CameraSettings {
            get {
                return cameraSettings;
            }
            set {
                cameraSettings = value;
            }
        }

        private ColorSchemaSettings colorSchemaSettings = new ColorSchemaSettings();
        [XmlElement(nameof(ColorSchemaSettings))]
        public ColorSchemaSettings ColorSchemaSettings {
            get {
                return colorSchemaSettings;
            }
            set {
                colorSchemaSettings = value;
            }
        }

        private FilterWheelSettings filterWheelSettings = new FilterWheelSettings();
        [XmlElement(nameof(FilterWheelSettings))]
        public FilterWheelSettings FilterWheelSettings {
            get {
                return filterWheelSettings;
            }
            set {
                filterWheelSettings = value;
            }
        }

        private FocuserSettings focuserSettings = new FocuserSettings();
        [XmlElement(nameof(FocuserSettings))]
        public FocuserSettings FocuserSettings {
            get {
                return focuserSettings;
            }
            set {
                focuserSettings = value;
            }
        }

        private FramingAssistantSettings framingAssistantSettings = new FramingAssistantSettings();
        [XmlElement(nameof(FramingAssistantSettings))]
        public FramingAssistantSettings FramingAssistantSettings {
            get {
                return framingAssistantSettings;
            }
            set {
                framingAssistantSettings = value;
            }
        }

        private GuiderSettings guiderSettings = new GuiderSettings();
        [XmlElement(nameof(GuiderSettings))]
        public GuiderSettings GuiderSettings {
            get {
                return guiderSettings;
            }
            set {
                guiderSettings = value;
            }
        }

        private ImageFileSettings imageFileSettings = new ImageFileSettings();
        [XmlElement(nameof(ImageFileSettings))]
        public ImageFileSettings ImageFileSettings {
            get {
                return imageFileSettings;
            }
            set {
                imageFileSettings = value;
            }
        }

        private ImageSettings imageSettings = new ImageSettings();
        [XmlElement(nameof(ImageSettings))]
        public ImageSettings ImageSettings {
            get {
                return imageSettings;
            }
            set {
                imageSettings = value;
            }
        }

        private MeridianFlipSettings meridianFlipSettings = new MeridianFlipSettings();
        [XmlElement(nameof(MeridianFlipSettings))]
        public MeridianFlipSettings MeridianFlipSettings {
            get {
                return meridianFlipSettings;
            }
            set {
                meridianFlipSettings = value;
            }
        }

        private PlateSolveSettings plateSolveSettings = new PlateSolveSettings();
        [XmlElement(nameof(PlateSolveSettings))]
        public PlateSolveSettings PlateSolveSettings {
            get {
                return plateSolveSettings;
            }
            set {
                plateSolveSettings = value;
            }
        }

        private PolarAlignmentSettings polarAlignmentSettings = new PolarAlignmentSettings();
        [XmlElement(nameof(PolarAlignmentSettings))]
        public PolarAlignmentSettings PolarAlignmentSettings {
            get {
                return polarAlignmentSettings;
            }
            set {
                polarAlignmentSettings = value;
            }
        }

        private SequenceSettings sequenceSettings = new SequenceSettings();
        [XmlElement(nameof(SequenceSettings))]
        public SequenceSettings SequenceSettings {
            get {
                return sequenceSettings;
            }
            set {
                sequenceSettings = value;
            }
        }

        private TelescopeSettings telescopeSettings = new TelescopeSettings();
        [XmlElement(nameof(TelescopeSettings))]
        public TelescopeSettings TelescopeSettings {
            get {
                return telescopeSettings;
            }
            set {
                telescopeSettings = value;
            }
        }

        private WeatherDataSettings weatherDataSettings = new WeatherDataSettings();
        [XmlElement(nameof(WeatherDataSettings))]
        public WeatherDataSettings WeatherDataSettings {
            get {
                return weatherDataSettings;
            }
            set {
                weatherDataSettings = value;
            }
        }
    }
}
