using NINA.Model.MyCamera;

namespace NINA.Utility.Profile {

    public interface IFlatWizardSettings : ISettings {
        int FlatCount { get; set; }
        double HistogramMeanTarget { get; set; }
        double HistogramTolerance { get; set; }
        bool NoFlatProcessing { get; set; }
        double StepSize { get; set; }
        BinningMode BinningMode { get; set; }
    }
}