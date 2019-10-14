namespace NINA.Model.MyFlatDevice {

    internal class FlatDeviceInfo : DeviceInfo {
        private CoverState coverState;

        public CoverState CoverState {
            get { return coverState; }
            set { coverState = value; RaisePropertyChanged(); }
        }

        private bool lightOn;

        public bool LightOn {
            get { return lightOn; }
            set { lightOn = value; RaisePropertyChanged(); }
        }

        private int brightness;

        public int Brightness {
            get { return brightness; }
            set { brightness = value; RaisePropertyChanged(); }
        }
    }
}