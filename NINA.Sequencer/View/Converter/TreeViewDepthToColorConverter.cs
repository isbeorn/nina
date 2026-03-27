#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.View.Sequencer.Converter {

    public class TreeViewDepthToColorConverter : IValueConverter {
        private static readonly Brush[] _colorPalette = new Brush[] {
            new SolidColorBrush(Color.FromRgb(91, 143, 191)),   // Blue
            new SolidColorBrush(Color.FromRgb(88, 168, 120)),   // Green
            new SolidColorBrush(Color.FromRgb(142, 110, 175)),  // Purple
            new SolidColorBrush(Color.FromRgb(184, 156, 62)),   // Yellow
            new SolidColorBrush(Color.FromRgb(188, 122, 68)),   // Orange
            new SolidColorBrush(Color.FromRgb(180, 90, 82)),    // Red
            new SolidColorBrush(Color.FromRgb(64, 152, 138)),   // Teal
            new SolidColorBrush(Color.FromRgb(120, 138, 140))   // Gray
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is TreeViewItem item) {
                int depth = GetDepth(item);
                return _colorPalette[depth % _colorPalette.Length];
            }
            return _colorPalette[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        private int GetDepth(TreeViewItem item) {
            int depth = 0;
            var parent = ItemsControl.ItemsControlFromItemContainer(item);
            while (parent != null && parent is TreeViewItem) {
                depth++;
                parent = ItemsControl.ItemsControlFromItemContainer(parent);
            }
            return depth;
        }
    }
}
