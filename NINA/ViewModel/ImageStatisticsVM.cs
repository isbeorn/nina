#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model.MyCamera;
using NINA.Utility.Profile;
using System;

namespace NINA.ViewModel {

    public class ImageStatisticsVM : DockableVM {
        private string _downloadToDataRatio;

        private double _maxRecommendedExposureTime;

        private double _optimizedExposureTime;

        private double _recommendedExposureTime;

        private IImageStatistics _statistics;

        public ImageStatisticsVM(IProfileService profileService) : base(profileService) {
            Title = "LblStatistics";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistogramSVG"];
        }

        public string CurrentDownloadToDataRatio {
            get {
                return "1:" + _downloadToDataRatio;
            }
            set {
                _downloadToDataRatio = value;
                RaisePropertyChanged();
            }
        }

        public double MaxExposureADU {
            get {
                return _offset + 10 * _squaredReadNoise;
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

        public double MinExposureADU {
            get {
                return _offset + 3 * _squaredReadNoise;
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

        public IImageStatistics Statistics {
            get {
                return _statistics;
            }
            set {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        private double ConvertToOutputBitDepth(double input) {
            if (Statistics != null) {
                if (Statistics.IsBayered && profileService.ActiveProfile.CameraSettings.RawConverter == Utility.Enum.RawConverterEnum.DCRAW
                    || !Statistics.IsBayered) {
                    return input * (Math.Pow(2, 16) / Math.Pow(2, Statistics.BitDepth));
                }

                return input;
            } else {
                return 0.0;
            }
        }

        private double _offset {
            get {
                return ConvertToOutputBitDepth(profileService.ActiveProfile.CameraSettings.Offset);
            }
        }

        private double _squaredReadNoise {
            get {
                if (Statistics != null) {
                    return ConvertToOutputBitDepth(Math.Pow(profileService.ActiveProfile.CameraSettings.ReadNoise / (profileService.ActiveProfile.CameraSettings.FullWellCapacity / Math.Pow(2, Statistics.BitDepth)), 2));
                } else {
                    return 0;
                }
            }
        }

        public void Add(IImageStatistics stats) {
            Statistics = stats;

            if (stats.ExposureTime > 0) {
                CalculateRecommendedExposureTime(stats.Mean, stats.ExposureTime);
            }
        }

        private void CalculateRecommendedExposureTime(double mean, double exposureTime) {
            MinimumRecommendedExposureTime = ((MinExposureADU - _offset) / (mean - _offset)) * exposureTime;
            MaximumRecommendedExposureTime = ((MaxExposureADU - _offset) / (mean - _offset)) * exposureTime;
            var downloadTime = profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime.TotalSeconds;

            var optimalRatioExposureTime = profileService.ActiveProfile.CameraSettings.DownloadToDataRatio * downloadTime;

            CurrentDownloadToDataRatio = (exposureTime / downloadTime).ToString("0.000");

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