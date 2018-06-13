using NINA.Utility.Astrometry;

namespace NINA.Utility.Profile {
    public interface IAstrometrySettings {
        Epoch EpochType { get; set; }
        Hemisphere HemisphereType { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
    }
}