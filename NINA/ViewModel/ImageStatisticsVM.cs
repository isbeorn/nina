using NINA.Model.MyCamera;
using NINA.Utility.Profile;
using System;

namespace NINA.ViewModel {

    public class ImageStatisticsVM : DockableVM {
        private string _downloadToDataRatio;

        private double _maxRecommendedExposureTime;

        private double _optimizedExposureTime;

        private double _recommendedExposureTime;

        private ImageStatistics _statistics;

        public ImageStatisticsVM() {
            Title = "LblStatistics";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistogramSVG"];

            ContentId = nameof(ImageStatisticsVM);
            Statistics = new ImageStatistics { };
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

        public ImageStatistics Statistics {
            get {
                return _statistics;
            }
            set {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        private double ConvertToOutputBitDepth(double input) {
            if (Statistics.IsBayered && ProfileManager.Instance.ActiveProfile.CameraSettings.RawConverter == Utility.Enum.RawConverterEnum.DCRAW
                || !Statistics.IsBayered) {
                return input * (Math.Pow(2, 16) / Math.Pow(2, _bitDepth));
            }

            return input;
        }

        private double _bitDepth {
            get {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        private double _offset {
            get {
                return ConvertToOutputBitDepth(ProfileManager.Instance.ActiveProfile.CameraSettings.Offset);
            }
        }

        private double _squaredReadNoise {
            get {
                return ConvertToOutputBitDepth(Math.Pow(ProfileManager.Instance.ActiveProfile.CameraSettings.ReadNoise / (ProfileManager.Instance.ActiveProfile.CameraSettings.FullWellCapacity / Math.Pow(2, _bitDepth)), 2));
            }
        }

        public void Add(ImageStatistics stats) {
            Statistics = stats;

            if (stats.ExposureTime > 0) {
                CalculateRecommendedExposureTime(stats.Mean, stats.ExposureTime);
            }
        }

        private void CalculateRecommendedExposureTime(double mean, double exposureTime) {
            MinimumRecommendedExposureTime = ((MinExposureADU - _offset) / (mean - _offset)) * exposureTime;
            MaximumRecommendedExposureTime = ((MaxExposureADU - _offset) / (mean - _offset)) * exposureTime;
            var downloadTime = ProfileManager.Instance.ActiveProfile.SequenceSettings.EstimatedDownloadTime.TotalSeconds;

            var optimalRatioExposureTime = ProfileManager.Instance.ActiveProfile.CameraSettings.DownloadToDataRatio * downloadTime;

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