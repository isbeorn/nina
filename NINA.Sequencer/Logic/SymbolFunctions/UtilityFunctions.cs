using NINA.Core.Locale;
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
                    key: "Random",
                    category: "Utility",
                    description: Loc.Instance["Lbl_SymbolFunction_Utility_Random_Description"],
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
