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

        public IEnumerable<ConcurrentDictionary<string, object>> GetEquipmentKeys();

        public SymbolBrokerVM.DataSource GetDataSource(string key);

        public List<SymbolBrokerVM.Datum> GetDataSymbols();

        public ISymbolProvider RegisterSymbolProvider(string friendlyName, string code);

        public void AddSymbol(ISymbolProvider provider, string token, object value);
        public bool RemoveSymbol(ISymbolProvider provider, string token);
    }
}
