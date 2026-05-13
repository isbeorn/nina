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
        private readonly object eventLock = new object();
        private EventHandler<SymbolChangedEventArgs> symbolAdded;
        private EventHandler<SymbolChangedEventArgs> symbolUpdated;
        private EventHandler<SymbolChangedEventArgs> symbolRemoved;

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

        public event EventHandler<SymbolChangedEventArgs> SymbolAdded {
            add {
                lock (eventLock) {
                    if (symbolAdded == null) {
                        broker.SymbolAdded += OnBrokerSymbolAdded;
                    }
                    symbolAdded += value;
                }
            }
            remove {
                lock (eventLock) {
                    symbolAdded -= value;
                    if (symbolAdded == null) {
                        broker.SymbolAdded -= OnBrokerSymbolAdded;
                    }
                }
            }
        }

        public event EventHandler<SymbolChangedEventArgs> SymbolUpdated {
            add {
                lock (eventLock) {
                    if (symbolUpdated == null) {
                        broker.SymbolUpdated += OnBrokerSymbolUpdated;
                    }
                    symbolUpdated += value;
                }
            }
            remove {
                lock (eventLock) {
                    symbolUpdated -= value;
                    if (symbolUpdated == null) {
                        broker.SymbolUpdated -= OnBrokerSymbolUpdated;
                    }
                }
            }
        }

        public event EventHandler<SymbolChangedEventArgs> SymbolRemoved {
            add {
                lock (eventLock) {
                    if (symbolRemoved == null) {
                        broker.SymbolRemoved += OnBrokerSymbolRemoved;
                    }
                    symbolRemoved += value;
                }
            }
            remove {
                lock (eventLock) {
                    symbolRemoved -= value;
                    if (symbolRemoved == null) {
                        broker.SymbolRemoved -= OnBrokerSymbolRemoved;
                    }
                }
            }
        }

        private bool IsMySymbol(SymbolChangedEventArgs e) {
            return string.Equals(e.ProviderName, name, StringComparison.OrdinalIgnoreCase);
        }

        private void OnBrokerSymbolAdded(object sender, SymbolChangedEventArgs e) {
            if (IsMySymbol(e)) {
                SymbolEventPublisher.Publish(symbolAdded, this, e, nameof(SymbolAdded));
            }
        }

        private void OnBrokerSymbolUpdated(object sender, SymbolChangedEventArgs e) {
            if (IsMySymbol(e)) {
                SymbolEventPublisher.Publish(symbolUpdated, this, e, nameof(SymbolUpdated));
            }
        }

        private void OnBrokerSymbolRemoved(object sender, SymbolChangedEventArgs e) {
            if (IsMySymbol(e)) {
                SymbolEventPublisher.Publish(symbolRemoved, this, e, nameof(SymbolRemoved));
            }
        }

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
