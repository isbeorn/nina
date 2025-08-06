#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace NINA.Sequencer.Logic {

    public class ValConverter : IMultiValueConverter {

        private const int VALUE_EXPR = 0;              // The expression to be evaluated
        private const int VALUE_ERR = 1;
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            Expression expr = value[VALUE_EXPR] as Expression;
            if (expr != null && expr.Error == null && expr.Value != 0) {
                return new SolidColorBrush(Colors.GreenYellow);
            }
            if (expr != null && expr.Error != null && expr.Error.StartsWith("*")) {
                return new SolidColorBrush(Colors.Yellow);
            }
            return new SolidColorBrush(Colors.Orange);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}