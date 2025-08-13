#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
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
    /// Interaction logic for SequenceRootContainerView.xaml
    /// </summary>
    public partial class SequenceRootContainerView : UserControl {

        public SequenceRootContainerView() {
            InitializeComponent();
        }

        private void MenuItemTarget_Click(object sender, RoutedEventArgs e) {
            if (sender is Control ctrl) {
                if (ctrl.DataContext is TargetSequenceContainer target) {
                    if (this.DataContext is SequenceContainer container) {
                        var p = new DropIntoParameters(target as IDroppable);
                        p.Position = DropTargetEnum.Center;
                        container.DropIntoCommand.Execute(p);
                    }
                }
            }
        }

        private void MenuItemTemplate_Click(object sender, RoutedEventArgs e) {
            if (sender is Control ctrl) {
                if (ctrl.DataContext is TemplatedSequenceContainer template) {
                    if (this.DataContext is SequenceContainer container) {
                        var p = new DropIntoParameters(template as IDroppable);
                        p.Position = DropTargetEnum.Center;
                        container.DropIntoCommand.Execute(p);
                    }
                }
            }
        }

        private void MenuItemInstruction_Click(object sender, RoutedEventArgs e) {
            if (sender is Control ctrl) {
                if(ctrl.DataContext is SidebarEntity entity) { 
                    if (entity.Entity is ISequenceItem item) {
                        if (this.DataContext is SequenceContainer container) {
                            var p = new DropIntoParameters(item as IDroppable);
                            p.Position = DropTargetEnum.Center;
                            container.DropIntoCommand.Execute(p);
                        }
                    }
                }
            }
        }

        private void MenuItemTrigger_Click(object sender, RoutedEventArgs e) {
            if (sender is Control ctrl) {
                if (ctrl.DataContext is SidebarEntity entity) {
                    if (entity.Entity is ISequenceTrigger item) {
                        if (this.DataContext is SequenceContainer container) {
                            var p = new DropIntoParameters(item as IDroppable);
                            p.Position = DropTargetEnum.Center;
                            container.DropIntoTriggersCommand.Execute(p);
                        }
                    }
                }
            }
        }

        private void MenuItemCondition_Click(object sender, RoutedEventArgs e) {
            if (sender is Control ctrl) {
                if (ctrl.DataContext is SidebarEntity entity) {
                    if (entity.Entity is ISequenceCondition item) {
                        if (this.DataContext is SequenceContainer container) {
                            var p = new DropIntoParameters(item as IDroppable);
                            p.Position = DropTargetEnum.Center;
                            container.DropIntoConditionsCommand.Execute(p);
                        }
                    }
                }
            }
        }
    }
}