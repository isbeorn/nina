using System.Globalization;
using NINA.Utility.Enum;

namespace NINA.Utility.Profile {

    public interface IApplicationSettings : ISettings {
        string Culture { get; set; }
        string DatabaseLocation { get; set; }
        double DevicePollingInterval { get; set; }
        CultureInfo Language { get; set; }
        LogLevelEnum LogLevel { get; set; }
        string SkyAtlasImageRepository { get; set; }
    }
}