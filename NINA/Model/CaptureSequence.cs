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
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using NINA.Utility.Notification;

namespace NINA.Model {
    [Serializable()]
    [XmlRoot(nameof(CaptureSequenceList))]
    public class CaptureSequenceList : BaseINPC {

        public CaptureSequenceList() {
            TargetName = string.Empty;
            Mode = SequenceMode.STANDARD;
            Coordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours);
            AltitudeVisible = false;
        }

        private AsyncObservableCollection<CaptureSequence> _items = new AsyncObservableCollection<CaptureSequence>();
        [XmlElement(nameof(CaptureSequence))]
        public AsyncObservableCollection<CaptureSequence> Items {
            get {
                return _items;
            }
            set {
                _items = value;
                RaisePropertyChanged();
            }
        }

        public IEnumerator<CaptureSequence> GetEnumerator() {
            return Items.GetEnumerator();
        }

        public int Count {
            get {
                return Items.Count;
            }            
        }

        public void Add(CaptureSequence s) {            
            Items.Add(s);
            if (Items.Count == 1) {
                ActiveSequence = Items.First();
            }
        }

        public void RemoveAt(int idx) {
            if(Items.Count > idx) {
                if(Items[idx] == ActiveSequence) {
                    if(idx == Items.Count -1) {
                        ActiveSequence = null;
                    } else {
                        ActiveSequence = Items[idx + 1];
                    }
                }
                Items.RemoveAt(idx);
            }
            
        }

        public void Save(string path) {
            try {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(CaptureSequenceList));
                
                using (StreamWriter writer = new StreamWriter(path)) {
                    xmlSerializer.Serialize(writer, this);
                }
                
            } catch (Exception ex) {
                Logger.Error(ex.Message, ex.StackTrace);
                Notification.ShowError(ex.Message);                
            }
            
        }        

        public static CaptureSequenceList Load(string path) {
            CaptureSequenceList l = null;
            try {
                var listXml = XElement.Load(path);

                System.IO.StringReader reader = new System.IO.StringReader(listXml.ToString());
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(CaptureSequenceList));

                l = (CaptureSequenceList)xmlSerializer.Deserialize(reader);
            } catch (Exception ex) {
                Logger.Error(ex.Message, ex.StackTrace);
                Notification.ShowError(ex.Message);
            }
            return l;
        }

        public CaptureSequenceList(CaptureSequence seq) : this() {
            Add(seq);
        }

        public void SetSequenceTarget(DeepSkyObject dso) {
            TargetName = dso.Name;
            Coordinates = dso.Coordinates;
        }

        private string _targetName;
        [XmlAttribute(nameof(TargetName))]
        public string TargetName {
            get {
                return _targetName;
            }
            set {
                _targetName = value;
                RaisePropertyChanged();
            }
        }

        private SequenceMode _mode;
        [XmlAttribute(nameof(Mode))]
        public SequenceMode Mode {
            get {
                return _mode;
            }
            set {
                _mode = value;
                RaisePropertyChanged();
            }
        }

        private bool _isRunning;
        [XmlIgnore]
        public bool IsRunning {
            get {
                return _isRunning;
            }
            set {
                _isRunning = value;
                RaisePropertyChanged();
            }
        }

        public CaptureSequence Next() {
            if (Items.Count == 0) { return null; }

            CaptureSequence seq = null;

            if (Mode == SequenceMode.STANDARD) {
                seq = ActiveSequence ?? Items.First();
                if (seq.ProgressExposureCount < seq.TotalExposureCount) {
                } else {
                    //No exposures remaining. Get next Sequence
                    var idx = Items.IndexOf(seq) + 1;
                    if (idx < Items.Count) {
                        seq = Items[idx];
                    } else {
                        seq = null;
                    }
                }
            } else if (Mode == SequenceMode.ROTATE) {
                //Check if all sequences are done
                if (Items.Count == Items.Where(x => x.ProgressExposureCount == x.TotalExposureCount).Count()) {
                    //All sequences done
                    ActiveSequence = null;
                    return null;
                }

                seq = ActiveSequence;
                if (seq == Items.FirstOrDefault() && seq?.ProgressExposureCount == 0 && seq?.TotalExposureCount > 0) {
                    //first sequence active
                    seq = Items.First();
                } else {
                    var idx = (Items.IndexOf(seq) + 1) % Items.Count;
                    seq = Items[idx];
                }

                if (seq.ProgressExposureCount == seq.TotalExposureCount) {
                    ActiveSequence = seq;
                    return this.Next(); //Search for next sequence where exposurecount > 0
                }
            }

            ActiveSequence = seq;
            if(seq != null) {
                seq.ProgressExposureCount++;
            }
            return seq;
        }

        private Coordinates _coordinates;
        [XmlElement(nameof(Coordinates))]
        public Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
                RaiseCoordinatesChanged();
            }
        }
        [XmlAttribute(nameof(RAHours))]
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
        [XmlAttribute(nameof(RAMinutes))]
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
        [XmlAttribute(nameof(RASeconds))]
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


        [XmlAttribute(nameof(DecDegrees))]
        public int DecDegrees {
            get {
                return (int)(Math.Truncate(_coordinates.Dec));
            }
            set {
                _coordinates.Dec = _coordinates.Dec - DecDegrees + value;
                RaiseCoordinatesChanged();
            }
        }
        [XmlAttribute(nameof(DecMinutes))]
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
        [XmlAttribute(nameof(DecSeconds))]
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
            RaisePropertyChanged(nameof(Coordinates));
            RaisePropertyChanged(nameof(RAHours));
            RaisePropertyChanged(nameof(RAMinutes));
            RaisePropertyChanged(nameof(RASeconds));
            RaisePropertyChanged(nameof(DecDegrees));
            RaisePropertyChanged(nameof(DecMinutes));
            RaisePropertyChanged(nameof(DecSeconds));
            AltitudeVisible = true;
            DSO = new DeepSkyObject(this.TargetName, Coordinates);
        }

        private DeepSkyObject _dso;
        [XmlIgnore]
        public DeepSkyObject DSO {
            get {
                return _dso;
            }
            set {
                _dso = value;
                _dso.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), Settings.Latitude, Settings.Longitude);
                RaisePropertyChanged();
            }
        }

        private object lockobj = new object();
        private CaptureSequence _activeSequence;
        [XmlIgnore]
        public CaptureSequence ActiveSequence {
            get {
                lock (lockobj) {
                    return _activeSequence;
                }
            }
            private set {
                lock (lockobj) {
                    _activeSequence = value;
                    RaisePropertyChanged(nameof(ActiveSequence));
                    RaisePropertyChanged(nameof(ActiveSequenceIndex));
                }
            }
        }

        public int ActiveSequenceIndex {
            get {
                return Items.IndexOf(_activeSequence) == -1 ? -1 : Items.IndexOf(_activeSequence) + 1;
            }
        }

        private int _delay;
        [XmlAttribute(nameof(Delay))]
        public int Delay {
            get {
                return _delay;
            }
            set {
                _delay = value;
                RaisePropertyChanged();
            }
        }

        private bool _slewToTarget;
        [XmlAttribute(nameof(SlewToTarget))]
        public bool SlewToTarget {
            get {
                return _slewToTarget;
            }
            set {
                _slewToTarget = value;
                if (!_slewToTarget) { CenterTarget = _slewToTarget; }
                RaisePropertyChanged();
            }
        }

        private bool _autoFocusOnStart;
        [XmlAttribute(nameof(AutoFocusOnStart))]
        public bool AutoFocusOnStart {
            get {
                return _autoFocusOnStart;
            }
            set {
                _autoFocusOnStart = value;
                RaisePropertyChanged();
            }
        }

        private bool _centerTarget;
        [XmlAttribute(nameof(CenterTarget))]
        public bool CenterTarget {
            get {
                return _centerTarget;
            }
            set {
                _centerTarget = value;
                if (_centerTarget) { SlewToTarget = _centerTarget; }
                RaisePropertyChanged();
            }
        }

        private bool _startGuiding;
        [XmlAttribute(nameof(StartGuiding))]
        public bool StartGuiding {
            get {
                return _startGuiding;
            }
            set {
                _startGuiding = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoFocusOnFilterChange;
        [XmlAttribute(nameof(AutoFocusOnFilterChange))]
        public bool AutoFocusOnFilterChange {
            get {
                return _autoFocusOnFilterChange;
            }
            set {
                _autoFocusOnFilterChange = value;
                RaisePropertyChanged();
            }
        }

        private bool _altitudeVisisble;
        [XmlIgnore]
        public bool AltitudeVisible {
            get {
                return _altitudeVisisble;
            }
            private set {
                _altitudeVisisble = value;
                RaisePropertyChanged();
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
        }

        private double _exposureTime;
        private string _imageType;
        private MyFilterWheel.FilterInfo _filterType;
        private MyCamera.BinningMode _binning;
        private int _progressExposureCount;        

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
                if(_totalExposureCount < ProgressExposureCount && _totalExposureCount >= 0) {
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
                if(ProgressExposureCount > TotalExposureCount) {
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
    }
}
