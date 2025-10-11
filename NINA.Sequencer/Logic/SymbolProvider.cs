using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Logic {

    public class SymbolProvider : ISymbolProvider {

        private string friendlyName;
        public string code;
        ISymbolBroker broker;

        public SymbolProvider(string friendlyName, string code, ISymbolBroker broker) {
            this.friendlyName = friendlyName;
            this.code = code;
            this.broker = broker;
        }

        public string FriendlyName => friendlyName;

        public string GetProviderCode() {
            return code;
        }

        public string GetProviderFriendlyName() {
            return friendlyName;
        }

        // Allow constants to be added at some point (like CoverStatus, PierSide)
        public void AddOrUpdateSymbol(string name, object value) {
            broker.AddOrUpdateSymbol(this, name, value);
        }

        public void AddOrUpdateSymbol(string name, object value, Symbol[] values) {
            broker.AddOrUpdateSymbol(this, name, value, values);
        }

        public bool RemoveSymbol(string name) {
            return broker.RemoveSymbol(this, name);
        }
    }
}
