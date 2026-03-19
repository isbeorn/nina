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

        public static readonly String VALID_SYMBOL = "^[a-zA-Z_][a-zA-Z0-9_]*$";

        /// <summary>
        /// Precompiled regex for validating symbol identifiers. Use this instead of Regex.IsMatch(str, VALID_SYMBOL) for better performance.
        /// </summary>
        public static readonly Regex ValidSymbolRegex = new Regex(VALID_SYMBOL, RegexOptions.Compiled);

        internal SymbolProvider(string name, ISymbolBrokerProviderApi  broker) {
            if (name.Length == 0 || !ValidSymbolRegex.IsMatch(name)) {
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
            if (!ValidSymbolRegex.IsMatch(token)) {
                throw new ArgumentException("Invalid Symbol - " + token);
            }
           broker.AddOrUpdateSymbol(this, token, value);
        }

        public void AddOrUpdateSymbol(string token, object value, Symbol[] values) {
            if (!ValidSymbolRegex.IsMatch(token)) {
                throw new ArgumentException("Invalid Symbol - " + token);
            }
            broker.AddOrUpdateSymbol(this, token, value, values);
        }

        public void AddOrUpdateHiddenSymbol(string token, object value, Symbol[] values) {
            if (!ValidSymbolRegex.IsMatch(token)) {
                throw new ArgumentException("Invalid Symbol - " + token);
            }
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
