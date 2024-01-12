#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable]
    [DataContract]
    public class FlatDeviceSettings : Settings, IFlatDeviceSettings {

        public FlatDeviceSettings() {
            FilterSettings = new ObservableDictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
            TrainedFlatExposureSettings = new ObserveAllCollection<TrainedFlatExposureSetting>();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (TrainedFlatExposureSettings == null) {
                TrainedFlatExposureSettings = new ObserveAllCollection<TrainedFlatExposureSetting>();
            }
            if (FilterSettings == null) {
                FilterSettings =
                    new ObservableDictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
            } else if (FilterSettings.Count > 0 && TrainedFlatExposureSettings.Count == 0) {
                // Migrate to new list
                foreach (var setting in FilterSettings) {
                    if (setting.Key.Position == null && !string.IsNullOrEmpty(setting.Key.FilterName)) {
                        // Skip entries from versions prior to 2.0 that don't have a position for a filter name
                        continue;
                    }
                    AddTrainedFlatExposureSetting(setting.Key.Position, setting.Key.Binning, setting.Key.Gain, -1, setting.Value.AbsoluteBrightness, setting.Value.Time);
                }
            }
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
        }

        private string id;

        [DataMember]
        public string Id {
            get => id;
            set {
                if (id == value) return;
                id = value;
                RaisePropertyChanged();
            }
        }

        private string name;

        [DataMember]
        public string Name {
            get => name;
            set {
                if (name == value) return;
                name = value;
                RaisePropertyChanged();
            }
        }

        private string portName;

        [DataMember]
        public string PortName {
            get => portName;
            set {
                if (portName == value) return;
                portName = value;
                RaisePropertyChanged();
            }
        }

        private int settleTime;

        [DataMember]
        public int SettleTime {
            get => settleTime;
            set {
                if (settleTime == value) return;
                settleTime = value;
                RaisePropertyChanged();
            }
        }

        private ObservableDictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> filterSettings;

        [DataMember]
        [Obsolete("Superseded by TrainedFlatExposureSettings")]
        internal ObservableDictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> FilterSettings {
            get => filterSettings;
            set {
                if (filterSettings == value) return;
                filterSettings = value;
                RaisePropertyChanged();

            }
        }

        private ObserveAllCollection<TrainedFlatExposureSetting> trainedFlatExposureSettings;
        [DataMember]
        public ObserveAllCollection<TrainedFlatExposureSetting> TrainedFlatExposureSettings {
            get => trainedFlatExposureSettings;
            set {
                if (trainedFlatExposureSettings == value) return;
                if (trainedFlatExposureSettings != null) {
                    trainedFlatExposureSettings.CollectionChanged -= TrainedFlatExposureSettings_CollectionChanged;
                }
                trainedFlatExposureSettings = value;
                if (trainedFlatExposureSettings != null) {
                    trainedFlatExposureSettings.CollectionChanged += TrainedFlatExposureSettings_CollectionChanged;
                }
                RaisePropertyChanged();
            }
        }
        private void TrainedFlatExposureSettings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            RaisePropertyChanged(nameof(TrainedFlatExposureSettings));
        }

        public void AddEmptyTrainedExposureSetting() {
            TrainedFlatExposureSettings.Add(new TrainedFlatExposureSetting(-1, new BinningMode(1,1), -1, -1, -1, -1));
        }

        public void AddTrainedFlatExposureSetting(short? filterPosition, BinningMode binning, int gain, int offset, int brightness, double exposureTime) {
            var existingSetting = GetTrainedFlatExposureSetting(filterPosition, binning, gain, offset);           

            if (existingSetting == null) {
                var filter = filterPosition ?? -1;
                if (binning == null) { binning = new BinningMode(1, 1); }
                TrainedFlatExposureSettings.Add(new TrainedFlatExposureSetting(filter, binning, gain, offset, brightness, exposureTime));
            } else {
                existingSetting.Brightness = brightness;
                existingSetting.Time = exposureTime;
            }
        }

        public bool RemoveFlatExposureSetting(TrainedFlatExposureSetting setting) {
            var remove = TrainedFlatExposureSettings.Remove(setting);
            if(remove) {
                RaisePropertyChanged(nameof(TrainedFlatExposureSettings));
            }
            return remove;
        }

        public TrainedFlatExposureSetting GetTrainedFlatExposureSetting(short? filterPosition, BinningMode binning, int gain, int offset) {
            var filter = filterPosition ?? -1;
            if (binning == null) { binning = new BinningMode(1, 1); }
            TrainedFlatExposureSetting setting = TrainedFlatExposureSettings.FirstOrDefault(
                x => x.Filter == filter
                && x.Binning.X == binning.X
                && x.Binning.Y == binning.Y
                && x.Gain == gain
                && x.Offset == offset);

            if (setting == null) {
                // Check for an entry without offset
                setting = TrainedFlatExposureSettings.FirstOrDefault(
                x => x.Filter == filter
                && x.Binning.X == binning.X
                && x.Binning.Y == binning.Y
                && x.Gain == gain
                && x.Offset == -1);
            }


            if (setting == null) {
                // Check for an entry without gain
                setting = TrainedFlatExposureSettings.FirstOrDefault(
                x => x.Filter == filter
                && x.Binning.X == binning.X
                && x.Binning.Y == binning.Y
                && x.Gain == -1
                && x.Offset == offset);
            }

            if (setting == null) {
                // Check for an entry without gain or offset
                setting = TrainedFlatExposureSettings.FirstOrDefault(
                x => x.Filter == filter
                && x.Binning.X == binning.X
                && x.Binning.Y == binning.Y
                && x.Gain == -1
                && x.Offset == -1);
            }

            return setting;
        }

    }

    [Serializable]
    [DataContract]
    public class TrainedFlatExposureSetting : SerializableINPC {

        public TrainedFlatExposureSetting() {
            Filter = -1;
            Binning = new BinningMode(1, 1);
            Gain = -1;
            Offset = -1;
            Brightness = 0;
            Time = 0;
        }
        public TrainedFlatExposureSetting(short filter, BinningMode binning, int gain, int offset, int brightness, double time) {
            Filter = filter;
            Binning = binning;
            Gain = gain;
            Offset = offset;
            Brightness = brightness;
            Time = time;
        }

        private short filter;
        [DataMember]
        public short Filter {
            get => filter;
            set {
                if (filter == value) return;
                filter = value;
                RaisePropertyChanged();
            }
        }

        private BinningMode binning;
        [DataMember]
        public BinningMode Binning {
            get => binning;
            set {
                if (binning == value) return;
                binning = value;
                RaisePropertyChanged();
            }
        }

        private int gain;
        [DataMember]
        public int Gain {
            get => gain;
            set {
                if (gain == value) return;
                gain = value;
                RaisePropertyChanged();
            }
        }

        private int offset;
        [DataMember]
        public int Offset {
            get => offset;
            set {
                if (offset == value) return;
                offset = value;
                RaisePropertyChanged();
            }
        }

        private int brightness;
        [DataMember]
        public int Brightness {
            get => brightness;
            set {
                if(value < 0) { value = 0; }
                if (brightness == value) return;
                brightness = value;
                RaisePropertyChanged();
            }
        }

        private double time;
        [DataMember]
        public double Time {
            get => time;
            set {
                if (value < 0) { value = 0; }
                if (time == value) return;
                time = value;
                RaisePropertyChanged();
            }
        }
    }

    #region Obsolete

    [Serializable]
    [DataContract]
    [Obsolete]
    public class FlatDeviceFilterSettingsKey {

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string FilterName { get; set; }

        [DataMember]
        public short? Position { get; set; }

        [DataMember]
        public BinningMode Binning { get; set; }

        [DataMember]
        public int Gain { get; set; }

        public FlatDeviceFilterSettingsKey(short? position, BinningMode binning, int gain) {
            Position = position;
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
                    return Position == other.Position && Gain == other.Gain;

                case null:
                    return false;

                default:
                    return Position == other.Position && Binning.Equals(other.Binning) && Gain == other.Gain;
            }
        }

        public override int GetHashCode() {
            //see https://en.wikipedia.org/wiki/Hash_function
            const int primeNumber = 397;
            unchecked {
                var hashCode = Position != null ? Position.GetHashCode() : 0;
                hashCode = (hashCode * primeNumber) ^ (Binning != null ? Binning.GetHashCode() : 0);
                hashCode = (hashCode * primeNumber) ^ Gain.GetHashCode();
                return hashCode;
            }
        }
    }

    [Serializable]
    [DataContract]
    [Obsolete]
    public class FlatDeviceFilterSettingsValue {

        [Obsolete]
        [DataMember]
        public double Brightness { get; set; } = double.NaN;

        [DataMember]
        public int AbsoluteBrightness { get; set; }

        [DataMember]
        public double Time { get; set; }

        public FlatDeviceFilterSettingsValue(int brightness, double time) {
            AbsoluteBrightness = brightness;
            Time = time;
        }
    }

    #endregion
}