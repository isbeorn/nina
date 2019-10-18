namespace NINA.Model.MyFlatDevice
{
    internal class FlatDeviceInfo : DeviceInfo
    {
        private CoverState coverState;

        public CoverState CoverState {
            get => coverState;
            set { coverState = value; RaisePropertyChanged(); }
        }

        private bool lightOn;

        public bool LightOn {
            get => lightOn;
            set { lightOn = value; RaisePropertyChanged(); }
        }

        private int brightness;

        public int Brightness {
            get => brightness;
            set { brightness = value; RaisePropertyChanged(); }
        }
    }
}