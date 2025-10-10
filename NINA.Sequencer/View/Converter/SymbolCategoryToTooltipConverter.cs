#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Sequencer;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace NINA.View.Sequencer.Converter {

    public class SymbolCategoryToTooltipConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            // values[0] = group name (category), values[1] = DataContext (root VM)
            var category = values[0]?.ToString();
            if (string.IsNullOrEmpty(category) || values[1] is null) return null;

            if (values[1] is SymbolController vm) {
                var syms = vm.GetHiddenSymbols(category); 
                if (syms == null || syms.Count == 0) {
                    return string.Format(Loc.Instance["Lbl_SymbolBroker_NoCategoryData"], category);
                }

                var sb = new StringBuilder(Loc.Instance["Lbl_SymbolBroker_AdditionalData"] + " ");
                for (int i = 0; i < syms.Count; i++) {
                    sb.Append(syms[i].Key).Append('=').Append(syms[i].Value);
                    if (i < syms.Count - 1) sb.Append("; ");
                }
                return sb.ToString();
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}