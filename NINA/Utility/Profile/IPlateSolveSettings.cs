using NINA.Model.MyFilterWheel;
using NINA.Utility.Enum;

namespace NINA.Utility.Profile {

    public interface IPlateSolveSettings : ISettings {
        string AstrometryAPIKey { get; set; }
        BlindSolverEnum BlindSolverType { get; set; }
        string CygwinLocation { get; set; }
        double ExposureTime { get; set; }
        FilterInfo Filter { get; set; }
        PlateSolverEnum PlateSolverType { get; set; }
        string PS2Location { get; set; }
        int Regions { get; set; }
        double SearchRadius { get; set; }
        double Threshold { get; set; }
    }
}