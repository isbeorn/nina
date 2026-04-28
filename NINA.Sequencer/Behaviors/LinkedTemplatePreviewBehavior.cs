#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Sequencer.Behaviors {

    public class LinkedTemplatePreviewBehavior : Behavior<FrameworkElement> {
        private const double ReadOnlyOpacity = 0.55d;

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
            nameof(IsEditing),
            typeof(bool),
            typeof(LinkedTemplatePreviewBehavior),
            new PropertyMetadata(false, IsEditingChanged));

        private readonly List<ChildPreviewState> styledChildren = new List<ChildPreviewState>();
        private TreeViewItem linkedTemplateTreeViewItem;

        public bool IsEditing {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
            ScheduleApplyPreviewState();
        }

        protected override void OnDetaching() {
            DetachFromAssociatedObject(AssociatedObject);
            base.OnDetaching();
        }

        private void DetachFromAssociatedObject(FrameworkElement associatedObject) {
            if (associatedObject == null) {
                return;
            }

            Dispatcher dispatcher = associatedObject.Dispatcher;
            if (dispatcher == null) {
                return;
            }

            if (dispatcher.CheckAccess()) {
                DetachFromAssociatedObjectOnDispatcher(associatedObject);
                return;
            }

            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) {
                return;
            }

            try {
                dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => DetachFromAssociatedObjectOnDispatcher(associatedObject)));
            } catch (InvalidOperationException) {
            }
        }

        private void DetachFromAssociatedObjectOnDispatcher(FrameworkElement associatedObject) {
            DetachFromTreeViewItem();
            associatedObject.Loaded -= AssociatedObject_Loaded;
            associatedObject.Unloaded -= AssociatedObject_Unloaded;
        }

        private static void IsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((LinkedTemplatePreviewBehavior)d).ScheduleApplyPreviewState();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e) {
            AttachToTreeViewItem();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e) {
            DetachFromTreeViewItem();
        }

        private void AttachToTreeViewItem() {
            TreeViewItem treeViewItem = FindAncestor<TreeViewItem>(AssociatedObject);
            if (treeViewItem == null) {
                return;
            }

            if (ReferenceEquals(treeViewItem, linkedTemplateTreeViewItem)) {
                ScheduleApplyPreviewState();
                return;
            }

            DetachFromTreeViewItem();
            linkedTemplateTreeViewItem = treeViewItem;
            linkedTemplateTreeViewItem.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
            linkedTemplateTreeViewItem.ItemContainerGenerator.ItemsChanged += ItemContainerGenerator_ItemsChanged;
            ScheduleApplyPreviewState();
        }

        private void DetachFromTreeViewItem() {
            RestoreStyledChildren();
            if (linkedTemplateTreeViewItem == null) {
                return;
            }

            linkedTemplateTreeViewItem.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
            linkedTemplateTreeViewItem.ItemContainerGenerator.ItemsChanged -= ItemContainerGenerator_ItemsChanged;
            linkedTemplateTreeViewItem = null;
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e) {
            ScheduleApplyPreviewState();
        }

        private void ItemContainerGenerator_ItemsChanged(object sender, ItemsChangedEventArgs e) {
            ScheduleApplyPreviewState();
        }

        private void ScheduleApplyPreviewState() {
            FrameworkElement associatedObject = AssociatedObject;
            if (associatedObject == null) {
                return;
            }

            Dispatcher dispatcher = associatedObject.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) {
                return;
            }

            try {
                dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ApplyPreviewState));
            } catch (InvalidOperationException) {
            }
        }

        private void ApplyPreviewState() {
            if (linkedTemplateTreeViewItem == null) {
                AttachToTreeViewItem();
            }

            if (linkedTemplateTreeViewItem == null) {
                return;
            }

            RestoreStyledChildren();

            bool isEditing = IsEditing;
            for (int i = 0; i < linkedTemplateTreeViewItem.Items.Count; i++) {
                TreeViewItem child = linkedTemplateTreeViewItem.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (child == null) {
                    child = linkedTemplateTreeViewItem.ItemContainerGenerator.ContainerFromItem(linkedTemplateTreeViewItem.Items[i]) as TreeViewItem;
                }

                if (child == null) {
                    continue;
                }

                ChildPreviewState state = new ChildPreviewState(child, child.IsHitTestVisible, child.Opacity);
                styledChildren.Add(state);
                child.IsHitTestVisible = isEditing;
                child.Opacity = isEditing ? 1d : ReadOnlyOpacity;
            }
        }

        private void RestoreStyledChildren() {
            foreach (ChildPreviewState state in styledChildren) {
                state.Child.IsHitTestVisible = state.IsHitTestVisible;
                state.Child.Opacity = state.Opacity;
            }

            styledChildren.Clear();
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject {
            while (current != null) {
                if (current is T match) {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private sealed class ChildPreviewState {
            public ChildPreviewState(TreeViewItem child, bool isHitTestVisible, double opacity) {
                Child = child;
                IsHitTestVisible = isHitTestVisible;
                Opacity = opacity;
            }

            public TreeViewItem Child { get; }
            public bool IsHitTestVisible { get; }
            public double Opacity { get; }
        }
    }
}