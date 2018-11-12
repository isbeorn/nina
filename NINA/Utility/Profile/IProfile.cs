using System;
using System.ComponentModel;

namespace NINA.Utility.Profile {

    public interface IProfile : INotifyPropertyChanged {
        IApplicationSettings ApplicationSettings { get; set; }
        IAstrometrySettings AstrometrySettings { get; set; }
        ICameraSettings CameraSettings { get; set; }
        IColorSchemaSettings ColorSchemaSettings { get; set; }
        IFilterWheelSettings FilterWheelSettings { get; set; }
        IFocuserSettings FocuserSettings { get; set; }
        IFramingAssistantSettings FramingAssistantSettings { get; set; }
        IFlatWizardSettings FlatWizardSettings { get; set; }
        IGuiderSettings GuiderSettings { get; set; }
        Guid Id { get; set; }
        IImageFileSettings ImageFileSettings { get; set; }
        IImageSettings ImageSettings { get; set; }
        bool IsActive { get; set; }
        IMeridianFlipSettings MeridianFlipSettings { get; set; }
        string Name { get; set; }
        IPlateSolveSettings PlateSolveSettings { get; set; }
        IPolarAlignmentSettings PolarAlignmentSettings { get; set; }
        IRotatorSettings RotatorSettings { get; set; }
        ISequenceSettings SequenceSettings { get; set; }
        ITelescopeSettings TelescopeSettings { get; set; }
        IWeatherDataSettings WeatherDataSettings { get; set; }

        void MatchFilterSettingsWithFilterList();
    }
}