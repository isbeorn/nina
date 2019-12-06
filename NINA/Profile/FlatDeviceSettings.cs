using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NINA.Model.MyCamera;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    internal class FlatDeviceSettings : Settings, IFlatDeviceSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (FilterSettings == null) {
                FilterSettings =
                    new Dictionary<(string name, BinningMode binning, short gain), (double time, double brightness)>();
            }
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
        }

        private string _id;

        [DataMember]
        public string Id {
            get => _id;
            set {
                if (_id == value) return;
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        [DataMember]
        public string Name {
            get => _name;
            set {
                if (_name == value) return;
                _name = value;
                RaisePropertyChanged();
            }
        }

        private string _portName;

        [DataMember]
        public string PortName {
            get => _portName;
            set {
                if (_portName == value) return;
                _portName = value;
                RaisePropertyChanged();
            }
        }

        private bool _closeAtSequenceEnd;

        [DataMember]
        public bool CloseAtSequenceEnd {
            get => _closeAtSequenceEnd;
            set {
                if (_closeAtSequenceEnd == value) return;
                _closeAtSequenceEnd = value;
                RaisePropertyChanged();
            }
        }

        private bool _openForDarkFlats;

        [DataMember]
        public bool OpenForDarkFlats {
            get => _openForDarkFlats;
            set {
                if (_openForDarkFlats == value) return;
                _openForDarkFlats = value;
                RaisePropertyChanged();
            }
        }

        private bool _useWizardTrainedValues;

        [DataMember]
        public bool UseWizardTrainedValues {
            get => _useWizardTrainedValues;
            set {
                if (_useWizardTrainedValues == value) return;
                _useWizardTrainedValues = value;
                RaisePropertyChanged();
            }
        }

        private Dictionary<(string name, BinningMode binning, short gain), (double time, double brightness)> _filterSettings;

        [DataMember]
        public Dictionary<(string name, BinningMode binning, short gain), (double time, double brightness)> FilterSettings {
            get => _filterSettings;
            set {
                _filterSettings = value;
                RaisePropertyChanged();
            }
        }

        public void AddBrightnessInfo((string name, BinningMode binning, short gain) key, (double time, double brightness) value) {
            if (FilterSettings.ContainsKey(key)) {
                FilterSettings[key] = value;
            } else {
                FilterSettings.Add(key, value);
            }

            RaisePropertyChanged(nameof(FlatDeviceSettings));
        }

        public (double time, double brightness)? GetBrightnessInfo((string name, BinningMode binning, short gain) key) {
            if (FilterSettings.ContainsKey(key)) {
                return FilterSettings[key];
            }

            return null;
        }

        public IEnumerable<BinningMode> GetBrightnessInfoBinnings() {
            var result = FilterSettings.Keys.Select(key => key.binning).ToList();

            return result.Distinct();
        }

        public IEnumerable<short> GetBrightnessInfoGains() {
            var result = FilterSettings.Keys.Select(key => key.gain).ToList();

            return result.Distinct();
        }
    }
}