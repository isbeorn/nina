#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NCalc;
using NCalc.Handlers;
using NINA.Core.Locale;
using System;

namespace NINA.Sequencer.Logic {
    public class SymbolFunction {
        public string Key { get; }
        public string Category { get; }
        public string Description { get; }
        public string UsageExample { get; }
        public Func<FunctionArgs, object> Implementation { get; }
        public int MinArgs { get; }
        public int MaxArgs { get; }  
        public bool IsVolatile { get; }

        public SymbolFunction(
                string key,
                string category,
                string description,            
                string usageExample,
                Func<FunctionArgs, object> implementation,
                int minArgs = 0,
                int maxArgs = 0,
                bool isVolatile = false) {
            if (MinArgs > MaxArgs) throw new ArgumentException("Invalid argument configuration: MinArgs cannot be greater than MaxArgs.");

            Key = key;
            Category = category;
            Description = description;
            UsageExample = usageExample;
            Implementation = implementation;
            MinArgs = minArgs;
            MaxArgs = maxArgs == 0 ? minArgs : maxArgs;
            IsVolatile = isVolatile;
        }

        public void ValidateArgs(string name, FunctionArgs args) {
            var count = args.Parameters?.Length ?? 0;

            if (MaxArgs == 0 && count > 0) {
                throw new ArgumentException(string.Format(Loc.Instance["Lbl_SymbolFunctions_Validation_NoArguments"], name, count));
            } else if (MinArgs == 1 && MaxArgs == 1 && count != 1) {
                throw new ArgumentException(string.Format(Loc.Instance["Lbl_SymbolFunctions_Validation_ExactlyOneArgument"], name, count));
            } else if (MinArgs == MaxArgs && count != MaxArgs) {
                throw new ArgumentException(string.Format(Loc.Instance["Lbl_SymbolFunctions_Validation_ExactlyManyArguments"], name, MaxArgs, count));
            } else if (count < MinArgs || count > MaxArgs) {
                throw new ArgumentException(string.Format(Loc.Instance["Lbl_SymbolFunctions_Validation_BetweenArguments"], name, MinArgs, MaxArgs, count));
            }
        }
    }
}
