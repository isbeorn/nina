using NINA.Model.ImageData;
using NINA.Model.MyCamera;

namespace NINA.ViewModel.FlatWizard {

    public interface IFlatWizardExposureTimeFinderService {

        FlatWizardExposureAduState GetFlatExposureState(IImageData imageData, double exposureTime, FlatWizardFilterSettingsWrapper wrapper);

        FlatWizardExposureTimeState GetNextFlatExposureState(double exposureTime, FlatWizardFilterSettingsWrapper wrapper);

        System.Threading.Tasks.Task<FlatWizardUserPromptVMResponse> EvaluateUserPromptResultAsync(IImageData imageData, double exposureTime, string message, FlatWizardFilterSettingsWrapper wrapper);

        double GetExpectedExposureTime(FlatWizardFilterSettingsWrapper wrapper);

        double GetNextExposureTime(double exposureTime, FlatWizardFilterSettingsWrapper wrapper);

        void AddDataPoint(double exposureTime, double mean);

        void ClearDataPoints();
    }

    public enum FlatWizardExposureAduState {
        ExposureFinished,
        ExposureAduAboveMean,
        ExposureAduBelowMean
    }

    public enum FlatWizardExposureTimeState {
        ExposureTimeWithinBounds,
        ExposureTimeAboveMaxTime,
        ExposureTimeBelowMinTime
    }
}