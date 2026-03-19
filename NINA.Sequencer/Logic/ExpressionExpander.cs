#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.SequenceItem;
using System;
using System.Text.RegularExpressions;

namespace NINA.Sequencer.Logic {
    public static class ExpressionExpander {
        private static readonly Regex ExpressionPattern = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);

        public static string Expand(string text, ISymbolBroker symbolBroker, ISequenceItem parent) {
            string value = text?.Trim();
            if (string.IsNullOrEmpty(value) || symbolBroker == null) {
                return value;
            }

            try {
                value = ExpressionPattern.Replace(value, match => {
                    string toReplace = match.Groups[1].Value;
                    Expression ex = new Expression(toReplace, parent);
                    ex.SymbolBroker = symbolBroker;
                    ex.Evaluate(true);
                    if (ex.Error != null) {
                        return "Error";
                    } else if (ex.StringValue != null) {
                        return ex.StringValue;
                    } else if (ex.Value is double doubleValue) {
                        double truncated = Math.Truncate(doubleValue * 100000) / 100000;
                        return truncated.ToString("G10");
                    } else {
                        return ex.ValueString;
                    }
                });
            } catch (InvalidOperationException) {
                value = "Error";
            }

            return value;
        }
    }
}