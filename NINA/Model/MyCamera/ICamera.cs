using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {
    interface ICamera {

        bool HasShutter { get; }
        bool Connected { get; set; }
        double CCDTemperature { get; }
        double SetCCDTemperature { get; set; }
        short BinX { get; set; }
        short BinY { get; set; }
        string Name { get;  }
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
        bool CanSetCCDTemperature { get; }
        bool CoolerOn { get; set; }
        double CoolerPower { get; }
        AsyncObservableCollection<BinningMode> BinningModes { get; }

        bool Connect();
        void Disconnect();
        void UpdateValues();
        void SetBinning(short x, short y);
        void StartExposure(double exposureTime, bool isLightFrame);
        void StopExposure();
        void AbortExposure();
        Task<Array> DownloadExposure(CancellationTokenSource tokenSource); 
    }
}
