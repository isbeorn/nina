using NINA.Model.MyCamera;
using System.Collections.Generic;

namespace NINA.Profile {

    public interface IFlatDeviceSettings : ISettings {
        string Id { get; set; }
        string Name { get; set; }
        string PortName { get; set; }
        bool OpenForDarkFlats { get; set; }
        bool CloseAtSequenceEnd { get; set; }
        bool UseWizardTrainedValues { get; set; }

        Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> FilterSettings { get; set; }

        void AddBrightnessInfo(FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value);

        FlatDeviceFilterSettingsValue GetBrightnessInfo(FlatDeviceFilterSettingsKey key);

        IEnumerable<BinningMode> GetBrightnessInfoBinnings();

        IEnumerable<short> GetBrightnessInfoGains();

        void ClearBrightnessInfo();
    }
}