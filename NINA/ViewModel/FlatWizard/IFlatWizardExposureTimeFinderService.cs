#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using System.Threading.Tasks;

namespace NINA.ViewModel.FlatWizard {

    public interface IFlatWizardExposureTimeFinderService {

        Task<FlatWizardExposureAduState> GetFlatExposureState(IImageData imageData, double exposureTime, FlatWizardFilterSettingsWrapper wrapper);

        FlatWizardExposureTimeState GetNextFlatExposureState(double exposureTime, FlatWizardFilterSettingsWrapper wrapper);

        Task<FlatWizardUserPromptVMResponse> EvaluateUserPromptResultAsync(IImageData imageData, double exposureTime, string message, FlatWizardFilterSettingsWrapper wrapper);

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