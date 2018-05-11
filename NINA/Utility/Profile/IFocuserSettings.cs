namespace NINA.Utility.Profile {
    public interface IFocuserSettings {
        int AutoFocusExposureTime { get; set; }
        int AutoFocusInitialOffsetSteps { get; set; }
        int AutoFocusStepSize { get; set; }
        string Id { get; set; }
        bool UseFilterWheelOffsets { get; set; }
    }
}