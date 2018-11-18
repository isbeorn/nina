using NINA.Utility.Enum;

namespace NINA.Utility.Profile {

    public interface IGuiderSettings : ISettings {
        double DitherPixels { get; set; }
        bool DitherRAOnly { get; set; }
        GuiderScaleEnum PHD2GuiderScale { get; set; }
        int PHD2HistorySize { get; set; }
        int PHD2ServerPort { get; set; }
        string PHD2ServerUrl { get; set; }
        int SettleTime { get; set; }
        double SettlePixels { get; set; }
        int SettleTimeout { get; set; }
    }
}