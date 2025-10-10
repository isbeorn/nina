#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Logic;
using NINA.ViewModel.Sequencer;
using NJsonSchema.Annotations;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NINA.View.Sequencer.AdvancedSequencer {

    /// <summary>
    /// Interaction logic for AdvancedSequencerView.xaml
    /// </summary>
    public partial class AdvancedSequencerView : UserControl {

        public AdvancedSequencerView() {
            InitializeComponent();
        }
    }
}
