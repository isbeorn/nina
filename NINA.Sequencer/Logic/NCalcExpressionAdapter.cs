#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NCalc;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NINA.Sequencer.Logic {
    internal static class NCalcExpressionAdapter {
        // Consume literals and escaped identifiers unchanged before looking for bare qualified names.
        private static readonly Regex QualifiedNames = new(
            """
            '(?:\\.|[^'\\])*(?:'|$)
            | "(?:\\.|[^"\\])*(?:"|$)
            | \[[^\]]*(?:\]|$)
            | \{[^}]*(?:\}|$)
            | \#[^#]*(?:\#|$)
            | (?<![\w.])(?<name>[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*)(?![\w.])
            """,
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        internal static NCalc.Expression Create(string definition) {
            const ExpressionOptions options = ExpressionOptions.IgnoreCaseAtBuiltInFunctions;
            if (!definition.Contains('.')) {
                return new NCalc.Expression(definition, options);
            }

            string prefix = "__nina_qualified_";
            while (definition.Contains(prefix, StringComparison.Ordinal)) {
                prefix = "_" + prefix;
            }
            var names = new Dictionary<string, string>();
            string translated = QualifiedNames.Replace(definition, match => {
                if (!match.Groups["name"].Success) {
                    return match.Value;
                }
                string alias = prefix + names.Count;
                names.Add(alias, match.Value);
                return alias;
            });

            if (names.Count == 0) {
                return new NCalc.Expression(definition, options);
            }

            // NCalc's shared parse cache must never see a tree whose identifiers we mutate.
            // Expression retains its own evaluated instance for subsequent evaluations.
            var expression = new NCalc.Expression(translated, options | ExpressionOptions.NoCache);
            if (!expression.HasErrors()) {
                RestoreNames(expression.LogicalExpression, names);
            }
            return expression;
        }

        private static void RestoreNames(LogicalExpression expression, Dictionary<string, string> names) {
            switch (expression) {
                case Identifier identifier:
                    if (names.TryGetValue(identifier.Name, out string name)) {
                        identifier.Name = name;
                    }
                    break;
                case Function function:
                    RestoreNames(function.Identifier, names);
                    RestoreNames(function.Parameters, names);
                    break;
                case BinaryExpression binary:
                    RestoreNames(binary.LeftExpression, names);
                    RestoreNames(binary.RightExpression, names);
                    break;
                case UnaryExpression unary:
                    RestoreNames(unary.Expression, names);
                    break;
                case TernaryExpression ternary:
                    RestoreNames(ternary.LeftExpression, names);
                    RestoreNames(ternary.MiddleExpression, names);
                    RestoreNames(ternary.RightExpression, names);
                    break;
                case LogicalExpressionList list:
                    foreach (LogicalExpression item in list) {
                        RestoreNames(item, names);
                    }
                    break;
            }
        }
    }
}
