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
        ISymbolBroker broker;

        public static readonly String VALID_SYMBOL = "^[a-zA-Z][a-zA-Z0-9-+_]*$";

        public SymbolProvider(string name, ISymbolBroker broker) {
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

        public bool RemoveSymbol(string token) {
            return broker.RemoveSymbol(this, token);
        }
    }
}
