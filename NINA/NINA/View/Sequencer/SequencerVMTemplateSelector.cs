#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.ViewModel;
using NINA.ViewModel.Sequencer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Sequencer {

    internal class SequencerVMTemplateSelector : DataTemplateSelector {
        public DataTemplate Navigation { get; set; }
        public DataTemplate Simple { get; set; }
        public DataTemplate Advanced { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is Sequence2VM) {
                return Advanced;
            }
            if (item is SimpleSequenceVM) {
                return Simple;
            } else {
                return Navigation;
            }
        }
    }
}