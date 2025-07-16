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
using NINA.Core.Utility;
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
    /// Interaction logic for SequenceContainerView.xaml
    /// </summary>
    [Obsolete("Use HierarchicalSequenceContainerView inside a Hierarchical Data Template instead!")]
    public partial class SequenceContainerView : UserControl {

        public SequenceContainerView() {
            InitializeComponent();
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        public static readonly DependencyProperty SequenceContainerContentProperty =
            DependencyProperty.Register(nameof(SequenceContainerContent), typeof(object), typeof(SequenceContainerView));

        public object SequenceContainerContent {
            get => (object)GetValue(SequenceContainerContentProperty);
            set => SetValue(SequenceContainerContentProperty, value);
        }

        public static readonly DependencyProperty ShowDetailsProperty =
            DependencyProperty.Register(nameof(ShowDetails), typeof(bool), typeof(SequenceContainerView), new PropertyMetadata(true));

        public bool ShowDetails {
            get => (bool)GetValue(ShowDetailsProperty);
            set => SetValue(ShowDetailsProperty, value);
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
                if (ctrl.DataContext is SidebarEntity entity) {
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

        private void TemplateContainerButton_Click(object sender, RoutedEventArgs e) {
            if (sender is Control ctrl) {
                if (ctrl.DataContext is ISequenceContainer container) {
                    if (Content.Resources["ViewModel"] is BindingProxy proxy) {
                        var prop = proxy.Data.GetType().GetProperty("AddTemplateCommand");

                        var value = prop.GetValue(proxy.Data) as ICommand;
                        if (value != null) {
                            var p = new DropIntoParameters(container as IDroppable);
                            p.Position = DropTargetEnum.Center;
                            value.Execute(p);
                        }
                    }
                }
            }
        }

        private void TargetContainerButton_Click(object sender, RoutedEventArgs e) {
            if (sender is Control ctrl) {
                if (ctrl.DataContext is IDeepSkyObjectContainer container) {
                    if (Content.Resources["ViewModel"] is BindingProxy proxy) {
                        var prop = proxy.Data.GetType().GetProperty("AddTargetToControllerCommand");

                        var value = prop.GetValue(proxy.Data) as ICommand;
                        if (value != null) {
                            var p = new DropIntoParameters(container as IDroppable);
                            p.Position = DropTargetEnum.Center;
                            value.Execute(p);
                        }
                    }
                }
            }
        }
    }

    internal class DeprecatedContainerStyleSelector : DataTemplateSelector {
        public DataTemplate DeepSkyObjectContainer { get; set; }
        public DataTemplate Container { get; set; }
        public DataTemplate ParallelContainer { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is SequentialContainer && item is not IImmutableContainer) {
                return Container;
            }
            if (item is ParallelContainer && item is not IImmutableContainer) {
                return ParallelContainer;
            }
            if (item is DeepSkyObjectContainer && item is not IImmutableContainer) {
                return DeepSkyObjectContainer;
            }
            return null;
        }
    }
}