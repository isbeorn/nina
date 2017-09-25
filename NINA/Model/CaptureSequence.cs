using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using NINA.Utility.Astrometry;

namespace NINA.Model {

    public class CaptureSequenceList : AsyncObservableCollection<CaptureSequence> {

        public CaptureSequenceList() {
            TargetName = string.Empty;
            Mediator.Instance.Register((object o) => {
                var args = (object[])o;
                if(args.Length == 1) {
                    DSO = (DeepSkyObject)args[0];
                    TargetName = DSO.AlsoKnownAs.FirstOrDefault();
                    Coordinates = DSO.Coordinates;                    
                }                
            },MediatorMessages.SetSequenceCoordinates);
        }

        public CaptureSequenceList(CaptureSequence seq) : this() {
            this.Add(seq);
        }

        private string _targetName;
        public string TargetName {
            get {
                return _targetName;
            }
            set {
                _targetName = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(TargetName)));
            }
        }

        public bool SetActiveSequence(CaptureSequence seq) {
            if(this.Contains(seq)) {
                ActiveSequence = seq;
                return true;
            } else {
                return false;
            }            
        }

        private Coordinates _coordinates;
        public Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Coordinates)));
            }
        }

        private DeepSkyObject _dso;
        public DeepSkyObject DSO {
            get {
                return _dso;
            }
            set {
                _dso = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(DSO)));
            }
        }

        private CaptureSequence _activeSequence;
        public CaptureSequence ActiveSequence {
            get {
                return _activeSequence;
            }
            private set {
                _activeSequence = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ActiveSequence)));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ActiveSequenceIndex)));
            }
        }

        public int ActiveSequenceIndex {
            get {
                return this.IndexOf(_activeSequence);
            }
        }

        private int _delay;
        public int Delay {
            get {
                return _delay;
            }
            set {
                _delay = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Delay)));
            }
        }
    }

    public class CaptureSequence :BaseINPC {
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
            return ExposureCount.ToString() + "x" + ExposureTime.ToString() + " " + ImageType;
        }

        public CaptureSequence(double exposureTime, string imageType, MyFilterWheel.FilterInfo filterType, MyCamera.BinningMode binning, int exposureCount) {
            ExposureTime = exposureTime;
            ImageType = imageType;
            FilterType = filterType;
            Binning = binning;
            TotalExposureCount = exposureCount;
            DitherAmount = 1;
            Gain = -1;
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

        
        /// <summary>
        /// Remaining Exposure Count
        /// </summary>
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

        public int ExposureNr {
            get {
                return TotalExposureCount - ExposureCount + 1;
            }
        }

        private short _gain;
        public short Gain {
            get {
                return _gain;
            }
            set {
                _gain = value;
                RaisePropertyChanged();
            }
        }

        private int _totalExposureCount;        
        /// <summary>
        /// Total exposures of a sequence
        /// </summary>
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

        /// <summary>
        /// Number of exposures already taken
        /// </summary>
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
