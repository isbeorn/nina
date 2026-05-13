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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Sequencer.Behaviors {

    public class LinkedTemplateFallbackVisibilityBehavior : Behavior<FrameworkElement> {

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.DataContextChanged += AssociatedObject_DataContextChanged;
            if (AssociatedObject.IsLoaded) {
                ScheduleUpdateFallbackVisibility();
            }
        }

        protected override void OnDetaching() {
            if (AssociatedObject != null) {
                AssociatedObject.Loaded -= AssociatedObject_Loaded;
                AssociatedObject.DataContextChanged -= AssociatedObject_DataContextChanged;
            }

            base.OnDetaching();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e) {
            ScheduleUpdateFallbackVisibility();
        }

        private void AssociatedObject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (AssociatedObject?.IsLoaded == true) {
                ScheduleUpdateFallbackVisibility();
            }
        }

        private void ScheduleUpdateFallbackVisibility() {
            FrameworkElement associatedObject = AssociatedObject;
            if (associatedObject == null) {
                return;
            }

            UpdateFallbackVisibility();
            ScheduleUpdateFallbackVisibility(DispatcherPriority.Loaded);
            ScheduleUpdateFallbackVisibility(DispatcherPriority.ContextIdle);
        }

        private void ScheduleUpdateFallbackVisibility(DispatcherPriority priority) {
            FrameworkElement associatedObject = AssociatedObject;
            if (associatedObject == null) {
                return;
            }

            Dispatcher dispatcher = associatedObject.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) {
                return;
            }

            try {
                dispatcher.BeginInvoke(priority, new Action(UpdateFallbackVisibility));
            } catch (InvalidOperationException) {
            }
        }

        private void UpdateFallbackVisibility() {
            FrameworkElement associatedObject = AssociatedObject;
            if (associatedObject == null) {
                return;
            }

            associatedObject.Visibility = IsHostedByOwnTreeViewItem(associatedObject)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private static bool IsHostedByOwnTreeViewItem(FrameworkElement fallbackHost) {
            object linkedTemplate = fallbackHost.DataContext;
            if (linkedTemplate == null) {
                return false;
            }

            TreeViewItem treeViewItem = FindAncestor<TreeViewItem>(fallbackHost);
            return treeViewItem != null
                && (ReferenceEquals(treeViewItem.DataContext, linkedTemplate)
                    || ReferenceEquals(treeViewItem.Header, linkedTemplate));
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject {
            while (current != null) {
                current = GetParent(current);
                if (current is T match) {
                    return match;
                }
            }

            return null;
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
    }
}
