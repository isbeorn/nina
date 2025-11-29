using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NINA.Sequencer.Logic.SymbolFunctions {
    public class UtilityFunctions : IEnumerable<SymbolFunction> {
        private static Random rng = new Random();
        private readonly ISymbolBroker symbolBroker;
        private readonly List<SymbolFunction> _all;

        public UtilityFunctions(ISymbolBroker symbolBroker) {
            this.symbolBroker = symbolBroker;
            _all = [
                new SymbolFunction(
                    name: "Random",
                    category: "Utility",
                    description: "Returns a random double value in the range 0.0–1.0.",
                    usageExample: "Random()",
                    implementation: args => rng.NextDouble(),
                    minArgs: 0,
                    maxArgs: 0,
                    isVolatile: true
                )
            ];
        }

        public IEnumerator<SymbolFunction> GetEnumerator() => _all.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
