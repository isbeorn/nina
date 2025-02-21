#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Sequencer.SequenceItem.FilterWheel {

    [Export(typeof(ResourceDictionary))]
    public partial class Datatemplates : ResourceDictionary {

        public Datatemplates() {
            InitializeComponent();
        }

        public void FilterSelectionChanged(object sender, SelectionChangedEventArgs e) {
            Logger.Info("Foo");
            ComboBox cb = (ComboBox)sender;
            if (cb.DataContext is SwitchFilter sw && cb.SelectedIndex >= 0) {
                sw.SelectedFilter = cb.SelectedIndex;
            }
        }

        public void ComboBoxLostFocus(object sender, RoutedEventArgs args) {

        }
    }
}
