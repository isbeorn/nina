using NINA.Core.Locale;
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
                    key: "StartsWith",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_StartsWith_Description"],
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
                    key: "StrLength",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_StrLength_Description"],
                    usageExample: "StrLength(\"hello\")",
                    implementation: args => {
                        var v = args.Parameters[0].Evaluate();
                        return v is string s ? s.Length : -1;
                    },
                    minArgs: 1,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    key: "StrConcat",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_StrConcat_Description"],
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
                    key: "StrAtPos",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_StrAtPos_Description"],
                    usageExample: "StrAtPos(\"hello\", 1)",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture) ?? string.Empty;
                        try {
                            int idx = Convert.ToInt32(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                            if (idx >= 0 && idx < s.Length)
                                return s[idx].ToString();
                        } catch {
                        }
                        return string.Empty;
                    },
                    minArgs: 2,
                    maxArgs: 2
                ),

                new SymbolFunction(
                    key: "Contains",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_Contains_Description"],
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
                    key: "EndsWith",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_EndsWith_Description"],
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
                    key: "Substring",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_Substring_Description"],
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
                    key: "ToLower",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_ToLower_Description"],
                    usageExample: "ToLower(filterName)",
                    implementation: args => {
                        var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return s?.ToLowerInvariant();
                    },
                    minArgs: 1,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    key: "ToUpper",
                    category: "String",
                    description: Loc.Instance["Lbl_SymbolFunction_String_ToUpper_Description"],
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
