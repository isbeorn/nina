using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model {
    public class SequenceModel :BaseINPC {
        public static class ImageTypes {
            public const string LIGHT = "LIGHT";
            public const string FLAT = "FLAT";
            public const string DARK = "DARK";
            public const string BIAS = "BIAS";
            public const string SNAP = "SNAP";
        }

        public SequenceModel() {
            ExposureTime = 1;
            ImageType = ImageTypes.LIGHT;
            TotalExposureCount = 1;            
            Dither = false;
            DitherAmount = 1; 
        }

        public override string ToString() {
            return "Model";
        }

        public SequenceModel(double exposureTime, string imageType, MyFilterWheel.FilterInfo filterType, MyCamera.BinningMode binning, int exposureCount) {
            ExposureTime = exposureTime;
            ImageType = imageType;
            FilterType = filterType;
            Binning = binning;
            ExposureCount = exposureCount;
        }

        private double _exposureTime;
        private string _imageType;
        private MyFilterWheel.FilterInfo _filterType;
        private MyCamera.BinningMode _binning;
        private int _exposureCount;        

        public double ExposureTime {
            get {
                return _exposureTime;
            }

            set {
                _exposureTime = value;
                RaisePropertyChanged();
            }
        }

        public string ImageType {
            get {
                return _imageType;
            }

            set {
                _imageType = value;
                RaisePropertyChanged();
            }
        }

        public Model.MyFilterWheel.FilterInfo FilterType {
            get {
                return _filterType;
            }

            set {
                _filterType = value;
                RaisePropertyChanged();
            }
        }

        public MyCamera.BinningMode Binning {
            get {
                if(_binning == null) {
                    _binning = new MyCamera.BinningMode(1, 1);
                }
                return _binning;
            }

            set {
                _binning = value;
                RaisePropertyChanged();
            }
        }

        public int ExposureCount {
            get {
                return _exposureCount;
            }

            set {
                _exposureCount = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ProgressExposureCount));

            }
        }

        private int _totalExposureCount;
        public int TotalExposureCount {
            get {
                return _totalExposureCount;
            }
            set {
                _totalExposureCount = value;
                ExposureCount = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ProgressExposureCount));
            }
        }

        public int ProgressExposureCount {
            get {
                return TotalExposureCount - ExposureCount;
            }
        }

        private bool _dither;
        public bool Dither {
            get {
                return _dither;
            }
            set {
                _dither = value;
                RaisePropertyChanged();
            }
        }

        private int _ditherAmount;
        public int DitherAmount {
            get {
                return _ditherAmount;
            }
            set {
                _ditherAmount = value;
                RaisePropertyChanged();
            }
        }
    }
}
