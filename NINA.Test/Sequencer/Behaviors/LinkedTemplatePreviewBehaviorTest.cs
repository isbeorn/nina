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
using System.Windows.Threading;

namespace NINA.Test.Sequencer.Behaviors {

    [TestFixture]
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
