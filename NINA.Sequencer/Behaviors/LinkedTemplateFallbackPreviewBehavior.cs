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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Sequencer.Behaviors {

    public class LinkedTemplateFallbackPreviewBehavior : Behavior<FrameworkElement> {
        private const double ReadOnlyOpacity = 0.75d;

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
            nameof(IsEditing),
            typeof(bool),
            typeof(LinkedTemplateFallbackPreviewBehavior),
            new PropertyMetadata(false, IsEditingChanged));

        private readonly List<PreviewState> previewStates = new List<PreviewState>();
        private readonly List<ItemContainerGenerator> observedGenerators = new List<ItemContainerGenerator>();
        private bool suppressingInput;
        private bool applyingPreviewState;

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
            ClearPreviewState();
            if (AssociatedObject != null) {
                AssociatedObject.Loaded -= AssociatedObject_Loaded;
                AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
            }

            base.OnDetaching();
        }

        private static void IsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((LinkedTemplateFallbackPreviewBehavior)d).ScheduleApplyPreviewState();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e) {
            ScheduleApplyPreviewState();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e) {
            ClearPreviewState();
        }

        private void ClearPreviewState() {
            SetInputSuppression(false);
            RestorePreviewState();
            DetachFromGenerators();
        }

        private void ScheduleApplyPreviewState() {
            ScheduleApplyPreviewState(DispatcherPriority.Loaded);
            ScheduleApplyPreviewState(DispatcherPriority.ContextIdle);
        }

        private void ScheduleApplyPreviewState(DispatcherPriority priority) {
            FrameworkElement associatedObject = AssociatedObject;
            if (associatedObject == null) {
                return;
            }

            Dispatcher dispatcher = associatedObject.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) {
                return;
            }

            try {
                dispatcher.BeginInvoke(priority, new Action(ApplyPreviewState));
            } catch (InvalidOperationException) {
            }
        }

        private void ApplyPreviewState() {
            if (applyingPreviewState) {
                return;
            }

            applyingPreviewState = true;
            try {
                SetInputSuppression(false);
                RestorePreviewState();
                DetachFromGenerators();

                FrameworkElement associatedObject = AssociatedObject;
                if (associatedObject == null || IsEditing) {
                    return;
                }

                SetInputSuppression(true);
                StoreOpacityState(associatedObject);
                associatedObject.Opacity = ReadOnlyOpacity;

                foreach (DependencyObject current in EnumerateSelfAndDescendants(associatedObject)) {
                    if (current is ItemsControl itemsControl) {
                        ObserveGenerator(itemsControl);
                    }

                    if (current is FrameworkElement frameworkElement) {
                        DisablePreviewBehaviors(frameworkElement);
                        if (ShouldSuppressHitTesting(frameworkElement)) {
                            StoreHitTestState(frameworkElement);
                            frameworkElement.IsHitTestVisible = false;
                        }
                    }
                }
            } finally {
                applyingPreviewState = false;
            }
        }

        private void RestorePreviewState() {
            foreach (PreviewState state in previewStates) {
                state.Restore();
            }

            previewStates.Clear();
        }

        private void DisablePreviewBehaviors(FrameworkElement frameworkElement) {
            foreach (Behavior behavior in Interaction.GetBehaviors(frameworkElement)) {
                if (behavior is DragDropBehavior dragDropBehavior) {
                    bool isEnabled = dragDropBehavior.IsEnabled;
                    previewStates.Add(new PreviewState(() => dragDropBehavior.IsEnabled = isEnabled));
                    dragDropBehavior.IsEnabled = false;
                } else if (behavior is DragOverBehavior dragOverBehavior) {
                    bool isEnabled = dragOverBehavior.Enabled;
                    previewStates.Add(new PreviewState(() => dragOverBehavior.Enabled = isEnabled));
                    dragOverBehavior.Enabled = false;
                } else if (behavior is DropIntoBehavior dropIntoBehavior) {
                    bool isEnabled = dropIntoBehavior.IsEnabled;
                    previewStates.Add(new PreviewState(() => dropIntoBehavior.IsEnabled = isEnabled));
                    dropIntoBehavior.IsEnabled = false;
                }
            }
        }

        private void StoreHitTestState(UIElement element) {
            bool isHitTestVisible = element.IsHitTestVisible;
            previewStates.Add(new PreviewState(() => element.IsHitTestVisible = isHitTestVisible));
        }

        private void StoreOpacityState(UIElement element) {
            double opacity = element.Opacity;
            previewStates.Add(new PreviewState(() => element.Opacity = opacity));
        }

        private void SetInputSuppression(bool suppressInput) {
            FrameworkElement associatedObject = AssociatedObject;
            if (associatedObject == null || suppressingInput == suppressInput) {
                return;
            }

            if (suppressInput) {
                associatedObject.AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(ReadOnlyPreview_PreviewMouseDown), true);
                associatedObject.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(ReadOnlyPreview_MouseActivity), true);
                associatedObject.AddHandler(UIElement.MouseEnterEvent, new MouseEventHandler(ReadOnlyPreview_MouseActivity), true);
                associatedObject.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(ReadOnlyPreview_MouseActivity), true);
                associatedObject.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(ReadOnlyPreview_PreviewKeyDown), true);
                associatedObject.AddHandler(FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(ReadOnlyPreview_ContextMenuOpening), true);
            } else {
                associatedObject.RemoveHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(ReadOnlyPreview_PreviewMouseDown));
                associatedObject.RemoveHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(ReadOnlyPreview_MouseActivity));
                associatedObject.RemoveHandler(UIElement.MouseEnterEvent, new MouseEventHandler(ReadOnlyPreview_MouseActivity));
                associatedObject.RemoveHandler(UIElement.MouseMoveEvent, new MouseEventHandler(ReadOnlyPreview_MouseActivity));
                associatedObject.RemoveHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(ReadOnlyPreview_PreviewKeyDown));
                associatedObject.RemoveHandler(FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(ReadOnlyPreview_ContextMenuOpening));
            }

            suppressingInput = suppressInput;
        }

        private void ReadOnlyPreview_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            if (IsEditing) {
                return;
            }

            ApplyPreviewState();
            e.Handled = !IsExpanderHeaderToggle(e.OriginalSource as DependencyObject);
        }

        private void ReadOnlyPreview_MouseActivity(object sender, MouseEventArgs e) {
            if (!IsEditing) {
                ApplyPreviewState();
            }
        }

        private void ReadOnlyPreview_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (!IsEditing) {
                e.Handled = true;
            }
        }

        private void ReadOnlyPreview_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            if (!IsEditing) {
                e.Handled = true;
            }
        }

        private static bool ShouldSuppressHitTesting(FrameworkElement element) {
            return element is TextBoxBase
                || element is Selector
                || element is RangeBase
                || (element is ButtonBase && !IsExpanderHeaderToggle(element));
        }

        private static bool IsExpanderHeaderToggle(DependencyObject element) {
            DependencyObject current = element;
            while (current != null) {
                if (current is ToggleButton toggleButton && toggleButton.TemplatedParent is Expander) {
                    return true;
                }

                current = GetParent(current);
            }

            return false;
        }

        private void ObserveGenerator(ItemsControl itemsControl) {
            ItemContainerGenerator generator = itemsControl?.ItemContainerGenerator;
            if (generator == null || observedGenerators.Contains(generator)) {
                return;
            }

            generator.StatusChanged += ItemContainerGenerator_StatusChanged;
            generator.ItemsChanged += ItemContainerGenerator_ItemsChanged;
            observedGenerators.Add(generator);
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e) {
            ScheduleApplyPreviewState();
        }

        private void ItemContainerGenerator_ItemsChanged(object sender, ItemsChangedEventArgs e) {
            ScheduleApplyPreviewState();
        }

        private void DetachFromGenerators() {
            foreach (ItemContainerGenerator generator in observedGenerators) {
                generator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                generator.ItemsChanged -= ItemContainerGenerator_ItemsChanged;
            }

            observedGenerators.Clear();
        }

        private static IEnumerable<DependencyObject> EnumerateSelfAndDescendants(DependencyObject root) {
            if (root == null) {
                yield break;
            }

            yield return root;

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++) {
                foreach (DependencyObject child in EnumerateSelfAndDescendants(VisualTreeHelper.GetChild(root, i))) {
                    yield return child;
                }
            }
        }

        private static DependencyObject GetParent(DependencyObject current) {
            if (current == null) {
                return null;
            }

            try {
                DependencyObject visualParent = VisualTreeHelper.GetParent(current);
                if (visualParent != null) {
                    return visualParent;
                }
            } catch (InvalidOperationException) {
            }

            if (current is FrameworkElement frameworkElement) {
                if (frameworkElement.Parent != null) {
                    return frameworkElement.Parent;
                }

                if (frameworkElement.TemplatedParent is DependencyObject templatedParent) {
                    return templatedParent;
                }
            }

            if (current is FrameworkContentElement frameworkContentElement) {
                if (frameworkContentElement.Parent != null) {
                    return frameworkContentElement.Parent;
                }

                if (frameworkContentElement.TemplatedParent is DependencyObject templatedParent) {
                    return templatedParent;
                }
            }

            return LogicalTreeHelper.GetParent(current);
        }

        private sealed class PreviewState {
            private readonly Action restore;

            public PreviewState(Action restore) {
                this.restore = restore;
            }

            public void Restore() {
                restore();
            }
        }
    }
}
