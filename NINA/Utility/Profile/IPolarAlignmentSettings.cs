namespace NINA.Utility.Profile {
    public interface IPolarAlignmentSettings {
        double AltitudeDeclination { get; set; }
        double AltitudeMeridianOffset { get; set; }
        double AzimuthDeclination { get; set; }
        double AzimuthMeridianOffset { get; set; }
    }
}