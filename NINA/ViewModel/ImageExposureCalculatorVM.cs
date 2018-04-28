using NINA.Model.MyCamera;
using NINA.Utility.Profile;
using System;

namespace NINA.ViewModel {

    public class ImageExposureCalculatorVM : DockableVM {

        public ImageExposureCalculatorVM() {
            Title = "LblExposureCalculator";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistogramSVG"];

            ContentId = nameof(ImageExposureCalculatorVM);
            Statistics = new ImageStatistics { };
            RaiseAllPropertiesChanged();
        }

        private ImageStatistics _statistics;
        private double _recommendedExposureTime;

        public ImageStatistics Statistics {
            get {
                return _statistics;
            }
            set {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        public double MinExposureADU {
            get {
                return Offset + 3 * _squaredReadNoise;
            }
        }

        public double MaxExposureADU {
            get {
                return Offset + 10 * _squaredReadNoise;
            }
        }

        public bool IsBetweenMinAndMax {
            get {
                return _statistics.Mean > MinExposureADU && _statistics.Mean < MaxExposureADU;
            }
        }

        public double RecommendedExposureTime {
            get {
                return _recommendedExposureTime;
            }
            set {
                _recommendedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private double _squaredReadNoise {
            get {
                return ConvertTo16Bit(Math.Pow(ProfileManager.Instance.ActiveProfile.CameraSettings.ReadNoise / ProfileManager.Instance.ActiveProfile.CameraSettings.FullWellCapacity / Math.Pow(2, BitDepth), 2));
            }
        }

        public double Offset {
            get {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.Offset;
            }
        }

        public double BitDepth {
            get {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        private double ConvertTo16Bit(double value) {
            return value * (Math.Pow(2, 16) / Math.Pow(2, BitDepth));
        }

        public void Add(ImageStatistics stats) {
            Statistics = stats;
            RaisePropertyChanged(nameof(IsBetweenMinAndMax));
            if (stats.ExposureTime > 0) {
                CalculateRecommendedExposureTime(stats.Mean, stats.ExposureTime);
            }
        }

        private void CalculateRecommendedExposureTime(double mean, double exposureTime) {
            var minimumRecommendedExposureTime = ((MinExposureADU - Offset) / (mean - Offset)) * exposureTime;
            var downloadTime = ProfileManager.Instance.ActiveProfile.SequenceSettings.EstimatedDownloadTime.TotalSeconds;

            var optimalRatioExposureTime = 9 * downloadTime;

            var recommendedTime = minimumRecommendedExposureTime;

            if (optimalRatioExposureTime > minimumRecommendedExposureTime) {
                recommendedTime = optimalRatioExposureTime;
            }

            RecommendedExposureTime = recommendedTime;
        }
    }
}