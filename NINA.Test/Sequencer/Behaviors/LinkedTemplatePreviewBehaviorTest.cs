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
using NINA.Sequencer.Behaviors;
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
        public void ApplyPreviewState_ReadOnlySuppressesOnlyMaterializedChildHitTesting() {
            LinkedTemplatePreviewBehavior sut = new LinkedTemplatePreviewBehavior();
            TreeView treeView = new TreeView {
                Width = 200,
                Height = 200
            };
            TreeViewItem linkedTemplateItem = new TreeViewItem();
            linkedTemplateItem.Items.Add("Materialized template");
            linkedTemplateItem.IsExpanded = true;
            treeView.Items.Add(linkedTemplateItem);
            treeView.Measure(new Size(200, 200));
            treeView.Arrange(new Rect(0, 0, 200, 200));
            treeView.UpdateLayout();
            linkedTemplateItem.UpdateLayout();

            TreeViewItem materializedChild = linkedTemplateItem.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
            materializedChild.Should().NotBeNull();
            DragDropBehavior dragDropBehavior = new DragDropBehavior(new Grid());
            DropIntoBehavior dropIntoBehavior = new DropIntoBehavior();
            Interaction.GetBehaviors(materializedChild).Add(dragDropBehavior);
            Interaction.GetBehaviors(materializedChild).Add(dropIntoBehavior);
            SetPrivateField(sut, "linkedTemplateTreeViewItem", linkedTemplateItem);

            InvokePrivate(sut, "ApplyPreviewState");

            materializedChild.IsHitTestVisible.Should().BeFalse();
            materializedChild.IsEnabled.Should().BeTrue();
            materializedChild.Opacity.Should().BeApproximately(0.55d, 0.001d);
            dragDropBehavior.IsEnabled.Should().BeTrue();

            sut.IsEditing = true;
            InvokePrivate(sut, "ApplyPreviewState");

            materializedChild.IsHitTestVisible.Should().BeTrue();
            materializedChild.IsEnabled.Should().BeTrue();
            materializedChild.Opacity.Should().Be(1d);
            dragDropBehavior.IsEnabled.Should().BeTrue();
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