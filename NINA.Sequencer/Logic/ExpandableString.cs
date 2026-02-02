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

    /// <summary>
    /// Represents a string that can contain NINA expressions in the format {expression} 
    /// which are expanded/evaluated when the Expanded property is accessed.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ExpandableString : INotifyPropertyChanged {
        private static readonly Regex ExpressionPattern = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);
        
        private string _rawValue;
        private string _expandedValue;
        private ISymbolBroker _symbolBroker;
        private ISequenceContainer _parent;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Creates a new ExpandableString with no value
        /// </summary>
        public ExpandableString() {
        }

        /// <summary>
        /// Creates a new ExpandableString with the specified raw value
        /// </summary>
        /// <param name="value">The raw string value that may contain expressions</param>
        public ExpandableString(string value) {
            _rawValue = value;
        }

        /// <summary>
        /// Gets or sets the raw string value (may contain expressions in {expression} format)
        /// </summary>
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

        /// <summary>
        /// Gets the expanded string with all expressions evaluated and replaced
        /// </summary>
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
                    value = ExpressionPattern.Replace(value, match => {
                        string toReplace = match.Groups[1].Value;
                        Expression ex = new Expression(toReplace, _parent);
                        ex.SymbolBroker = _symbolBroker;
                        ex.Evaluate(true);
                        
                        if (ex.Error != null) {
                            Error = ex.Error;
                            return "Error";
                        } else if (ex.StringValue != null) {
                            return ex.StringValue;
                        } else {
                            return ex.ValueString;
                        }
                    });
                } catch (InvalidOperationException ex) {
                    Error = ex.Message;
                    value = "Error";
                }

                _expandedValue = value;
                return value;
            }
        }

        /// <summary>
        /// Gets the last error that occurred during expression expansion, or null if no error
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Gets whether the last expansion had an error
        /// </summary>
        public bool HasError => Error != null;

        /// <summary>
        /// Sets the symbol broker used for evaluating expressions
        /// </summary>
        /// <param name="symbolBroker">The symbol broker to use</param>
        public void SetSymbolBroker(ISymbolBroker symbolBroker) {
            if (_symbolBroker != symbolBroker) {
                _symbolBroker = symbolBroker;
                _expandedValue = null; // Invalidate cached expansion
                OnPropertyChanged(nameof(Expanded));
            }
        }

        /// <summary>
        /// Sets the parent sequence container used for expression context
        /// </summary>
        /// <param name="parent">The parent container</param>
        public void SetParent(ISequenceContainer parent) {
            if (_parent != parent) {
                _parent = parent;
                _expandedValue = null; // Invalidate cached expansion
                OnPropertyChanged(nameof(Expanded));
            }
        }

        /// <summary>
        /// Invalidates the cached expanded value, forcing re-evaluation on next access
        /// </summary>
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

        /// <summary>
        /// Implicit conversion from string to ExpandableString
        /// </summary>
        public static implicit operator string(ExpandableString expandableString) {
            return expandableString?.Value;
        }

        /// <summary>
        /// Implicit conversion from ExpandableString to string
        /// </summary>
        public static implicit operator ExpandableString(string value) {
            return new ExpandableString(value);
        }
    }
}