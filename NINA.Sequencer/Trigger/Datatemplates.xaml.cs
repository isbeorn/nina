#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Sequencer;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Trigger.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Sequencer.Trigger {

    [Export(typeof(ResourceDictionary))]
    public partial class Datatemplates {

        public Datatemplates() {
            InitializeComponent();
        }

        private void AddTriggerSourceButton_Click(object sender, RoutedEventArgs e) {
            if (sender is not Button button || button.ContextMenu == null) {
                return;
            }

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.DataContext = button.DataContext;
            button.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void MenuItemTriggerSource_Click(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem menuItem) {
                return;
            }

            if (menuItem.DataContext is not SidebarEntity entity || entity.Entity is not ISequenceTrigger trigger) {
                return;
            }

            if (ItemsControl.ItemsControlFromItemContainer(menuItem) is not ContextMenu contextMenu) {
                return;
            }

            CustomTrigger customTrigger = contextMenu.DataContext as CustomTrigger;
            if (customTrigger == null && contextMenu.PlacementTarget is FrameworkElement placementTarget) {
                customTrigger = placementTarget.DataContext as CustomTrigger;
            }

            if (customTrigger == null) {
                return;
            }

            DropIntoParameters parameters = new DropIntoParameters(trigger) {
                Position = DropTargetEnum.Center
            };

            customTrigger.DropIntoTriggerSourceCommand.Execute(parameters);
        }
    }
}
