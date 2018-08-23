namespace NINA.Utility.Profile {

    public interface IFramingAssistantSettings : ISettings {
        int CameraHeight { get; set; }
        int CameraWidth { get; set; }
        double FieldOfView { get; set; }
    }
}