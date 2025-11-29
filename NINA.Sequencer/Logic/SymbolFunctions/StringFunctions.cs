using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NINA.Sequencer.Logic.SymbolFunctions {
    public class StringFunctions : IEnumerable<SymbolFunction> {
        private readonly ISymbolBroker symbolBroker;
        private readonly List<SymbolFunction> _all;

        public StringFunctions(ISymbolBroker symbolBroker) {
            this.symbolBroker = symbolBroker;
            _all = [
                new SymbolFunction(
                    name: "StartsWith",
                    category: "String",
                    description: "Returns whether the string starts with the specified prefix.",
                    usageExample: "StartsWith(\"hello\", \"he\")",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var prefix = Convert.ToString(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        return s.StartsWith(prefix, StringComparison.Ordinal);
                    },
                    minArgs: 2,
                    maxArgs: 2
                ),

                new SymbolFunction(
                    name: "StrLength",
                    category: "String",
                    description: "Returns the length of the given string, or -1 if the argument is not a string.",
                    usageExample: "StrLength(\"hello\")",
                    implementation: args => {
                        var v = args.Parameters[0].Evaluate();
                        return v is string s ? s.Length : -1;
                    },
                    minArgs: 1,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "StrConcat",
                    category: "String",
                    description: "Concatenates two strings.",
                    usageExample: "StrConcat(\"hello\", \" world\")",
                    implementation: args => {
                        var a = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var b = Convert.ToString(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        return string.Concat(a, b);
                    },
                    minArgs: 2,
                    maxArgs: 2
                ),

                new SymbolFunction(
                    name: "StrAtPos",
                    category: "String",
                    description: "Returns the character at the specified zero-based index in a string, or an empty string if the index is out of bounds.",
                    usageExample: "StrAtPos(\"hello\", 1)",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var idxObj = args.Parameters[1].Evaluate();
                        if (idxObj is int idx && idx >= 0 && idx < s.Length)
                            return s[idx].ToString();
                        return string.Empty;
                    },
                    minArgs: 2,
                    maxArgs: 2
                ),

                new SymbolFunction(
                    name: "Contains",
                    category: "String",
                    description: "Returns whether the string contains the specified substring.",
                    usageExample: "Contains(filterName, \"Ha\")",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var sub = Convert.ToString(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        return s?.Contains(sub, StringComparison.Ordinal) ?? false;
                    },
                    minArgs: 2,
                    maxArgs: 2
                ),

                new SymbolFunction(
                    name: "EndsWith",
                    category: "String",
                    description: "Returns whether the string ends with the specified suffix.",
                    usageExample: "EndsWith(fileName, \".fits\")",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var suffix = Convert.ToString(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        return s?.EndsWith(suffix, StringComparison.Ordinal) ?? false;
                    },
                    minArgs: 2,
                    maxArgs: 2
                ),

                new SymbolFunction(
                    name: "Substring",
                    category: "String",
                    description: "Returns a substring of the given string. Out-of-range results in an empty string.",
                    usageExample: "Substring(\"hello\", 1, 3)  // \"ell\"",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture) ?? string.Empty;
                        int start = Convert.ToInt32(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);

                        if (start < 0 || start > s.Length)
                            return string.Empty;

                        if (args.Parameters.Length == 2) {
                            // from start to end
                            return s.Substring(start);
                        } else {
                            int length = Convert.ToInt32(args.Parameters[2].Evaluate(), CultureInfo.InvariantCulture);
                            if (length < 0)
                                return string.Empty;
                            if (start + length > s.Length)
                                return string.Empty;
                            return s.Substring(start, length);
                        }
                    },
                    minArgs: 2,
                    maxArgs: 3
                ),

                new SymbolFunction(
                    name: "ToLower",
                    category: "String",
                    description: "Converts the string to lower case (invariant).",
                    usageExample: "ToLower(filterName)",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return s?.ToLowerInvariant();
                    },
                    minArgs: 1,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "ToUpper",
                    category: "String",
                    description: "Converts the string to upper case (invariant).",
                    usageExample: "toUpper(filterName)",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return s?.ToUpperInvariant();
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
