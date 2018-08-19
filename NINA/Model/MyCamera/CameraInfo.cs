using NINA.Utility;

namespace NINA.Model.MyCamera {

    internal class CameraInfo : DeviceInfo {
        private bool hasShutter;

        public bool HasShutter {
            get { return hasShutter; }
            set { hasShutter = value; RaisePropertyChanged(); }
        }

        private double temperature;

        public double Temperature {
            get { return temperature; }
            set { temperature = value; RaisePropertyChanged(); }
        }

        private short gain;

        public short Gain {
            get { return gain; }
            set { gain = value; RaisePropertyChanged(); }
        }

        private short binxX;

        public short BinX {
            get { return binxX; }
            set { binxX = value; RaisePropertyChanged(); }
        }

        private short binY;

        public short BinY {
            get { return binY; }
            set { binY = value; RaisePropertyChanged(); }
        }

        private int offset;

        public int Offset {
            get { return offset; }
            set { offset = value; RaisePropertyChanged(); }
        }

        private bool isSubSampleEnabled;

        public bool IsSubSampleEnabled {
            get { return isSubSampleEnabled; }
            set { isSubSampleEnabled = value; RaisePropertyChanged(); }
        }

        private string cameraState;

        public string CameraState {
            get { return cameraState; }
            set { cameraState = value; RaisePropertyChanged(); }
        }

        private int xSize;

        public int XSize {
            get { return xSize; }
            set { xSize = value; RaisePropertyChanged(); }
        }

        private int ySize;

        public int YSize {
            get { return ySize; }
            set { ySize = value; RaisePropertyChanged(); }
        }

        private double pixelSize;

        public double PixelSize {
            get { return pixelSize; }
            set { pixelSize = value; RaisePropertyChanged(); }
        }

        private bool coolerOn;

        public bool CoolerOn {
            get {
                return coolerOn;
            }
            set {
                coolerOn = value;
                RaisePropertyChanged();
            }
        }

        private double _coolerPower;

        public double CoolerPower {
            get {
                return _coolerPower;
            }
            set {
                _coolerPower = value;
                RaisePropertyChanged();
            }
        }

        private bool canSubSample;

        public bool CanSubSample {
            get { return canSubSample; }
            set { canSubSample = value; RaisePropertyChanged(); }
        }

        private double temperatureSetPoint;

        public double TemperatureSetPoint {
            get { return temperatureSetPoint; }
            set { temperatureSetPoint = value; RaisePropertyChanged(); }
        }
    }
}