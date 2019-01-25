using NINA.Utility;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace NINA.Model {

    [Serializable()]
    [XmlRoot(ElementName = "CaptureSequence")]
    public class CaptureSequence : BaseINPC {

        public static class ImageTypes {
            public const string LIGHT = "LIGHT";
            public const string FLAT = "FLAT";
            public const string DARK = "DARK";
            public const string BIAS = "BIAS";
            public const string DARKFLAT = "DARKFLAT";
            public const string SNAP = "SNAP";
        }

        public CaptureSequence() {
            ExposureTime = 1;
            ImageType = ImageTypes.LIGHT;
            TotalExposureCount = 1;
            Dither = false;
            DitherAmount = 1;
            Gain = -1;
        }

        public override string ToString() {
            return TotalExposureCount.ToString() + "x" + ExposureTime.ToString() + " " + ImageType;
        }

        public CaptureSequence(double exposureTime, string imageType, MyFilterWheel.FilterInfo filterType, MyCamera.BinningMode binning, int exposureCount) {
            ExposureTime = exposureTime;
            ImageType = imageType;
            FilterType = filterType;
            Binning = binning;
            TotalExposureCount = exposureCount;
            DitherAmount = 1;
            Gain = -1;
            Enabled = true;
        }

        private double _exposureTime;
        private string _imageType;
        private MyFilterWheel.FilterInfo _filterType;
        private MyCamera.BinningMode _binning;
        private int _progressExposureCount;

        [XmlElement(nameof(Enabled))]
        public bool Enabled {
            get {
                return _enabled;
            }
            set {
                _enabled = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(ExposureTime))]
        public double ExposureTime {
            get {
                return _exposureTime;
            }

            set {
                _exposureTime = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(ImageType))]
        public string ImageType {
            get {
                return _imageType;
            }

            set {
                _imageType = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(FilterType))]
        public Model.MyFilterWheel.FilterInfo FilterType {
            get {
                return _filterType;
            }

            set {
                _filterType = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(Binning))]
        public MyCamera.BinningMode Binning {
            get {
                if (_binning == null) {
                    _binning = new MyCamera.BinningMode(1, 1);
                }
                return _binning;
            }

            set {
                _binning = value;
                RaisePropertyChanged();
            }
        }

        private short _gain;

        [XmlElement(nameof(Gain))]
        public short Gain {
            get {
                return _gain;
            }
            set {
                _gain = value;
                RaisePropertyChanged();
            }
        }

        private bool _enableSubSample = false;

        [XmlIgnore]
        public bool EnableSubSample {
            get {
                return _enableSubSample;
            }
            set {
                _enableSubSample = value;
                RaisePropertyChanged();
            }
        }

        private int _totalExposureCount;

        /// <summary>
        /// Total exposures of a sequence
        /// </summary>
        [XmlElement(nameof(TotalExposureCount))]
        public int TotalExposureCount {
            get {
                return _totalExposureCount;
            }
            set {
                _totalExposureCount = value;
                if (_totalExposureCount < ProgressExposureCount && _totalExposureCount >= 0) {
                    ProgressExposureCount = _totalExposureCount;
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Number of exposures already taken
        /// </summary>
        [XmlElement(nameof(ProgressExposureCount))]
        public int ProgressExposureCount {
            get {
                return _progressExposureCount;
            }
            set {
                _progressExposureCount = value;
                if (ProgressExposureCount > TotalExposureCount) {
                    TotalExposureCount = ProgressExposureCount;
                }
                RaisePropertyChanged();
            }
        }

        private bool _dither;

        [XmlElement(nameof(Dither))]
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
        private bool _enabled = true;

        [XmlElement(nameof(DitherAmount))]
        public int DitherAmount {
            get {
                return _ditherAmount;
            }
            set {
                _ditherAmount = value;
                RaisePropertyChanged();
            }
        }

        private CaptureSequence nextSequence;

        [XmlIgnore]
        public CaptureSequence NextSequence {
            get => nextSequence;
            set {
                nextSequence = value;
                RaisePropertyChanged();
            }
        }
    }
}