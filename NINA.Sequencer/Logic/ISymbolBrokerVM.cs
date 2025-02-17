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

    }
}
