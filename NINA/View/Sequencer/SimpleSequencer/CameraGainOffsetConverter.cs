#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Logic;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Expression = NINA.Sequencer.Logic.Expression;

namespace NINA.View.SimpleSequencer {

    /// <summary>
    /// Local copy of the Core converter so this assembly can reference Sequencer types (e.g. Expression).
    /// </summary>
    public sealed class CameraGainOffsetConverter : IMultiValueConverter {

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            // Put ?? on screen so we'll be alerted there's a bug...
            if (value[0] == DependencyProperty.UnsetValue) {
                return "(??)";
            }

            Expression expr = value[0] as Expression;
            // The dropdown writes the numeric gain through its first binding and reads expression metadata through the second.
            if (expr == null && value[0] is int && value.Length > 1) {
                expr = value[1] as Expression;
            }
            // Shouldn't ever happen, but ...
            if (expr == null) {
                return "(??)";
            }
            
            // Two cases, if the field is empty
            if (expr.Definition.Length == 0) {
                if (!expr.IsValid) {
                    return "(" + Core.Locale.Loc.Instance["LblCamera"] + ")";
                } else {
                    return "(" + expr.Default.ToString() + ")";
                }
            }
            return expr.Value.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            object[] result = new object[targetType.Length];
            Array.Fill(result, Binding.DoNothing);
            if (value == null || value == DependencyProperty.UnsetValue || value == Binding.DoNothing
                || targetType.Length == 0 || targetType[0] != typeof(int)) {
                return result;
            }

            result[0] = int.TryParse(value.ToString(), NumberStyles.Integer, culture, out int gain) ? gain : -1;
            return result;
        }

    }
}