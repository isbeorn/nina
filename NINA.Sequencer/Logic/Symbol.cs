#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Google.Protobuf.WellKnownTypes;
using NmeaParser.Gnss.Ntrip;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace NINA.Sequencer.Logic {

    public class Symbol {
        private string key;
        private object value;
        private string category;
        private Symbol[] constants;
        private bool silent;

        public Symbol(string key, object value, string category, Symbol[] constants, bool silent) {
            this.key = key;
            this.value = value;
            this.category = category;
            this.constants = constants;
            this.silent = silent;
        }

        public Symbol(string key, object value) {

            this.key = key;
            this.value = value;
            this.category = null;
        }

        public string Key { get { return key; } }
        public object Value {
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
            }
        }
        public bool Silent { get { return silent; } }
        public string Category { get { return category; } }
        public Symbol[] Constants { get { return constants; } }

        public override string ToString() {
            return $"{key} : {value}";
        }
    }

    public class AmbiguousSymbol : Symbol {

        public AmbiguousSymbol(string key, List<Symbol> symbols) : base(key, null, null, symbols.ToArray(), false) {
        }

        public Symbol[] Symbols => Constants;
    }

}
