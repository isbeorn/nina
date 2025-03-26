#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces.ViewModel;
using System.Collections.Generic;

namespace NINA.Sequencer.Logic {
    public interface ISymbolBrokerVM : IDockableVM {
        public bool TryGetValue(string key, out object value);
        public bool TryGetSymbol(string key, out Symbol symbol);
        public List<Symbol> GetSymbols();
        public ISymbolProvider RegisterSymbolProvider(string friendlyName, string code);
        public void AddSymbol(ISymbolProvider provider, string token, object value);
        public bool RemoveSymbol(ISymbolProvider provider, string token);
        public IList<Symbol> GetHiddenSymbols(string source);
    }
}
