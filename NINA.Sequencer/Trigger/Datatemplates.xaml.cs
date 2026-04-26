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
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger.Utility;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NINA.Sequencer.Trigger {

    [Export(typeof(ResourceDictionary))]
    public partial class Datatemplates {

        public Datatemplates() {
            InitializeComponent();
        }

        private void AddTriggerSourceButton_Click(object sender, RoutedEventArgs e) {
            OpenButtonContextMenu(sender as Button, e);
        }

        private void AddTriggerInstructionButton_Click(object sender, RoutedEventArgs e) {
            OpenButtonContextMenu(sender as Button, e);
        }

        private void MenuItemTriggerSource_Click(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem menuItem) {
                return;
            }

            if (menuItem.DataContext is not SidebarEntity entity || entity.Entity is not ISequenceTrigger trigger) {
                return;
            }

            ContextMenu contextMenu = GetOwningContextMenu(menuItem);
            if (contextMenu == null) {
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

        private void MenuItemTriggerInstruction_Click(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem menuItem) {
                return;
            }

            if (menuItem.DataContext is not SidebarEntity entity || entity.Entity is not ISequenceItem sequenceItem) {
                return;
            }

            ContextMenu contextMenu = GetOwningContextMenu(menuItem);
            if (contextMenu == null) {
                return;
            }

            IDropContainer dropContainer = contextMenu.DataContext as IDropContainer;
            if (dropContainer == null && contextMenu.PlacementTarget is FrameworkElement placementTarget) {
                dropContainer = placementTarget.DataContext as IDropContainer;
            }

            if (dropContainer == null) {
                return;
            }

            DropIntoParameters parameters = new DropIntoParameters(sequenceItem as IDroppable) {
                Position = DropTargetEnum.Center
            };

            dropContainer.DropIntoCommand.Execute(parameters);
        }

        private static void OpenButtonContextMenu(Button button, RoutedEventArgs e) {
            if (button == null || button.ContextMenu == null) {
                return;
            }

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.DataContext = button.DataContext;
            button.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private static ContextMenu GetOwningContextMenu(MenuItem menuItem) {
            DependencyObject current = menuItem;

            while (current != null) {
                if (current is ContextMenu contextMenu) {
                    return contextMenu;
                }

                current = GetParent(current);
            }

            return null;
        }

        private static DependencyObject GetParent(DependencyObject dependencyObject) {
            DependencyObject parent = LogicalTreeHelper.GetParent(dependencyObject);
            if (parent != null) {
                return parent;
            }

            if (dependencyObject is Visual) {
                return VisualTreeHelper.GetParent(dependencyObject);
            }

            return null;
        }
    }
}
