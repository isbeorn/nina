using NINA.Sequencer.SequenceItem.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NINA.Sequencer.Logic {

    public class SymbolProvider : ISymbolProvider {

        private string name;
        ISymbolBrokerProviderApi  broker;

        public static readonly String VALID_SYMBOL = "^[a-zA-Z][a-zA-Z0-9-+_]*$";

        internal SymbolProvider(string name, ISymbolBrokerProviderApi  broker) {
            if (name.Length == 0 || !Regex.IsMatch(name, UserSymbol.VALID_SYMBOL)) {
                throw new ArgumentException("SymbolProvider name must be an alphanumeric word.");
            }
            this.name = name;
            this.broker = broker;
        }

        public string Name => name;

        public string GetProviderName() {
            return Name;
        }

        // Allow constants to be added at some point (like CoverStatus, PierSide)
        public void AddOrUpdateSymbol(string token, object value) {
            broker.AddOrUpdateSymbol(this, token, value);
        }

        public void AddOrUpdateSymbol(string token, object value, Symbol[] values) {
            broker.AddOrUpdateSymbol(this, token, value, values);
        }

        public void AddOrUpdateHiddenSymbol(string token, object value, Symbol[] values) {
            broker.AddOrUpdateHiddenSymbol(this, token, value, values);
        }

        public bool RemoveSymbol(string token) {
            return broker.RemoveSymbol(this, token);
        }

        public void RegisterFunction(SymbolFunction function) {
            broker.RegisterFunction(this, function);
        }
    }
}
