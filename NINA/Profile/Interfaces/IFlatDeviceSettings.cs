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

        Dictionary<(string name, BinningMode binning, short gain),
            (double time, double brightness)> FilterSettings {
            get; set;
        }

        void AddBrightnessInfo((string name, BinningMode binning, short gain) key,
            (double time, double brightness) value);

        (double time, double brightness)? GetBrightnessInfo((string name, BinningMode binning, short gain) key);

        IEnumerable<BinningMode> GetBrightnessInfoBinnings();

        IEnumerable<short> GetBrightnessInfoGains();
    }
}