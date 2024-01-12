#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View.Sequencer {

    /// <summary>
    /// Interaction logic for SequenceViewNew.xaml
    /// </summary>
    public partial class SequenceView : UserControl {

        public SequenceView() {
            InitializeComponent();
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        /// <summary>
        /// TreeView.OnKeyDown handler has a custom handler for shift+Tab
        /// This event handler code prevents the custom handler to allow for expected keyboard navigation inside the sequencer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift) && e.Key == Key.Tab) {
                var focusedElement = Keyboard.FocusedElement;
                var elem = focusedElement as UIElement;
                if (elem != null) {
                    elem.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                }

                e.Handled = true;
            }
        }
    }
}