#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"


using System;
using System.Collections.Generic;
using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Utility.WindowService;
using OxyPlot;

namespace NINA.ViewModel.FlatWizard {
    internal class FlatWizardExposureTimeFinderService : IFlatWizardExposureTimeFinderService {
        private List<DataPoint> dataPoints = new List<DataPoint>();

        private IWindowService WindowService { get; set; } = new WindowService();

        public void ClearDataPoints() {
            dataPoints = new List<DataPoint>();
        }

        public FlatWizardUserPromptVMResponse EvaluateUserPromptResult(ImageArray imageArray, double exposureTime, string message, FlatWizardFilterSettingsWrapper wrapper) {
            var flatsWizardUserPrompt = new FlatWizardUserPromptVM(message,
                                                    imageArray.Statistics.Mean, CameraBitDepthToAdu(wrapper.CameraInfo.BitDepth), wrapper, exposureTime);
            WindowService.ShowDialog(flatsWizardUserPrompt, Loc.Instance["LblFlatUserPromptFailure"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow).Wait();

            if (flatsWizardUserPrompt.Reset) {
                ClearDataPoints();
            }

            return new FlatWizardUserPromptVMResponse() {
                Continue = flatsWizardUserPrompt.Continue,
                NextExposureTime = flatsWizardUserPrompt.Reset ? wrapper.Settings.MinFlatExposureTime : exposureTime
            };
        }

        public double GetExpectedExposureTime(FlatWizardFilterSettingsWrapper wrapper) {
            TrendLine trendLine = new TrendLine(dataPoints);

            return (wrapper.Settings.HistogramMeanTarget * CameraBitDepthToAdu(wrapper.CameraInfo.BitDepth) - trendLine.Offset) / trendLine.Slope;
        }

        public FlatWizardExposureAduState GetFlatExposureState(ImageArray imageArray, double exposureTime, FlatWizardFilterSettingsWrapper wrapper) {
            var histogramMeanAdu = wrapper.Settings.HistogramMeanTarget * CameraBitDepthToAdu(wrapper.CameraInfo.BitDepth);
            var histogramMeanAduTolerance = histogramMeanAdu * wrapper.Settings.HistogramTolerance;
            var histogramToleranceUpperBound = histogramMeanAdu + histogramMeanAduTolerance;
            var histogramToleranceLowerBound = histogramMeanAdu - histogramMeanAduTolerance;
            var currentMean = imageArray.Statistics.Mean;

            dataPoints.Add(new DataPoint(exposureTime, imageArray.Statistics.Mean));

            if (histogramToleranceLowerBound <= currentMean && histogramToleranceUpperBound >= currentMean) {
                return FlatWizardExposureAduState.ExposureFinished;
            } else if (currentMean > histogramMeanAdu + histogramMeanAduTolerance) {
                return FlatWizardExposureAduState.ExposureAduAboveMean;
            } else {
                return FlatWizardExposureAduState.ExposureAduBelowMean;
            }
        }

        public FlatWizardExposureTimeState GetNextFlatExposureState(double exposureTime, FlatWizardFilterSettingsWrapper wrapper) {
            if (exposureTime > wrapper.Settings.MaxFlatExposureTime) {
                return FlatWizardExposureTimeState.ExposureTimeAboveMaxTime;
            } else if (exposureTime < wrapper.Settings.MinFlatExposureTime) {
                return FlatWizardExposureTimeState.ExposureTimeBelowMinTime;
            } else {
                return FlatWizardExposureTimeState.ExposureTimeWithinBounds;
            }
        }

        public double GetNextExposureTime(double exposureTime, FlatWizardFilterSettingsWrapper wrapper) {
            if (dataPoints.Count >= 3) {
                return GetExpectedExposureTime(wrapper);
            } else {
                return exposureTime += wrapper.Settings.StepSize;
            }
        }

        private static double CameraBitDepthToAdu(double cameraBitDepth) {
            return Math.Pow(2, cameraBitDepth);
        }
    }

    internal struct FlatWizardUserPromptVMResponse {
        public bool Continue;
        public double NextExposureTime;
    }
}
