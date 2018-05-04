using NINA.Utility;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    internal interface ICamera : IDevice {
        bool HasShutter { get; }
        bool Connected { get; }
        double Temperature { get; }
        double TemperatureSetPoint { get; set; }
        short BinX { get; set; }
        short BinY { get; set; }
        string Description { get; }
        string DriverInfo { get; }
        string DriverVersion { get; }
        string SensorName { get; }
        ASCOM.DeviceInterface.SensorType SensorType { get; }
        int CameraXSize { get; }
        int CameraYSize { get; }
        double ExposureMin { get; }
        double ExposureMax { get; }
        short MaxBinX { get; }
        short MaxBinY { get; }
        double PixelSizeX { get; }
        double PixelSizeY { get; }
        bool CanSetTemperature { get; }
        bool CoolerOn { get; set; }
        double CoolerPower { get; }
        string CameraState { get; }
        bool CanSubSample { get; }
        bool EnableSubSample { get; set; }
        int SubSampleX { get; set; }
        int SubSampleY { get; set; }
        int SubSampleWidth { get; set; }
        int SubSampleHeight { get; set; }
        bool CanShowLiveView { get; }
        bool LiveViewEnabled { get; set; }

        int Offset { get; set; }
        int USBLimit { get; set; }
        bool CanSetOffset { get; }
        bool CanSetUSBLimit { get; }
        bool CanGetGain { get; }
        bool CanSetGain { get; }
        short GainMax { get; }
        short GainMin { get; }
        short Gain { get; set; }

        ArrayList Gains { get; }

        AsyncObservableCollection<BinningMode> BinningModes { get; }

        void UpdateValues();

        void SetBinning(short x, short y);

        void StartExposure(double exposureTime, bool isLightFrame);

        void StopExposure();

        void AbortExposure();

        Task<ImageArray> DownloadExposure(CancellationToken token);
    }
}