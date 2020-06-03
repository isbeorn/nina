#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.ViewModel.Equipment.FlatDevice;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment.FlatDevice {

    internal class WizardGridCellTemplateSelector : DataTemplateSelector {
        public DataTemplate FilterNameCell { get; set; }
        public DataTemplate TimingCell { get; set; }
        public DataTemplate EmptyCell { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            switch (item) {
                case FilterTiming timing when timing.ShowFilterNameOnly:
                    return FilterNameCell;

                case FilterTiming timing when timing.IsEmpty:
                    return EmptyCell;

                case FilterTiming _:
                    return TimingCell;

                default:
                    return EmptyCell;
            }
        }
    }
}