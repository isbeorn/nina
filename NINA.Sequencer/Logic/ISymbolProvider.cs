using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Logic
{
    public interface ISymbolProvider {
        event EventHandler<SymbolChangedEventArgs> SymbolAdded;

        event EventHandler<SymbolChangedEventArgs> SymbolUpdated;

        event EventHandler<SymbolChangedEventArgs> SymbolRemoved;

        public void AddOrUpdateSymbol(string token, object value);

        public void AddOrUpdateSymbol(string token, object value, Symbol[] values);

        public void AddOrUpdateHiddenSymbol(string token, object value, Symbol[] values);

        public bool RemoveSymbol(string token);

        public string GetProviderName();
        public void RegisterFunction(SymbolFunction function);
    }
}
