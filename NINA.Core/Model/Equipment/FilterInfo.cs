#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Utility;
using System;
using System.Runtime.Serialization;

namespace NINA.Core.Model.Equipment {

    [JsonObject(MemberSerialization.OptIn)]
    [Serializable()]
    [DataContract]
    public class FilterInfo : SerializableINPC {

        public FilterInfo() {
            AutoFocusGain = -1;
            AutoFocusOffset = -1;
            AutoFocusBinning = new BinningMode(1, 1);
            AutoFocusExposureTime = -1;
        }

        private string _name;
        private int _focusOffset;
        private short _position;
        private double _autoFocusExposureTime;
        private bool _autoFocusFilter;
        private BinningMode _autoFocusBinning;
        private int _autoFocusGain;
        private int _autoFocusOffset;
        private FlatWizardFilterSettings _flatWizardFilterSettings;

        [OnDeserializing]
        private void OnDeserializing(System.Runtime.Serialization.StreamingContext c) {
            AutoFocusGain = -1;
            AutoFocusOffset = -1;
            AutoFocusBinning = new BinningMode(1, 1);
            AutoFocusExposureTime = -1;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext c) {
            if (AutoFocusExposureTime == 0) AutoFocusExposureTime = -1;
            if (AutoFocusBinning == null) AutoFocusBinning = new BinningMode(1, 1);
        }

        [DataMember(Name = nameof(_name))]
        [JsonProperty(PropertyName = nameof(_name))]
        public string Name {
            get => _name;

            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_focusOffset))]
        [JsonProperty(PropertyName = nameof(_focusOffset))]
        public int FocusOffset {
            get => _focusOffset;

            set {
                _focusOffset = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_position))]
        [JsonProperty(PropertyName = nameof(_position))]
        public short Position {
            get => _position;

            set {
                _position = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_autoFocusExposureTime))]
        [JsonProperty(PropertyName = nameof(_autoFocusExposureTime))]
        public double AutoFocusExposureTime {
            get => _autoFocusExposureTime;

            set {
                _autoFocusExposureTime = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_autoFocusFilter))]
        [JsonProperty(PropertyName = nameof(_autoFocusFilter))]
        public bool AutoFocusFilter {
            get => _autoFocusFilter;

            set {
                _autoFocusFilter = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(FlatWizardFilterSettings), IsRequired = false)]
        [JsonProperty(PropertyName = nameof(FlatWizardFilterSettings))]
        public FlatWizardFilterSettings FlatWizardFilterSettings {
            get => _flatWizardFilterSettings;
            set {
                _flatWizardFilterSettings = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_autoFocusBinning), IsRequired = false)]
        [JsonProperty(PropertyName = nameof(_autoFocusBinning))]
        public BinningMode AutoFocusBinning {
            get => _autoFocusBinning;
            set {
                _autoFocusBinning = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_autoFocusGain), IsRequired = false)]
        [JsonProperty(PropertyName = nameof(_autoFocusGain))]
        public int AutoFocusGain {
            get => _autoFocusGain;
            set {
                _autoFocusGain = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_autoFocusOffset), IsRequired = false)]
        [JsonProperty(PropertyName = nameof(_autoFocusOffset))]
        public int AutoFocusOffset {
            get => _autoFocusOffset;
            set {
                _autoFocusOffset = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo(string n, int offset, short position) {
            Name = n;
            FocusOffset = offset;
            Position = position;
            AutoFocusBinning = new BinningMode(1, 1);
            AutoFocusGain = -1;
            AutoFocusOffset = -1;
            AutoFocusExposureTime = -1;
            FlatWizardFilterSettings = new FlatWizardFilterSettings();
        }

        public FilterInfo(string n, int offset, short position, double autoFocusExposureTime, BinningMode binning, int gain, int cameraOffset) : this(n, offset, position) {
            AutoFocusBinning = binning;
            AutoFocusGain = gain;
            AutoFocusOffset = cameraOffset;
            AutoFocusExposureTime = autoFocusExposureTime;
        }

        public override string ToString() {
            return Name;
        }
    }
}