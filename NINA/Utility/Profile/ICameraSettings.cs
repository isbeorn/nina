using NINA.Utility.Enum;

namespace NINA.Utility.Profile {

    public interface ICameraSettings : ISettings {
        double BitDepth { get; set; }
        CameraBulbModeEnum BulbMode { get; set; }
        double DownloadToDataRatio { get; set; }
        double FullWellCapacity { get; set; }
        string Id { get; set; }
        double Offset { get; set; }
        double PixelSize { get; set; }
        RawConverterEnum RawConverter { get; set; }
        double ReadNoise { get; set; }
        string SerialPort { get; set; }
        double MinFlatExposureTime { get; set; }
        double MaxFlatExposureTime { get; set; }
    }
}