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

        private Sequence2VM _sequencer;

        public AdvancedSequencerView() {
            InitializeComponent();
        }

        public void ShowSymbols(object sender, RoutedEventArgs e) {
            Sequence2VM vm = ((FrameworkElement)sender).DataContext as Sequence2VM;
            if (vm != null) {
                _sequencer = vm;
                // Yeah, this shouldn't reference SymbolBrokerVM directly...
                vm.DataSymbols = vm.SymbolBroker.GetSymbols();
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
            // Display the constants for this Datum
            var item = sender as ListViewItem;
            if (item != null) {
                if (item.DataContext is Symbol d && d.Constants != null) {
                    StringBuilder sb = new StringBuilder("Options: ");
                    Symbol[] cList = d.Constants;
                    for (int i = 0; i < cList.Length; i++) {
                        sb.Append(cList[i].Key);
                        if (i != cList.Length - 1) {
                            sb.Append("; ");
                        }
                    }
                    item.ToolTip = sb.ToString();
                }
            }
        }

        private void GroupItem_SetToolTip(object sender, ToolTipEventArgs e) {
            // Display the constants for this Datum
            var item = sender as GroupItem;
            if (item != null) {
                CollectionViewGroup group = (CollectionViewGroup)item.DataContext;
                string name = (string)group.Name;
                // Get list of hidden values
                if (_sequencer != null) {
                    // Yeah, this shouldn't reference SymbolBrokerVM directly...
                    IList<Symbol> syms = _sequencer.SymbolBroker.GetHiddenSymbols(name);
                    if (syms == null || syms.Count == 0) {
                        item.ToolTip = "No other data for " + name;
                        return;
                    }
                    StringBuilder sb = new StringBuilder("Also: ");
                    for (int i = 0; i < syms.Count; i++) {
                        sb.Append(syms[i].Key);
                        sb.Append("=");
                        sb.Append(syms[i].Value);
                        if (i != syms.Count - 1) {
                            sb.Append("; ");
                        }
                    }
                    item.ToolTip = sb.ToString();
                }
            }
        }
    }
}
