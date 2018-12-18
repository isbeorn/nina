using NINA.Model.MyCamera;
using System.Threading;

namespace NINA.ViewModel.FlatWizard {
    internal interface IFlatWizardExposureTimeFinderService {
        FlatWizardExposureAduState GetFlatExposureState(ImageArray imageArray, double exposureTime, FlatWizardFilterSettingsWrapper wrapper);

        FlatWizardExposureTimeState GetNextFlatExposureState(double exposureTime, FlatWizardFilterSettingsWrapper wrapper);

        FlatWizardUserPromptVMResponse EvaluateUserPromptResult(ImageArray imageArray, double exposureTime, string message, FlatWizardFilterSettingsWrapper wrapper);

        double GetExpectedExposureTime(FlatWizardFilterSettingsWrapper wrapper);

        double GetNextExposureTime(double exposureTime, FlatWizardFilterSettingsWrapper wrapper);
        void ClearDataPoints();
    }

    enum FlatWizardExposureAduState {
        ExposureFinished,
        ExposureAduAboveMean,
        ExposureAduBelowMean
    }

    enum FlatWizardExposureTimeState {
        ExposureTimeWithinBounds,
        ExposureTimeAboveMaxTime,
        ExposureTimeBelowMinTime
    }
}