#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model;
using NINA.Model.ImageData;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Imaging {

    public class ExposureCalculatorVM : DockableVM {
        private double _maxRecommendedExposureTime;
        private double _recommendedExposureTime;
        private double _optimizedExposureTime;
        private double _downloadToExposureRatio = 9;
        private Model.MyFilterWheel.FilterInfo _snapFilter;
        private CancellationTokenSource _cts;

        public ExposureCalculatorVM(IProfileService profileService, IImagingMediator imagingMediator) : base(profileService) {
            this._imagingMediator = imagingMediator;
            this.Title = "LblExposureCalculator";

            if (System.Windows.Application.Current != null) {
                ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CalculatorSVG"];
            }

            DetermineExposureTimeCommand = new AsyncCommand<bool>(DetermineExposureTime);
            CancelDetermineExposureTimeCommand = new RelayCommand(CancelDetermineExposureTime);
        }

        private void CancelDetermineExposureTime(object obj) {
            _cts?.Cancel();
        }

        private async Task<bool> DetermineExposureTime(object arg) {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            this.Statistics = null;

            var seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAPSHOT, SnapFilter, new Model.MyCamera.BinningMode(1, 1), 1);
            seq.Gain = SnapGain;
            var prepareParameters = new PrepareImageParameters(autoStretch: true, detectStars: false);
            var capture = await _imagingMediator.CaptureAndPrepareImage(seq, prepareParameters, _cts.Token, null); //todo progress
            this.Statistics = await AllImageStatistics.Create(capture.RawImageData);
            this.CalculateRecommendedExposureTime();
            return true;
        }

        public IAsyncCommand DetermineExposureTimeCommand { get; private set; }
        public ICommand CancelDetermineExposureTimeCommand { get; private set; }

        public int SnapGain {
            get => profileService.ActiveProfile.ExposureCalculatorSettings.Gain;

            set {
                profileService.ActiveProfile.ExposureCalculatorSettings.Gain = value;
                RaisePropertyChanged();
            }
        }

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get => _snapFilter;

            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        public double SnapExposureDuration {
            get => profileService.ActiveProfile.ExposureCalculatorSettings.ExposureDuration;

            set {
                profileService.ActiveProfile.ExposureCalculatorSettings.ExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        public double FullWellCapacity {
            get => profileService.ActiveProfile.ExposureCalculatorSettings.FullWellCapacity;
            set {
                profileService.ActiveProfile.ExposureCalculatorSettings.FullWellCapacity = value;
                RaisePropertyChanged();
            }
        }

        public double ReadNoise {
            get => profileService.ActiveProfile.ExposureCalculatorSettings.ReadNoise;
            set {
                profileService.ActiveProfile.ExposureCalculatorSettings.ReadNoise = value;
                RaisePropertyChanged();
            }
        }

        public double BiasMean {
            get => profileService.ActiveProfile.ExposureCalculatorSettings.BiasMean;
            set {
                profileService.ActiveProfile.ExposureCalculatorSettings.BiasMean = value;
                RaisePropertyChanged();
            }
        }

        public double MaximumRecommendedExposureTime {
            get {
                return _maxRecommendedExposureTime;
            }
            set {
                _maxRecommendedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public double MinimumRecommendedExposureTime {
            get {
                return _recommendedExposureTime;
            }
            set {
                _recommendedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public double OptimizedExposureTime {
            get {
                return _optimizedExposureTime;
            }
            set {
                _optimizedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public double MaxExposureADU {
            get {
                return _offset + 10 * _squaredReadNoise;
            }
        }

        public double MinExposureADU {
            get {
                return _offset + 3 * _squaredReadNoise;
            }
        }

        private double _offset {
            get {
                return ConvertToOutputBitDepth(BiasMean);
            }
        }

        private double _squaredReadNoise {
            get {
                if (Statistics != null) {
                    return ConvertToOutputBitDepth(Math.Pow(ReadNoise / (FullWellCapacity / Math.Pow(2, Statistics.ImageProperties.BitDepth)), 2));
                } else {
                    return 0;
                }
            }
        }

        private double ConvertToOutputBitDepth(double input) {
            if (Statistics != null) {
                if ((Statistics.ImageProperties.IsBayered && profileService.ActiveProfile.CameraSettings.RawConverter == Utility.Enum.RawConverterEnum.DCRAW)
                    || !Statistics.ImageProperties.IsBayered) {
                    return input * (Math.Pow(2, 16) / Math.Pow(2, Statistics.ImageProperties.BitDepth));
                }

                return input;
            } else {
                return 0.0;
            }
        }

        private AllImageStatistics _statistics;
        private IImagingMediator _imagingMediator;

        public AllImageStatistics Statistics {
            get {
                return _statistics;
            }
            set {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        private void CalculateRecommendedExposureTime() {
            if (Statistics.ImageStatistics.Result.Mean - _offset < 0) {
                this.Statistics = null;
                Notification.ShowError(Locale.Loc.Instance["LblExposureCalculatorMeanLessThanOffset"]);
            } else {
                MinimumRecommendedExposureTime = ((MinExposureADU - _offset) / (Statistics.ImageStatistics.Result.Mean - _offset)) * SnapExposureDuration;
                MaximumRecommendedExposureTime = ((MaxExposureADU - _offset) / (Statistics.ImageStatistics.Result.Mean - _offset)) * SnapExposureDuration;
                var downloadTime = profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime.TotalSeconds;

                var optimalRatioExposureTime = _downloadToExposureRatio * downloadTime;

                //CurrentDownloadToDataRatio = (exposureTime / downloadTime).ToString("0.000");

                var recommendedTime = MinimumRecommendedExposureTime;

                if (optimalRatioExposureTime > MinimumRecommendedExposureTime) {
                    recommendedTime = optimalRatioExposureTime;
                }

                OptimizedExposureTime = recommendedTime;

                RaisePropertyChanged(nameof(MinExposureADU));
                RaisePropertyChanged(nameof(MaxExposureADU));
            }
        }
    }
}