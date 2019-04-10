using NINA.Utility.Enum;

namespace NINA.Utility.Profile {

    public interface IPlanetariumSettings : ISettings {
        string StellariumHost { get; set; }
        int StellariumPort { get; set; }
        int StellariumTimeout { get; set; }
        string CdCHost { get; set; }
        int CdCPort { get; set; }
        int CdCTimeout { get; set; }
        PlanetariumEnum PreferredPlanetarium { get; set; }
    }
}