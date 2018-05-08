using NINA.Utility;

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

        public enum StatusProgressType {
            Percent,
            ValueOfMaxValue
        }
    }
}