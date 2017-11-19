using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using NINA.Utility.Astrometry;
using System.ComponentModel;
using NINA.ViewModel;

namespace NINA.Model {

    public class CaptureSequenceList : AsyncObservableCollection<CaptureSequence> {

        public CaptureSequenceList() {
            TargetName = string.Empty;
            Mode = SequenceMode.STANDARD;
            Coordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours);
            AltitudeVisible = false;
        }

        public CaptureSequenceList(CaptureSequence seq) : this() {
            this.Add(seq);
        }

        public void SetSequenceTarget(DeepSkyObject dso) {
            TargetName = dso.Name;
            Coordinates = dso.Coordinates;
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

        private SequenceMode _mode;
        public SequenceMode Mode {
            get {
                return _mode;
            }
            set {
                _mode = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Mode)));
            }
        }

        private bool _isRunning;
        public bool IsRunning {
            get {
                return _isRunning;
            }
            set {
                _isRunning = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IsRunning)));
            }
        }

        public CaptureSequence Next() {
            if (this.Count == 0) { return null; }

            CaptureSequence seq = null;

            if (Mode == SequenceMode.STANDARD) {
                seq = ActiveSequence ?? this.First();
                if (seq.ExposureCount > 0) {
                    //There are exposures remaining. Reduce by 1 and return
                    seq.ExposureCount--;
                } else {
                    //No exposures remaining. Get next Sequence, reduce by 1 and return
                    var idx = this.IndexOf(seq) + 1;
                    if (idx < this.Count) {
                        seq = this[idx];
                        seq.ExposureCount--;
                    } else {
                        seq = null;
                    }
                }
            } else if (Mode == SequenceMode.ROTATE) {
                if (this.Count == this.Where(x => x.ExposureCount == 0).Count()) {
                    //All sequences done
                    ActiveSequence = null;
                    return null;
                }

                seq = ActiveSequence;
                if (seq == null) {
                    seq = this.First();
                } else {
                    var idx = (this.IndexOf(seq) + 1) % this.Count;
                    seq = this[idx];
                    seq.ExposureCount--;
                }

                if (seq.ExposureCount == 0) {
                    ActiveSequence = seq;
                    return this.Next(); //Search for next sequence where exposurecount > 0
                }
            }

            ActiveSequence = seq;
            return seq;
        }

        private Coordinates _coordinates;
        public Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
                RaiseCoordinatesChanged();
            }
        }

        public int RAHours {
            get {
                return (int)Math.Abs(Math.Truncate(_coordinates.RA));
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RAHours + value;
                    RaiseCoordinatesChanged();
                }

            }
        }

        public int RAMinutes {
            get {
                return (int)Math.Abs(Math.Truncate((_coordinates.RA - RAHours) * 60));
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RAMinutes / 60.0d + value / 60.0d;
                    RaiseCoordinatesChanged();
                }

            }
        }

        public int RASeconds {
            get {
                return (int)Math.Abs(Math.Truncate((_coordinates.RA - RAHours - RAMinutes / 60.0d) * 60d * 60d));
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RASeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                    RaiseCoordinatesChanged();
                }

            }
        }



        public int DecDegrees {
            get {
                return (int)(Math.Truncate(_coordinates.Dec));
            }
            set {
                _coordinates.Dec = _coordinates.Dec - DecDegrees + value;
                RaiseCoordinatesChanged();
            }
        }

        public int DecMinutes {
            get {
                return (int)Math.Abs(Math.Truncate((_coordinates.Dec - DecDegrees) * 60));
            }
            set {
                if (_coordinates.Dec < 0) {
                    _coordinates.Dec = _coordinates.Dec + DecMinutes / 60.0d - value / 60.0d;
                } else {
                    _coordinates.Dec = _coordinates.Dec - DecMinutes / 60.0d + value / 60.0d;
                }

                RaiseCoordinatesChanged();
            }
        }

        public int DecSeconds {
            get {
                if (_coordinates.Dec >= 0) {
                    return (int)Math.Abs(Math.Truncate((_coordinates.Dec - DecDegrees - DecMinutes / 60.0d) * 60d * 60d));
                } else {
                    return (int)Math.Abs(Math.Truncate((_coordinates.Dec - DecDegrees + DecMinutes / 60.0d) * 60d * 60d));
                }
            }
            set {
                if (_coordinates.Dec < 0) {
                    _coordinates.Dec = _coordinates.Dec + DecSeconds / (60.0d * 60.0d) - value / (60.0d * 60.0d);
                } else {
                    _coordinates.Dec = _coordinates.Dec - DecSeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                }

                RaiseCoordinatesChanged();
            }
        }

        private void RaiseCoordinatesChanged() {
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Coordinates)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(RAHours)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(RAMinutes)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(RASeconds)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(DecDegrees)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(DecMinutes)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(DecSeconds)));
            AltitudeVisible = true;
            DSO = new DeepSkyObject(this.TargetName, Coordinates);
        }

        private DeepSkyObject _dso;
        public DeepSkyObject DSO {
            get {
                return _dso;
            }
            set {
                _dso = value;
                _dso.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), Settings.Latitude, Settings.Longitude);
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
                return this.IndexOf(_activeSequence) == -1 ? -1 : this.IndexOf(_activeSequence) + 1;
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

        private bool _slewToTarget;
        public bool SlewToTarget {
            get {
                return _slewToTarget;
            }
            set {
                _slewToTarget = value;
                if (!_slewToTarget) { CenterTarget = _slewToTarget; }
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(SlewToTarget)));
            }
        }

        private bool _centerTarget;
        public bool CenterTarget {
            get {
                return _centerTarget;
            }
            set {
                _centerTarget = value;
                if (_centerTarget) { SlewToTarget = _centerTarget; }
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(CenterTarget)));
            }
        }

        private bool _startGuiding;
        public bool StartGuiding {
            get {
                return _startGuiding;
            }
            set {
                _startGuiding = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(StartGuiding)));
            }
        }

        private bool _altitudeVisisble;
        public bool AltitudeVisible {
            get {
                return _altitudeVisisble;
            }
            private set {
                _altitudeVisisble = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(AltitudeVisible)));
            }
        }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SequenceMode {
        [Description("LblSequenceModeStandard")]
        STANDARD,
        [Description("LblSequenceModeRotate")]
        ROTATE
    }

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


        /// <summary>
        /// Remaining Exposure Count
        /// </summary>
        public int ExposureCount {
            get {
                return _exposureCount;
            }

            set {
                _exposureCount = value;
                if (_exposureCount < 0) { _exposureCount = 0; }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ProgressExposureCount));

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
