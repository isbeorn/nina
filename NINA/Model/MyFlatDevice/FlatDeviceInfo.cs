namespace NINA.Model.MyFlatDevice {

    public class FlatDeviceInfo : DeviceInfo {
        private CoverState _coverState;

        public CoverState CoverState {
            get => _coverState;
            set { _coverState = value; RaisePropertyChanged(); }
        }

        private bool _lightOn;

        public bool LightOn {
            get => _lightOn;
            set { _lightOn = value; RaisePropertyChanged(); }
        }

        private int _brightness;

        public int Brightness {
            get => _brightness;
            set { _brightness = value; RaisePropertyChanged(); }
        }

        private bool _supportsOpenClose;

        public bool SupportsOpenClose {
            get => _supportsOpenClose;
            set {
                _supportsOpenClose = value;
                RaisePropertyChanged();
            }
        }

        private int _minBrightness;

        public int MinBrightness {
            get => _minBrightness;
            set { _minBrightness = value; RaisePropertyChanged(); }
        }

        private int _maxBrightness;

        public int MaxBrightness {
            get => _maxBrightness;
            set { _maxBrightness = value; RaisePropertyChanged(); }
        }
    }
}