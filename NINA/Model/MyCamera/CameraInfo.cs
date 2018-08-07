using NINA.Utility;

namespace NINA.Model.MyCamera {

    internal class CameraInfo : BaseINPC {
        private bool connected;
        public bool Connected { get { return connected; } set { connected = value; RaisePropertyChanged(); } }
        private bool hasShutter;
        public bool HasShutter { get { return hasShutter; } set { hasShutter = value; RaisePropertyChanged(); } }
        private string name;
        public string Name { get { return name; } set { name = value; RaisePropertyChanged(); } }
        private double temperature;
        public double Temperature { get { return temperature; } set { temperature = value; RaisePropertyChanged(); } }
        private short gain;
        public short Gain { get { return gain; } set { gain = value; RaisePropertyChanged(); } }
        private short binxX;
        public short BinX { get { return binxX; } set { binxX = value; RaisePropertyChanged(); } }
        private short binY;
        public short BinY { get { return binY; } set { binY = value; RaisePropertyChanged(); } }
        private int offset;
        public int Offset { get { return offset; } set { offset = value; RaisePropertyChanged(); } }
        private bool isSubSampleEnabled;
        public bool IsSubSampleEnabled { get { return isSubSampleEnabled; } set { isSubSampleEnabled = value; RaisePropertyChanged(); } }
        private string cameraState;
        public string CameraState { get { return cameraState; } set { cameraState = value; RaisePropertyChanged(); } }
        private int xSize;
        public int XSize { get { return xSize; } set { xSize = value; RaisePropertyChanged(); } }
        private int ySize;
        public int YSize { get { return ySize; } set { ySize = value; RaisePropertyChanged(); } }
        private double pixelSize;
        public double PixelSize { get { return pixelSize; } set { pixelSize = value; RaisePropertyChanged(); } }
    }
}