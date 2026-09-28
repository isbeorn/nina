#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Microsoft.Xaml.Behaviors;
using NINA.CustomControlLibrary;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.Container;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;

namespace NINA.Test.Sequencer.Behaviors {

    [TestFixture, NonParallelizable]
    public class LinkedTemplatePreviewBehaviorTest {

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ApplyPreviewState_ReadOnlyKeepsMaterializedContainersExpandableButSuppressesEdits() {
            LinkedTemplatePreviewBehavior sut = new LinkedTemplatePreviewBehavior();
            TreeView treeView = new TreeView {
                Width = 200,
                Height = 200
            };
            TreeViewItem linkedTemplateItem = new TreeViewItem();
            TreeViewItem materializedContainer = new TreeViewItem {
                DataContext = new SequentialContainer()
            };
            TreeViewItem materializedInstruction = new TreeViewItem {
                DataContext = new object(),
                Header = "Instruction"
            };
            Button editButton = new Button();
            TextBox editableName = new TextBox();
            Border dropSurface = new Border();
            DragDropBehavior dragDropBehavior = new DragDropBehavior(new Grid());
            DragOverBehavior dragOverBehavior = new DragOverBehavior(new Grid());
            DropIntoBehavior dropIntoBehavior = new DropIntoBehavior();
            Interaction.GetBehaviors(dropSurface).Add(dragDropBehavior);
            Interaction.GetBehaviors(dropSurface).Add(dragOverBehavior);
            Interaction.GetBehaviors(dropSurface).Add(dropIntoBehavior);
            materializedContainer.Header = new DetachingExpander {
                Header = new StackPanel {
                    Children = {
                        editableName,
                        editButton
                    }
                },
                Content = dropSurface,
                IsExpanded = true
            };
            linkedTemplateItem.Items.Add(materializedContainer);
            linkedTemplateItem.Items.Add(materializedInstruction);
            linkedTemplateItem.IsExpanded = true;
            treeView.Items.Add(linkedTemplateItem);
            treeView.Measure(new Size(200, 200));
            treeView.Arrange(new Rect(0, 0, 200, 200));
            treeView.UpdateLayout();
            linkedTemplateItem.UpdateLayout();

            SetPrivateField(sut, "linkedTemplateTreeViewItem", linkedTemplateItem);

            InvokePrivate(sut, "ApplyPreviewState");

            materializedContainer.IsHitTestVisible.Should().BeTrue();
            materializedContainer.IsEnabled.Should().BeTrue();
            materializedContainer.Opacity.Should().BeApproximately(0.75d, 0.001d);
            materializedInstruction.IsHitTestVisible.Should().BeFalse();
            editableName.IsHitTestVisible.Should().BeFalse();
            editButton.IsHitTestVisible.Should().BeFalse();
            dragDropBehavior.IsEnabled.Should().BeFalse();
            dragOverBehavior.Enabled.Should().BeFalse();
            dropIntoBehavior.IsEnabled.Should().BeFalse();

            sut.IsEditing = true;
            InvokePrivate(sut, "ApplyPreviewState");

            materializedContainer.IsHitTestVisible.Should().BeTrue();
            materializedContainer.IsEnabled.Should().BeTrue();
            materializedContainer.Opacity.Should().Be(1d);
            materializedInstruction.IsHitTestVisible.Should().BeTrue();
            editableName.IsHitTestVisible.Should().BeTrue();
            editButton.IsHitTestVisible.Should().BeTrue();
            dragDropBehavior.IsEnabled.Should().BeTrue();
            dragOverBehavior.Enabled.Should().BeTrue();
            dropIntoBehavior.IsEnabled.Should().BeTrue();
        }

        [TestCase(false, "Unset")]
        [TestCase(false, "True")]
        [TestCase(false, "False")]
        [TestCase(false, "Binding")]
        [TestCase(true, "Unset")]
        [TestCase(true, "True")]
        [TestCase(true, "False")]
        [TestCase(true, "Binding")]
        [Apartment(ApartmentState.STA)]
        public void ApplyPreviewState_RestoresInstructionInputState(bool fallback, string originalState) {
            TextBox parameter = new TextBox();
            Button delete = new Button();
            CheckBox source = new CheckBox { IsChecked = true };
            Binding binding = new Binding(nameof(CheckBox.IsChecked)) { Source = source };
            if (originalState == "Binding") {
                parameter.SetBinding(UIElement.IsHitTestVisibleProperty, binding);
            } else if (originalState != "Unset") {
                parameter.IsHitTestVisible = bool.Parse(originalState);
            }
            StackPanel editors = new StackPanel { Children = { parameter, delete } };
            Behavior<FrameworkElement> behavior;
            FrameworkElement host;
            FrameworkElement view;
            TreeViewItem? linked = null;
            if (fallback) {
                // RangeBase and its template's input controls are both suppressed by the fallback behavior.
                Slider slider = new Slider {
                    Template = (ControlTemplate)XamlReader.Parse(
                        "<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" TargetType=\"Slider\"><ContentPresenter x:Name=\"Editors\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" /></ControlTemplate>")
                };
                slider.ApplyTemplate();
                ((ContentPresenter)slider.Template.FindName("Editors", slider)).Content = editors;
                host = view = slider;
                behavior = new LinkedTemplateFallbackPreviewBehavior();
            } else {
                host = new Border();
                linked = new TreeViewItem { Header = host, IsExpanded = true };
                TreeViewItem container = new TreeViewItem { DataContext = new SequentialContainer(), IsExpanded = true };
                container.Items.Add(new TreeViewItem { DataContext = new object(), Header = editors });
                linked.Items.Add(container);
                TreeView tree = new TreeView();
                tree.Items.Add(linked);
                view = tree;
                behavior = new LinkedTemplatePreviewBehavior();
            }
            view.Measure(new Size(400, 300));
            view.Arrange(new Rect(0, 0, 400, 300));
            view.UpdateLayout();
            behavior.Attach(host);
            if (linked != null) SetPrivateField(behavior, "linkedTemplateTreeViewItem", linked);
            DependencyProperty editingProperty = fallback
                ? LinkedTemplateFallbackPreviewBehavior.IsEditingProperty
                : LinkedTemplatePreviewBehavior.IsEditingProperty;
            try {
                for (int i = 0; i < 2; i++) {
                    behavior.SetValue(editingProperty, false);
                    InvokePrivate(behavior, "ApplyPreviewState");
                    InvokePrivate(behavior, "ApplyPreviewState");
                    parameter.IsHitTestVisible.Should().BeFalse();
                    delete.IsHitTestVisible.Should().BeFalse();

                    behavior.SetValue(editingProperty, true);
                    InvokePrivate(behavior, "ApplyPreviewState");
                    parameter.IsHitTestVisible.Should().Be(originalState != "False");
                    delete.IsHitTestVisible.Should().BeTrue();
                    if (originalState == "Unset") {
                        parameter.ReadLocalValue(UIElement.IsHitTestVisibleProperty).Should().BeSameAs(DependencyProperty.UnsetValue);
                    } else if (originalState == "Binding") {
                        BindingOperations.GetBindingBase(parameter, UIElement.IsHitTestVisibleProperty).Should().BeSameAs(binding);
                        source.IsChecked = false;
                        parameter.IsHitTestVisible.Should().BeFalse();
                        source.IsChecked = true;
                        parameter.IsHitTestVisible.Should().BeTrue();
                    } else {
                        parameter.ReadLocalValue(UIElement.IsHitTestVisibleProperty).Should().Be(bool.Parse(originalState));
                    }
                    host.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                    host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                    DrainDispatcher();
                }
            } finally {
                behavior.Detach();
                DrainDispatcher();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ApplyPreviewState_NestedReadOnlyLinkedTemplatesDoNotStackOpacity() {
            LinkedTemplatePreviewBehavior outerBehavior = new LinkedTemplatePreviewBehavior();
            LinkedTemplatePreviewBehavior nestedBehavior = new LinkedTemplatePreviewBehavior();
            TreeView treeView = new TreeView {
                Width = 300,
                Height = 300
            };
            TreeViewItem outerLinkedTemplate = new TreeViewItem {
                DataContext = new LinkedTemplateContainer()
            };
            TreeViewItem outerMaterializedContainer = new TreeViewItem {
                DataContext = new SequentialContainer()
            };
            TreeViewItem nestedLinkedTemplate = new TreeViewItem {
                DataContext = new LinkedTemplateContainer()
            };
            TreeViewItem nestedMaterializedContainer = new TreeViewItem {
                DataContext = new SequentialContainer()
            };
            nestedLinkedTemplate.Items.Add(nestedMaterializedContainer);
            outerMaterializedContainer.Items.Add(nestedLinkedTemplate);
            outerLinkedTemplate.Items.Add(outerMaterializedContainer);
            outerLinkedTemplate.IsExpanded = true;
            outerMaterializedContainer.IsExpanded = true;
            nestedLinkedTemplate.IsExpanded = true;
            treeView.Items.Add(outerLinkedTemplate);
            treeView.Measure(new Size(300, 300));
            treeView.Arrange(new Rect(0, 0, 300, 300));
            treeView.UpdateLayout();
            outerLinkedTemplate.UpdateLayout();
            outerMaterializedContainer.UpdateLayout();
            nestedLinkedTemplate.UpdateLayout();

            SetPrivateField(outerBehavior, "linkedTemplateTreeViewItem", outerLinkedTemplate);
            SetPrivateField(nestedBehavior, "linkedTemplateTreeViewItem", nestedLinkedTemplate);

            InvokePrivate(outerBehavior, "ApplyPreviewState");
            InvokePrivate(nestedBehavior, "ApplyPreviewState");

            outerMaterializedContainer.Opacity.Should().BeApproximately(0.75d, 0.001d);
            nestedLinkedTemplate.Opacity.Should().Be(1d);
            nestedMaterializedContainer.Opacity.Should().Be(1d);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task DetachFromAssociatedObject_MarshalsCleanupToAssociatedDispatcher() {
            LinkedTemplatePreviewBehavior sut = new LinkedTemplatePreviewBehavior();
            FrameworkElement associatedObject = new FrameworkElement();
            sut.Attach(associatedObject);

            await Task.Run(() => InvokePrivate(sut, "DetachFromAssociatedObject", associatedObject))
                .WaitAsync(TimeSpan.FromSeconds(5));
            DrainDispatcher();

            sut.Detach();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void FallbackVisibility_RemainsVisibleOutsideTreeViewHosts() {
            LinkedTemplateFallbackVisibilityBehavior sut = new LinkedTemplateFallbackVisibilityBehavior();
            LinkedTemplateContainer linkedTemplate = new LinkedTemplateContainer();
            ItemsControl fallbackHost = new ItemsControl {
                DataContext = linkedTemplate,
                Visibility = Visibility.Collapsed
            };
            Border nonTreeHost = new Border {
                Child = fallbackHost
            };

            sut.Attach(fallbackHost);

            InvokePrivate(sut, "UpdateFallbackVisibility");

            fallbackHost.Visibility.Should().Be(Visibility.Visible);
            nonTreeHost.Child.Should().BeSameAs(fallbackHost);
            sut.Detach();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void FallbackVisibility_HidesWhenTreeViewParentAppearsAfterInitialCheck() {
            LinkedTemplateFallbackVisibilityBehavior sut = new LinkedTemplateFallbackVisibilityBehavior();
            LinkedTemplateContainer linkedTemplate = new LinkedTemplateContainer();
            ItemsControl fallbackHost = new ItemsControl {
                DataContext = linkedTemplate,
                Visibility = Visibility.Visible
            };
            TreeView treeView = new TreeView {
                Width = 200,
                Height = 200
            };

            sut.Attach(fallbackHost);
            InvokePrivate(sut, "UpdateFallbackVisibility");
            fallbackHost.Visibility.Should().Be(Visibility.Visible);

            Border header = new Border {
                Child = fallbackHost
            };
            TreeViewItem linkedTemplateItem = new TreeViewItem {
                DataContext = linkedTemplate,
                Header = header,
                IsExpanded = true
            };
            treeView.Items.Add(linkedTemplateItem);
            treeView.Measure(new Size(200, 200));
            treeView.Arrange(new Rect(0, 0, 200, 200));
            treeView.UpdateLayout();
            linkedTemplateItem.UpdateLayout();
            DrainDispatcher();

            InvokePrivate(sut, "UpdateFallbackVisibility");

            fallbackHost.Visibility.Should().Be(Visibility.Collapsed);
            sut.Detach();
        }

        private static void SetPrivateField(object target, string fieldName, object value) {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.Should().NotBeNull();
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName) {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            method.Invoke(target, null);
        }

        private static void InvokePrivate(object target, string methodName, params object[] args) {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            method.Invoke(target, args);
        }

        private static void DrainDispatcher() {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }
}
