using NINA.Core.Locale;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NINA.Sequencer.Logic.SymbolFunctions {
    public class LogicFunctions : IEnumerable<SymbolFunction> {
        private readonly ISymbolBroker symbolBroker;
        private readonly List<SymbolFunction> _all;

        private static bool AreEqual(object left, object right) {
            if (left == null || right == null) {
                return left == null && right == null;
            }

            if (IsNumeric(left) && IsNumeric(right)) {
                return Convert.ToDouble(left, CultureInfo.InvariantCulture).Equals(Convert.ToDouble(right, CultureInfo.InvariantCulture));
            }

            return Equals(left, right);
        }

        private static bool IsNumeric(object value) {
            return value is byte or sbyte
                or short or ushort
                or int or uint
                or long or ulong
                or float or double
                or decimal;
        }

        public LogicFunctions(ISymbolBroker symbolBroker) {
            this.symbolBroker = symbolBroker;

            _all = [
                new SymbolFunction(
                    key: "In",
                    category: "Logic",
                    description: Loc.Instance["Lbl_SymbolFunction_Logic_In_Description"],
                    usageExample: "in(1 + 1, 1, 2, 3)",
                    implementation: args => {
                        var value = args.Parameters[0].Evaluate();
                        for (int i = 1; i < args.Parameters.Length; i++) {
                            if (AreEqual(value, args.Parameters[i].Evaluate()))
                                return true;
                        }
                        return false;
                    },
                    minArgs: 2, maxArgs: int.MaxValue
                ),

                new SymbolFunction(
                    key: "If",
                    category: "Logic",
                    description: Loc.Instance["Lbl_SymbolFunction_Logic_If_Description"],
                    usageExample: "if(3 % 2 = 1, 'value is true', 'value is false')",
                    implementation: args => {
                        bool condition = Convert.ToBoolean(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return condition
                            ? args.Parameters[1].Evaluate()
                            : args.Parameters[2].Evaluate();
                    },
                    minArgs: 3, maxArgs: 3
                ),

                new SymbolFunction(
                    key: "Ifs",
                    category: "Logic",
                    description: Loc.Instance["Lbl_SymbolFunction_Logic_Ifs_Description"],
                    usageExample: "ifs(foo > 50, \"bar\", foo > 75, \"baz\", \"quux\")",
                    implementation: args => {
                        int count = args.Parameters.Length;

                        if (count < 3)
                            throw new ArgumentException("ifs() requires at least 3 arguments.");

                        for (int i = 0; i < count - 1; i += 2) {
                            bool cond = Convert.ToBoolean(args.Parameters[i].Evaluate(), CultureInfo.InvariantCulture);
                            if (cond)
                                return args.Parameters[i + 1].Evaluate();
                        }

                        return args.Parameters[count - 1].Evaluate();
                    },
                    minArgs: 3, maxArgs: int.MaxValue
                ),

                new SymbolFunction(
                    key: "Defined",
                    category: "Logic",
                    description: Loc.Instance["Lbl_SymbolFunction_Logic_Defined_Description"],
                    usageExample: "Defined(\"foo\")",
                    implementation: args => {
                        var str = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return symbolBroker.TryGetValue(str, out _);
                    },
                    minArgs: 1,
                    maxArgs: 1,
                    isVolatile: true
                ),

                new SymbolFunction(
                    key: "Not",
                    category: "Logic",
                    description: Loc.Instance["Lbl_SymbolFunction_Logic_Not_Description"],
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
