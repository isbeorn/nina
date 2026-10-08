#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Container;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.View.Sequencer {
    /// <summary>
    /// Builds editor headers near the viewport after scrolling layout settles.
    /// Intermediate headers stay lightweight while WPF corrects its estimated scroll extent.
    /// </summary>
    public class DeferredSequenceHeader : ContentPresenter {
        private static readonly DataTemplate placeholder = new DataTemplate();
        // Cache geometry, never views. A released sequence must not be retained by scrolling.
        private static readonly ConditionalWeakTable<object, HeaderSize> sizes = new();
        private bool realized;
        private bool initializing;
        private double estimatedHeight;
        private DispatcherOperation pending;
        private ScrollContentPresenter viewport;
        private TreeView tree;

        public static readonly DependencyProperty PreviewTemplateProperty = DependencyProperty.Register(
            nameof(PreviewTemplate), typeof(DataTemplate), typeof(DeferredSequenceHeader),
            new FrameworkPropertyMetadata(null, (owner, _) => owner.CoerceValue(ContentTemplateProperty)));

        public DataTemplate PreviewTemplate {
            get => (DataTemplate)GetValue(PreviewTemplateProperty);
            set => SetValue(PreviewTemplateProperty, value);
        }

        static DeferredSequenceHeader() {
            ContentProperty.OverrideMetadata(typeof(DeferredSequenceHeader), new FrameworkPropertyMetadata(null,
                (owner, args) => ((DeferredSequenceHeader)owner).ResetContent()));
            ContentTemplateProperty.OverrideMetadata(typeof(DeferredSequenceHeader), new FrameworkPropertyMetadata(null, null,
                (owner, value) => ((DeferredSequenceHeader)owner).realized ? value : ((DeferredSequenceHeader)owner).PreviewTemplate ?? placeholder));
        }

        public DeferredSequenceHeader() {
            CoerceValue(ContentTemplateProperty);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsVisibleChanged += (_, _) => Queue();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ConnectViewport();
            Queue();
        }

        private void ConnectViewport() {
            if (realized) return;
            DisconnectViewport();
            for (DependencyObject parent = VisualTreeHelper.GetParent(this); parent != null; parent = VisualTreeHelper.GetParent(parent)) {
                if (viewport == null && parent is ScrollContentPresenter presenter) viewport = presenter;
                if (parent is TreeView owner) {
                    tree = owner;
                    break;
                }
            }
            LayoutUpdated += OnLayoutUpdated;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            pending?.Abort();
            pending = null;
            if (initializing) {
                initializing = false;
                InvalidateMeasure();
            }
            DisconnectViewport();
        }

        private void DisconnectViewport() {
            LayoutUpdated -= OnLayoutUpdated;
            viewport = null;
            tree = null;
        }

        private void ResetContent() {
            pending?.Abort();
            pending = null;
            realized = false;
            initializing = false;
            CoerceValue(ContentTemplateProperty);
            InvalidateMeasure();
            if (IsLoaded) {
                ConnectViewport();
                Queue();
            }
        }

        private void OnLayoutUpdated(object sender, EventArgs e) => Queue();

        private void Queue() {
            if (realized || pending != null || !IsLoaded) return;
            pending = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                pending = null;
                if (!IsLoaded || !IsVisible) return;
                if (viewport != null) {
                    var bounds = TransformToAncestor(viewport).TransformBounds(new Rect(RenderSize));
                    var bufferedViewport = new Rect(viewport.RenderSize);
                    if (tree != null && VirtualizingPanel.GetCacheLengthUnit(tree) == VirtualizationCacheLengthUnit.Pixel) {
                        var cache = VirtualizingPanel.GetCacheLength(tree);
                        bufferedViewport.Y -= cache.CacheBeforeViewport;
                        bufferedViewport.Height += cache.CacheBeforeViewport + cache.CacheAfterViewport;
                    }
                    if (!bounds.IntersectsWith(bufferedViewport)) return;
                }
                realized = true;
                // Binding initialization can briefly collapse an expander before its content appears.
                // Keep the estimated height through that layout pass so WPF does not move the scroll anchor twice.
                initializing = true;
                DisconnectViewport();
                CoerceValue(ContentTemplateProperty);
                InvalidateMeasure();
                pending = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                    pending = null;
                    initializing = false;
                    InvalidateMeasure();
                }));
            }));
        }

        protected override Size MeasureOverride(Size constraint) {
            if (!realized) {
                var previewSize = base.MeasureOverride(constraint);
                // Keep navigation identity visible while reserving room for the full editor.
                estimatedHeight = Content != null && sizes.TryGetValue(Content, out var previous)
                    ? previous.Size.Height
                    : Content is DeepSkyObjectContainer ? 350 : 30;
                estimatedHeight = Math.Max(estimatedHeight, previewSize.Height);
                return new Size(Math.Max(1, previewSize.Width), estimatedHeight);
            }
            var result = base.MeasureOverride(constraint);
            if (initializing) return new Size(result.Width, Math.Max(result.Height, estimatedHeight));
            if (Content != null) {
                var size = sizes.GetOrCreateValue(Content);
                size.Size = result;
            }
            return result;
        }

        private sealed class HeaderSize {
            public Size Size;
        }
    }
}