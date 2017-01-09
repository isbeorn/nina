using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {
    interface ICamera {

        bool HasShutter { get; set; }
        bool Connected { get; set; }
        double CCDTemperature { get; }
        double SetCCDTemperature { get; set; }
        short BinX { get; set; }
        short BinY { get; set; }
        string CameraStateString { get; }
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


        bool connect();
        void disconnect();
        void updateValues();
        void setBinning(short x, short y);
        void startExposure(double exposureTime, bool isLightFrame);
        void stopExposure();
        Task<Array> downloadExposure(CancellationTokenSource tokenSource); 
    }
}
