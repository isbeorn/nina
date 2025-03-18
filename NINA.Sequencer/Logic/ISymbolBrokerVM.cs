using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Logic {
    public interface ISymbolBrokerVM : IDockableVM {

        public bool TryGetValue(string key, out object value);

        public SymbolBrokerVM.DataSource GetDataSource(string key);

        public List<SymbolBrokerVM.Datum> GetDataSymbols();

        public bool AddSymbol(SymbolBrokerVM.SymbolProvider provider, string name, object value);
        public bool RemoveSymbol(SymbolBrokerVM.SymbolProvider provider, string name);

    }
}
