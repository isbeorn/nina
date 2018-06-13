using NINA.Utility;
using System;

namespace NINA.Model {

    public class ApplicationStatus : BaseINPC {
        private string _source;

        public string Source {
            get {
                return _source;
            }
            set {
                _source = value;
                RaisePropertyChanged();
            }
        }

        private string _status;

        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();
            }
        }

        private double _progress = -1;

        public double Progress {
            get {
                return _progress;
            }
            set {
                _progress = value;
                RaisePropertyChanged();
            }
        }

        private int _maxProgress = 1;

        public int MaxProgress {
            get {
                return _maxProgress;
            }
            set {
                _maxProgress = value;
                RaisePropertyChanged();
            }
        }

        private StatusProgressType _progressType = StatusProgressType.Percent;

        public StatusProgressType ProgressType {
            get {
                return _progressType;
            }
            set {
                _progressType = value;
                RaisePropertyChanged();
            }
        }
        private string _status2;

        public string Status2 {
            get {
                return _status2;
            }
            set {
                _status2 = value;
                RaisePropertyChanged();
            }
        }

        private double _progress2 = -1;

        public double Progress2 {
            get {
                return _progress2;
            }
            set {
                _progress2 = value;
                RaisePropertyChanged();
            }
        }

        private int _maxProgress2 = 1;

        public int MaxProgress2 {
            get {
                return _maxProgress2;
            }
            set {
                _maxProgress2 = value;
                RaisePropertyChanged();
            }
        }

        private StatusProgressType _progressType2 = StatusProgressType.Percent;

        public StatusProgressType ProgressType2 {
            get {
                return _progressType2;
            }
            set {
                _progressType2 = value;
                RaisePropertyChanged();
            }
        }
        private string _status3;

        public string Status3 {
            get {
                return _status3;
            }
            set {
                _status3 = value;
                RaisePropertyChanged();
            }
        }

        private double _progress3 = -1;

        public double Progress3 {
            get {
                return _progress3;
            }
            set {
                _progress3 = value;
                RaisePropertyChanged();
            }
        }

        private int _maxProgress3 = 1;

        public int MaxProgress3 {
            get {
                return _maxProgress3;
            }
            set {
                _maxProgress3 = value;
                RaisePropertyChanged();
            }
        }

        private StatusProgressType _progressType3 = StatusProgressType.Percent;

        public StatusProgressType ProgressType3 {
            get {
                return _progressType3;
            }
            set {
                _progressType3 = value;
                RaisePropertyChanged();
            }
        }

        public enum StatusProgressType {
            Percent,
            ValueOfMaxValue
        }
    }
}