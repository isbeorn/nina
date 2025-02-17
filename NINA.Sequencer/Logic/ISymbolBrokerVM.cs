using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Logic {
    public interface ISymbolBrokerVM : IDockableVM {

        public Symbol.Keys GetEquipmentKeys();
    }
}
