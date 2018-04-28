using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Model.MyCamera;
using NINA.Utility.Mediator;
using NINA.Utility.Profile;

namespace NINA.ViewModel
{
    public class ImageExposureCalculatorVM : DockableVM
    {

        public ImageExposureCalculatorVM()
        {
            Title = "LblExposureCalculator";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistogramSVG"];

            ContentId = nameof(ImageExposureCalculatorVM);
            Statistics = new ImageStatistics { };
            RaiseAllPropertiesChanged();
        }

        private ImageStatistics _statistics;
        private double _recommendedExposureTime;

        public ImageStatistics Statistics
        {
            get
            {
                return _statistics;
            }
            set
            {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        public double MinExposureADU
        {
            get
            {
                return Offset + 3 * _squaredReadNoise;
            }
        }

        public double MaxExposureADU
        {
            get
            {
                return Offset + 10 * _squaredReadNoise;
            }
        }

        public bool IsBetweenMinAndMax
        {
            get
            {
                return _statistics.Mean > MinExposureADU && _statistics.Mean < MaxExposureADU;
            }
        }

        public double RecommendedExposureTime
        {
            get
            {
                return _recommendedExposureTime;
            }
            set
            {
                _recommendedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private double _gain
        {
            get
            {
                return FullWellCapacity / Math.Pow(2, BitDepth);
            }
        }

        private double _readNoiseADU
        {
            get
            {
                return ReadNoise / _gain;
            }
        }

        private double _squaredReadNoise
        {
            get
            {
                return ConvertTo16Bit(Math.Pow(_readNoiseADU, 2));
            }
        }

        public double ReadNoise
        {
            get
            {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.ReadNoise;
            }
        }

        public double Offset
        {
            get
            {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.Offset;
            }
        }

        public double FullWellCapacity
        {
            get
            {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.FullWellCapacity;
            }
        }

        public double BitDepth
        {
            get
            {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        private double ConvertTo16Bit(double value)
        {
            return value * (Math.Pow(2, 16) / Math.Pow(2, BitDepth));
        }

        public void Add(ImageStatistics stats)
        {
            Statistics = stats;
            RaisePropertyChanged(nameof(IsBetweenMinAndMax));
            RecommendedExposureTime = ((MinExposureADU - Offset) / (stats.Mean - Offset)) * stats.ExposureTime;
        }
    }
}
