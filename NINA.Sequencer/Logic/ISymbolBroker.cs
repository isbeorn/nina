#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NCalc.Handlers;
using NINA.Equipment.Interfaces.ViewModel;
using System.Collections.Generic;

namespace NINA.Sequencer.Logic {
    public interface ISymbolBroker {
        bool TryGetValue(string key, out object value);
        bool TryGetSymbol(string key, out Symbol symbol);
        List<Symbol> GetSymbols();
        ISymbolProvider RegisterSymbolProvider(string name);
        IList<Symbol> GetHiddenSymbols(string source);
        IReadOnlyCollection<SymbolFunction> GetFunctions();
        void InvokeFunction(string name, FunctionArgs args, out object result, out bool isVolatile);
        public IReadOnlyCollection<ISymbolProvider> GetMyProviders();
    }
    internal interface ISymbolBrokerProviderApi : ISymbolBroker, IDockableVM {
        void AddOrUpdateSymbol(ISymbolProvider provider, string token, object value);
        void AddOrUpdateSymbol(ISymbolProvider provider, string token, object value, Symbol[] values);
        void AddOrUpdateHiddenSymbol(ISymbolProvider provider, string token, object value, Symbol[] values);
        bool RemoveSymbol(ISymbolProvider provider, string token);
        void RegisterFunction(ISymbolProvider provider, SymbolFunction function);
        ISymbolProvider GetInternalProvider(string name);
    }
}
