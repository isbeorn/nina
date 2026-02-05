#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NINA.Sequencer.Logic {

    public class AmbiguousSymbol : Symbol {

        public AmbiguousSymbol(string key, IList<Symbol> symbols) : base(key, null, null, ((List<Symbol>)symbols).ToArray(), SymbolType.SYMBOL_NORMAL) {
        }

        public Symbol[] Symbols => Constants;
    }

    public class Symbol : BaseINPC {
        // Category of this Symbol (typically, the source - device - of the data)
        private string category;

        // Constants used with this Symbol (or null)
        private Symbol[] constants;

        // The name of the Symbol
        private string key;
        // The type of the Symbol
        private SymbolType type;

        // The Symbol's current value
        private object value;
        public Symbol(string key, object value, string category, Symbol[] constants, SymbolType type) {
            this.key = key;
            this.value = value;
            this.category = category;
            this.constants = constants;
            this.type = type;
        }

        public Symbol(string key, object value) {

            this.key = key;
            this.value = value;
            this.category = null;
        }

        public enum SymbolType { SYMBOL_NORMAL, SYMBOL_CONSTANT, SYMBOL_HIDDEN }
        public string Category { get { return category; } }
        public Symbol[] Constants { get { return constants; } }
        public string Key { get { return key; } }
        public SymbolType Type { get { return type; } }

        public object Value {
            get => value;
            set {
                this.value = value;
                RaisePropertyChanged(nameof(Value));
            }
        }
        
        // This is used by the Symbols sidebar to show a constant name, if one exists
        public object ValueName {
            get {
                if (constants == null) {
                    return value;
                } else {
                    foreach (Symbol d in constants) {
                        if (value is Int32 i1 && d.value is Int32 i2 && i1 == i2) {
                            return d.key;
                        }
                    }
                    return value;
                }
            }
            set {
                this.value = (object)value;
                RaisePropertyChanged(nameof(ValueName));
            }
        }
        public override string ToString() {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{category}_{key}: {value}"
            );
        }
    }
}
