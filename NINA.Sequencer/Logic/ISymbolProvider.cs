using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Logic
{
    public interface ISymbolProvider {
        public void AddOrUpdateSymbol(string name, object value);

        public void AddOrUpdateSymbol(string name, object value, Symbol[] values);

        public bool RemoveSymbol(string name);

        public string GetProviderCode();

        public string GetProviderFriendlyName();
    }
}
