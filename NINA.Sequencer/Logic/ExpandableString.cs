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
using NINA.Sequencer.Container;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace NINA.Sequencer.Logic {

    // Represents a string that can contain NINA expressions in the format {expression}
    // which are expanded/evaluated when the Expanded property is accessed.
    [JsonObject(MemberSerialization.OptIn)]
    public class ExpandableString : INotifyPropertyChanged {
        private string _rawValue;
        private string _expandedValue;
        private ISymbolBroker _symbolBroker;
        private ISequenceContainer _parent;

        public event PropertyChangedEventHandler PropertyChanged;

        public ExpandableString() {
        }

        public ExpandableString(string value) {
            _rawValue = value;
        }

        [JsonProperty]
        public string Value {
            get => _rawValue?.Trim();
            set {
                if (_rawValue != value) {
                    _rawValue = value?.Trim();
                    _expandedValue = null; // Invalidate cached expansion
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(Expanded));
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public string Expanded {
            get {
                if (_expandedValue != null) {
                    return _expandedValue;
                }

                string value = Value;
                if (string.IsNullOrEmpty(value)) {
                    return value;
                }

                Error = null;

                try {
                    // Use the shared ExpressionExpander logic
                    value = ExpressionExpander.Expand(value, _symbolBroker, _parent);
                } catch (InvalidOperationException ex) {
                    Error = ex.Message;
                    value = "Error";
                }

                _expandedValue = value;
                return value;
            }
        }

        public string Error { get; private set; }

        public bool HasError => Error != null;

        public void SetSymbolBroker(ISymbolBroker symbolBroker) {
            if (_symbolBroker != symbolBroker) {
                _symbolBroker = symbolBroker;
                _expandedValue = null; // Invalidate cached expansion
                OnPropertyChanged(nameof(Expanded));
            }
        }

        public void SetParent(ISequenceContainer parent) {
            if (_parent != parent) {
                _parent = parent;
                _expandedValue = null; // Invalidate cached expansion
                OnPropertyChanged(nameof(Expanded));
            }
        }

        public void Invalidate() {
            _expandedValue = null;
            OnPropertyChanged(nameof(Expanded));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(Error));
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() {
            return Value ?? string.Empty;
        }

        // Implicit conversion from string to ExpandableString
        public static implicit operator string(ExpandableString expandableString) {
            return expandableString?.Value;
        }

        // Implicit conversion from ExpandableString to string
        public static implicit operator ExpandableString(string value) {
            return new ExpandableString(value);
        }
    }
}