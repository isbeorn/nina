#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace NINA.WPF.Base.Behaviors {

    public static class HyperlinkCopyBehavior {

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(HyperlinkCopyBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
            "State",
            typeof(BehaviorState),
            typeof(HyperlinkCopyBehavior),
            new PropertyMetadata(null));

        [AttachedPropertyBrowsableForType(typeof(Hyperlink))]
        public static bool GetIsEnabled(DependencyObject element) {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject element, bool value) {
            element.SetValue(IsEnabledProperty, value);
        }

        private static BehaviorState GetState(DependencyObject element) {
            return (BehaviorState)element.GetValue(StateProperty);
        }

        private static void SetState(DependencyObject element, BehaviorState value) {
            element.SetValue(StateProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args) {
            if (dependencyObject is not Hyperlink hyperlink) {
                return;
            }

            if ((bool)args.NewValue) {
                Attach(hyperlink);
            } else {
                Detach(hyperlink);
            }
        }

        private static void Attach(Hyperlink hyperlink) {
            if (GetState(hyperlink) != null) {
                return;
            }

            ContextMenu contextMenu = hyperlink.ContextMenu;
            bool ownsContextMenu = contextMenu == null;
            contextMenu ??= new ContextMenu();

            Separator separator = null;
            if (contextMenu.Items.Count > 0) {
                separator = new Separator();
                contextMenu.Items.Add(separator);
            }

            MenuItem copyMenuItem = new MenuItem();
            copyMenuItem.SetBinding(
                MenuItem.HeaderProperty,
                new Binding("[LblCopyURL]") {
                    Mode = BindingMode.OneWay,
                    Source = Loc.Instance
                });
            contextMenu.Items.Add(copyMenuItem);

            BehaviorState state = new BehaviorState(hyperlink, contextMenu, copyMenuItem, separator, ownsContextMenu);
            SetState(hyperlink, state);
            state.Attach();

            if (ownsContextMenu) {
                hyperlink.ContextMenu = contextMenu;
            }
        }

        private static void Detach(Hyperlink hyperlink) {
            BehaviorState state = GetState(hyperlink);
            if (state == null) {
                return;
            }

            state.Detach();
            hyperlink.ClearValue(StateProperty);
        }

        private static string ResolveTarget(Hyperlink hyperlink) {
            if (hyperlink.NavigateUri != null) {
                return hyperlink.NavigateUri.OriginalString;
            }

            if (hyperlink.CommandParameter is Uri uri) {
                return uri.OriginalString;
            }

            if (hyperlink.CommandParameter is string value
                    && Uri.TryCreate(value, UriKind.Absolute, out _)) {
                return value;
            }

            return null;
        }

        private sealed class BehaviorState {

            private readonly Hyperlink hyperlink;
            private readonly ContextMenu contextMenu;
            private readonly MenuItem copyMenuItem;
            private readonly Separator separator;
            private readonly bool ownsContextMenu;

            public BehaviorState(
                    Hyperlink hyperlink,
                    ContextMenu contextMenu,
                    MenuItem copyMenuItem,
                    Separator separator,
                    bool ownsContextMenu) {
                this.hyperlink = hyperlink;
                this.contextMenu = contextMenu;
                this.copyMenuItem = copyMenuItem;
                this.separator = separator;
                this.ownsContextMenu = ownsContextMenu;
            }

            public void Attach() {
                hyperlink.ContextMenuOpening += Hyperlink_ContextMenuOpening;
                copyMenuItem.Click += CopyMenuItem_Click;
                UpdateVisibility();
            }

            public void Detach() {
                hyperlink.ContextMenuOpening -= Hyperlink_ContextMenuOpening;
                copyMenuItem.Click -= CopyMenuItem_Click;
                contextMenu.Items.Remove(copyMenuItem);
                if (separator != null) {
                    contextMenu.Items.Remove(separator);
                }

                if (ownsContextMenu && ReferenceEquals(hyperlink.ContextMenu, contextMenu)) {
                    hyperlink.ClearValue(FrameworkContentElement.ContextMenuProperty);
                }
            }

            private void Hyperlink_ContextMenuOpening(object sender, ContextMenuEventArgs args) {
                bool hasTarget = UpdateVisibility();
                if (!hasTarget && ownsContextMenu) {
                    args.Handled = true;
                }
            }

            private void CopyMenuItem_Click(object sender, RoutedEventArgs args) {
                string target = ResolveTarget(hyperlink);
                if (!string.IsNullOrWhiteSpace(target)) {
                    Clipboard.SetText(target);
                }
            }

            private bool UpdateVisibility() {
                bool hasTarget = !string.IsNullOrWhiteSpace(ResolveTarget(hyperlink));
                Visibility visibility = hasTarget ? Visibility.Visible : Visibility.Collapsed;
                copyMenuItem.Visibility = visibility;
                if (separator != null) {
                    separator.Visibility = visibility;
                }
                return hasTarget;
            }
        }
    }
}