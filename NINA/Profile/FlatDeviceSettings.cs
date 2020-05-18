#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    internal class FlatDeviceSettings : Settings, IFlatDeviceSettings {

        public FlatDeviceSettings() {
            FilterSettings = new Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (FilterSettings == null) {
                FilterSettings =
                    new Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
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

        private Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> _filterSettings;

        [DataMember]
        public Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> FilterSettings {
            get => _filterSettings;
            set {
                _filterSettings = value;
                RaisePropertyChanged();
            }
        }

        public void AddBrightnessInfo(FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) {
            if (FilterSettings.ContainsKey(key)) {
                FilterSettings[key] = value;
            } else {
                FilterSettings.Add(key, value);
            }

            RaisePropertyChanged(nameof(FilterSettings));
        }

        public FlatDeviceFilterSettingsValue GetBrightnessInfo(FlatDeviceFilterSettingsKey key) {
            return FilterSettings.ContainsKey(key) ? FilterSettings[key] : null;
        }

        public IEnumerable<BinningMode> GetBrightnessInfoBinnings() {
            var result = FilterSettings.Keys.Select(key => key.Binning).ToList();

            return result.Distinct();
        }

        public IEnumerable<int> GetBrightnessInfoGains() {
            var result = FilterSettings.Keys.Select(key => key.Gain).ToList();

            return result.Distinct();
        }

        public void ClearBrightnessInfo() {
            FilterSettings =
                new Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
        }
    }

    [Serializable()]
    [DataContract]
    public class FlatDeviceFilterSettingsKey {

        [DataMember]
        public string FilterName { get; set; }

        [DataMember]
        public BinningMode Binning { get; set; }

        [DataMember]
        public int Gain { get; set; }

        public FlatDeviceFilterSettingsKey(string filterName, BinningMode binning, int gain) {
            FilterName = filterName;
            Binning = binning;
            Gain = gain;
        }

        public override bool Equals(object obj) {
            if (obj == null || this.GetType() != obj.GetType()) {
                return false;
            }

            var other = (FlatDeviceFilterSettingsKey)obj;
            switch (Binning) {
                case null when other.Binning == null:
                    return FilterName == other.FilterName && Gain == other.Gain;

                case null:
                    return false;

                default:
                    return FilterName == other.FilterName && Binning.Equals(other.Binning) && Gain == other.Gain;
            }
        }

        public override int GetHashCode() {
            //see https://en.wikipedia.org/wiki/Hash_function
            const int primeNumber = 397;
            unchecked {
                var hashCode = (FilterName != null ? FilterName.GetHashCode() : 0);
                hashCode = (hashCode * primeNumber) ^ (Binning != null ? Binning.GetHashCode() : 0);
                hashCode = (hashCode * primeNumber) ^ Gain.GetHashCode();
                return hashCode;
            }
        }
    }

    [Serializable()]
    [DataContract]
    public class FlatDeviceFilterSettingsValue {

        [DataMember]
        public double Brightness { get; set; }

        [DataMember]
        public double Time { get; set; }

        public FlatDeviceFilterSettingsValue(double brightness, double time) {
            Brightness = brightness;
            Time = time;
        }
    }
}
