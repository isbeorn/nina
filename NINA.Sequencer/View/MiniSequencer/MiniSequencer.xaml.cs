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
using NINA.Core.Utility.Extensions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.View.Sequencer.MiniSequencer {

    public partial class MiniSequencer : UserControl {
        // Virtualized rows cannot drive following through selection: an off-screen item may have no row at all.
        private readonly HashSet<INotifyPropertyChanged> observedItems = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<INotifyCollectionChanged> observedCollections = new(ReferenceEqualityComparer.Instance);
        private DispatcherOperation pendingRefresh;
        private bool subscriptionsDirty;
        private ISequenceItem followedItem;
        private ISequenceItem scrolledItem;

        public MiniSequencer() {
            InitializeComponent();

            Loaded += (_, _) => QueueRefresh(refreshSubscriptions: true, force: true);
            Unloaded += (_, _) => Detach();
            DataContextChanged += (_, _) => {
                Detach();
                QueueRefresh(refreshSubscriptions: true, force: true);
            };
            IsVisibleChanged += (_, _) => {
                if (IsVisible) {
                    QueueRefresh(refreshSubscriptions: true, force: true);
                }
            };
            SizeChanged += (_, _) => QueueRefresh(force: true);
        }

        private void Detach() {
            pendingRefresh?.Abort();
            pendingRefresh = null;
            Unsubscribe();
            followedItem = null;
            scrolledItem = null;
            subscriptionsDirty = false;
        }

        private void Unsubscribe() {
            foreach (INotifyPropertyChanged item in observedItems) {
                item.PropertyChanged -= ItemPropertyChanged;
            }
            foreach (INotifyCollectionChanged collection in observedCollections) {
                collection.CollectionChanged -= ItemsChanged;
            }
            observedItems.Clear();
            observedCollections.Clear();
        }

        private void Observe(ISequenceItem item) {
            if (item is INotifyPropertyChanged observable && observedItems.Add(observable)) {
                observable.PropertyChanged += ItemPropertyChanged;
            }
            if (item is ISequenceContainer container) {
                if (container.Items is INotifyCollectionChanged collection && observedCollections.Add(collection)) {
                    collection.CollectionChanged += ItemsChanged;
                }
                foreach (ISequenceItem child in container.GetItemsSnapshot()) {
                    Observe(child);
                }
            }
        }

        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e) {
            bool allProperties = string.IsNullOrEmpty(e.PropertyName);
            bool itemsChanged = allProperties || e.PropertyName == nameof(ISequenceContainer.Items);
            if (itemsChanged || e.PropertyName == nameof(ISequenceItem.Status) || e.PropertyName == nameof(ISequenceContainer.IsExpanded)) {
                // Sequence notifications can arrive on the execution thread. Only the dispatcher owns view state.
                Dispatcher.BeginInvoke(() => {
                    if (sender is INotifyPropertyChanged item && observedItems.Contains(item)) {
                        QueueRefresh(refreshSubscriptions: itemsChanged);
                    }
                });
            }
        }

        private void ItemsChanged(object sender, NotifyCollectionChangedEventArgs e) {
            Dispatcher.BeginInvoke(() => {
                if (sender is INotifyCollectionChanged collection && observedCollections.Contains(collection)) {
                    QueueRefresh(refreshSubscriptions: true);
                }
            });
        }

        private void QueueRefresh(bool refreshSubscriptions = false, bool force = false) {
            if (!IsLoaded) {
                return;
            }
            subscriptionsDirty |= refreshSubscriptions;
            if (force) {
                scrolledItem = null;
            }
            if (IsVisible && pendingRefresh == null) {
                pendingRefresh = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Loaded);
            }
        }

        private void Refresh() {
            pendingRefresh = null;
            if (!IsLoaded || !IsVisible || DataContext is not ISequenceContainer root) {
                return;
            }
            if (subscriptionsDirty) {
                Unsubscribe();
                Observe(root);
                subscriptionsDirty = false;
            }

            List<ISequenceItem> path = FindRunningPath(root);
            followedItem = path?.LastOrDefault();
            if (path == null) {
                scrolledItem = null;
                return;
            }

            // Follow the visible header of a folded ancestor without changing the user's fold preference.
            int last = path.FindIndex(item => item is ISequenceContainer container && !container.IsExpanded);
            if (last < 0) {
                last = path.Count - 1;
            }
            if (ReferenceEquals(scrolledItem, path[last])) {
                return;
            }

            ScrollViewer scroll = SequenceTree.GetDescendantByType(typeof(ScrollViewer)) as ScrollViewer;
            if (scroll == null) {
                return;
            }
            double horizontalOffset = scroll.HorizontalOffset;
            TreeViewItem row = RealizePath(path, last);
            if (row?.Template.FindName("PART_Header", row) is FrameworkElement header) {
                // Realizing nested rows can revise the panel's estimated heights after the first scroll.
                for (int pass = 0; pass < 2; pass++) {
                    scroll.UpdateLayout();
                    // Conditions and triggers share PART_Header with the title. Center the title line itself.
                    double center = header.TranslatePoint(new Point(0, Math.Min(20, header.ActualHeight) / 2), scroll).Y;
                    scroll.ScrollToVerticalOffset(Math.Clamp(scroll.VerticalOffset + center - scroll.ViewportHeight / 2, 0, scroll.ScrollableHeight));
                }
                scrolledItem = path[last];
            }
            scroll.ScrollToHorizontalOffset(horizontalOffset);
        }

        private List<ISequenceItem> FindRunningPath(ISequenceContainer container) {
            List<ISequenceItem> first = null;
            foreach (ISequenceItem item in container.GetItemsSnapshot()) {
                if (item.Status == SequenceEntityStatus.DISABLED) {
                    continue;
                }
                List<ISequenceItem> path = item is ISequenceContainer child
                    && SequenceTree.ItemTemplateSelector.SelectTemplate(item, SequenceTree) is HierarchicalDataTemplate
                    ? FindRunningPath(child) : null;
                if (path == null) {
                    if (item.Status != SequenceEntityStatus.RUNNING) {
                        continue;
                    }
                    path = new List<ISequenceItem>();
                }
                path.Insert(0, item);
                // Keep following the same instruction during parallel execution, otherwise use sequence order.
                if (ReferenceEquals(path[^1], followedItem)) {
                    return path;
                }
                first ??= path;
            }
            return first;
        }

        private TreeViewItem RealizePath(List<ISequenceItem> path, int last) {
            ItemsControl parent = SequenceTree;
            TreeViewItem row = null;
            for (int depth = 0; depth <= last; depth++) {
                parent.ApplyTemplate();
                parent.UpdateLayout();
                int index = parent.Items.IndexOf(path[depth]);
                if (index < 0) {
                    return null;
                }
                row = parent.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItem;
                if (row == null) {
                    ItemsPresenter presenter = parent is TreeViewItem parentRow
                        ? parentRow.Template.FindName("ItemsHost", parentRow) as ItemsPresenter
                        : parent.GetDescendantByType(typeof(ItemsPresenter)) as ItemsPresenter;
                    presenter?.ApplyTemplate();
                    if (presenter != null && VisualTreeHelper.GetChildrenCount(presenter) > 0
                        && VisualTreeHelper.GetChild(presenter, 0) is VirtualizingPanel panel) {
                        panel.BringIndexIntoViewPublic(index);
                        parent.UpdateLayout();
                        row = parent.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItem;
                    }
                }
                if (row == null) {
                    return null;
                }
                parent = row;
            }
            return row;
        }

    }
}