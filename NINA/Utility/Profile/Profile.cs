using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(Profile))]
    class Profile {
        public Profile(string id) {

        }

        private void Save() {

        }

        private string id;
        [XmlElement(nameof(Id))]
        public string Id {
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

        private ApplicationSettings applicationSettings;
        [XmlElement(nameof(ApplicationSettings))]
        public ApplicationSettings ApplicationSettings {
            get {
                return applicationSettings;
            }
            set {
                applicationSettings = value;
            }
        }

        private AstrometrySettings astrometrySettings;
        [XmlElement(nameof(AstrometrySettings))]
        public AstrometrySettings AstrometrySettings {
            get {
                return astrometrySettings;
            }
            set {
                astrometrySettings = value;
            }
        }

        private CameraSettings cameraSettings;
        [XmlElement(nameof(CameraSettings))]
        public CameraSettings CameraSettings {
            get {
                return cameraSettings;
            }
            set {
                cameraSettings = value;
            }
        }

        private ColorSchemaSettings colorSchemaSettings;
        [XmlElement(nameof(ColorSchemaSettings))]
        public ColorSchemaSettings ColorSchemaSettings {
            get {
                return colorSchemaSettings;
            }
            set {
                colorSchemaSettings = value;
            }
        }

        private FilterWheelSettings filterWheelSettings;
        [XmlElement(nameof(FilterWheelSettings))]
        public FilterWheelSettings FilterWheelSettings {
            get {
                return filterWheelSettings;
            }
            set {
                filterWheelSettings = value;
            }
        }

        private FocuserSettings focuserSettings;
        [XmlElement(nameof(FocuserSettings))]
        public FocuserSettings FocuserSettings {
            get {
                return focuserSettings;
            }
            set {
                focuserSettings = value;
            }
        }

        private FramingAssistantSettings framingAssistantSettings;
        [XmlElement(nameof(FramingAssistantSettings))]
        public FramingAssistantSettings FramingAssistantSettings {
            get {
                return framingAssistantSettings;
            }
            set {
                framingAssistantSettings = value;
            }
        }

        private GuiderSettings guiderSettings;
        [XmlElement(nameof(GuiderSettings))]
        public GuiderSettings GuiderSettings {
            get {
                return guiderSettings;
            }
            set {
                guiderSettings = value;
            }
        }

        private ImageFileSettings imageFileSettings;
        [XmlElement(nameof(ImageFileSettings))]
        public ImageFileSettings ImageFileSettings {
            get {
                return imageFileSettings;
            }
            set {
                imageFileSettings = value;
            }
        }

        private ImageSettings imageSettings;
        [XmlElement(nameof(ImageSettings))]
        public ImageSettings ImageSettings {
            get {
                return imageSettings;
            }
            set {
                imageSettings = value;
            }
        }

        private MeridianFlipSettings meridianFlipSettings;
        [XmlElement(nameof(MeridianFlipSettings))]
        public MeridianFlipSettings MeridianFlipSettings {
            get {
                return meridianFlipSettings;
            }
            set {
                meridianFlipSettings = value;
            }
        }

        private PlateSolveSettings plateSolveSettings;
        [XmlElement(nameof(PlateSolveSettings))]
        public PlateSolveSettings PlateSolveSettings {
            get {
                return plateSolveSettings;
            }
            set {
                plateSolveSettings = value;
            }
        }

        private PolarAlignmentSettings polarAlignmentSettings;
        [XmlElement(nameof(PolarAlignmentSettings))]
        public PolarAlignmentSettings PolarAlignmentSettings {
            get {
                return polarAlignmentSettings;
            }
            set {
                polarAlignmentSettings = value;
            }
        }

        private SequenceSettings sequenceSettings;
        [XmlElement(nameof(SequenceSettings))]
        public SequenceSettings SequenceSettings {
            get {
                return sequenceSettings;
            }
            set {
                sequenceSettings = value;
            }
        }

        private TelescopeSettings telescopeSettings;
        [XmlElement(nameof(TelescopeSettings))]
        public TelescopeSettings TelescopeSettings {
            get {
                return telescopeSettings;
            }
            set {
                telescopeSettings = value;
            }
        }

        private WeatherDataSettings weatherDataSettings;
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
