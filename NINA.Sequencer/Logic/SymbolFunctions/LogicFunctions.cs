using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NINA.Sequencer.Logic.SymbolFunctions {
    public class LogicFunctions : IEnumerable<SymbolFunction> {
        private readonly ISymbolBroker symbolBroker;
        private readonly List<SymbolFunction> _all;

        public LogicFunctions(ISymbolBroker symbolBroker) {
            this.symbolBroker = symbolBroker;

            _all = [
                new SymbolFunction(
                    name: "In",
                    category: "Logic",
                    description: "Returns whether an element is in a set of values.",
                    usageExample: "in(1 + 1, 1, 2, 3)",
                    implementation: args => {
                        var value = args.Parameters[0].Evaluate();
                        for (int i = 1; i < args.Parameters.Length; i++) {
                            if (Equals(value, args.Parameters[i].Evaluate()))
                                return true;
                        }
                        return false;
                    },
                    minArgs: 2, maxArgs: int.MaxValue),

                new SymbolFunction(
                    name: "If",
                    category: "Logic",
                    description: "Returns a value based on a condition.",
                    usageExample: "if(3 % 2 = 1, 'value is true', 'value is false')",
                    implementation: args => {
                        bool condition = Convert.ToBoolean(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return condition
                            ? args.Parameters[1].Evaluate()
                            : args.Parameters[2].Evaluate();
                    },
                    minArgs: 3, maxArgs: 3),

                new SymbolFunction(
                    name: "Ifs",
                    category: "Logic",
                    description: "Returns a value based on evaluating a number of conditions, with a default if none are true.",
                    usageExample: "ifs(foo > 50, \"bar\", foo > 75, \"baz\", \"quux\")",
                    implementation: args => {
                        int count = args.Parameters.Length;

                        // at least condition, value, default
                        if (count < 3)
                            throw new ArgumentException("ifs() requires at least 3 arguments.");

                        // all but last are (condition, value) pairs
                        for (int i = 0; i < count - 1; i += 2) {
                            bool cond = Convert.ToBoolean(args.Parameters[i].Evaluate(), CultureInfo.InvariantCulture);
                            if (cond)
                                return args.Parameters[i + 1].Evaluate();
                        }

                        // default value (last argument)
                        return args.Parameters[count - 1].Evaluate();
                    },
                    minArgs: 3, maxArgs: int.MaxValue),

                new SymbolFunction(
                name: "Defined",
                category: "Logic",
                description: "Returns whether a symbol name is defined in the symbol table.",
                usageExample: "Defined(\"foo\")",
                implementation: args => {
                    var str = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                    return symbolBroker.TryGetValue(str, out _);
                },
                minArgs: 1,
                maxArgs: 1,
                isVolatile: true), // depends on symbol table

                new SymbolFunction(
                    name: "Not",
                    category: "Logic",
                    description: "Returns the logical negation of a boolean value.",
                    usageExample: "Not(cloudy)",
                    implementation: args => {
                        bool v = Convert.ToBoolean(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return !v;
                    },
                    minArgs: 1,
                    maxArgs: 1
                )
            ];
        }

        public IEnumerator<SymbolFunction> GetEnumerator() => _all.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
