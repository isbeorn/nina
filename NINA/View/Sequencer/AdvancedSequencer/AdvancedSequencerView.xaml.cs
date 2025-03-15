#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Logic;
using NINA.ViewModel.Sequencer;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.View.Sequencer.AdvancedSequencer {

    /// <summary>
    /// Interaction logic for AdvancedSequencerView.xaml
    /// </summary>
    public partial class AdvancedSequencerView : UserControl {

        public AdvancedSequencerView() {
            InitializeComponent();
        }

        public void ShowSymbols(object sender, RoutedEventArgs e) {
            Sequence2VM vm = ((FrameworkElement)sender).DataContext as Sequence2VM;
            if (vm != null) {
                // Yeah, this shouldn't reference SymbolBrokerVM directly...
                vm.DataSymbols = vm.SymbolBroker.GetDataSymbols();
                SymbolPopup.IsOpen = true;
            }
        }

        public void HideSymbols(object sender, RoutedEventArgs e) {
            if (SymbolPopup.IsOpen) {
                SymbolPopup.IsOpen = false;
            }
        }
        public void SymbolPopupClosed(object sender, System.EventArgs e) {
            SymbolPopup.IsOpen = false;
        }

        private void ListViewItem_SetToolTip(object sender, ToolTipEventArgs e) {
            var item = sender as ListViewItem;
            if (item != null) {
                if (item.DataContext is SymbolBrokerVM.Datum d && d.Constants != null) {
                    StringBuilder sb = new StringBuilder();
                    foreach (SymbolBrokerVM.Datum dd in d.Constants) {
                        sb.Append(dd.Key);
                        sb.Append(" ");
                    }
                    item.ToolTip = sb.ToString();
                }
            }
        }
    }
}
