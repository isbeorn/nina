using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model {
    class SequenceModel :BaseINPC {
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
            ExposureCount = 1;            
            Dither = false;
            DitherAmount = 1; 
        }

        public override string ToString() {
            return "Model";
        }

        public SequenceModel(double exposureTime, string imageType, FilterWheelModel.FilterInfo filterType, MyCamera.BinningMode binning, int exposureCount) {
            ExposureTime = exposureTime;
            ImageType = imageType;
            FilterType = filterType;
            Binning = binning;
            ExposureCount = exposureCount;
        }

        private double _exposureTime;
        private string _imageType;
        private FilterWheelModel.FilterInfo _filterType;
        private MyCamera.BinningMode _binning;
        private int _exposureCount;
        private bool _active;

        public bool Active {
            get {
                return _active;
            }
            set {
                _active = value;
                RaisePropertyChanged();
            }
        }

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

        public FilterWheelModel.FilterInfo FilterType {
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
