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
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace NINA.CustomControlLibrary {

    public class ImageGallery : ListBox {

        static ImageGallery() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageGallery), new FrameworkPropertyMetadata(typeof(ImageGallery)));
        }

        public ImageGallery() {
            CommandBindings.Add(new CommandBinding(NavigationCommands.PreviousPage, (_, e) => {
                MoveSelection(-1);
                e.Handled = true;
            }, CanNavigate));
            CommandBindings.Add(new CommandBinding(NavigationCommands.NextPage, (_, e) => {
                MoveSelection(1);
                e.Handled = true;
            }, CanNavigate));
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e) {
            base.OnItemsChanged(e);

            if (e.Action == NotifyCollectionChangedAction.Reset || SelectedIndex < 0) {
                int index = Items.Count == 0 ? -1 : System.Math.Clamp(e.OldStartingIndex, 0, Items.Count - 1);
                SetCurrentValue(SelectedIndexProperty, index);
            }

            for (int index = 0; index < Items.Count; index++) {
                if (ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem container) {
                    SetIndicatorName(container, index);
                }
            }

            CommandManager.InvalidateRequerySuggested();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
            base.PrepareContainerForItemOverride(element, item);

            SetIndicatorName(element, ItemContainerGenerator.IndexFromContainer(element));
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            if ((e.Key == Key.Left || e.Key == Key.Right) && Items.Count > 1) {
                MoveSelection(e.Key == Key.Left ? -1 : 1);
                (ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as ListBoxItem)?.Focus();
                e.Handled = true;
            }

            base.OnPreviewKeyDown(e);
        }

        private void CanNavigate(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = Items.Count > 1;
            e.Handled = true;
        }

        private void MoveSelection(int direction) {
            if (Items.Count == 0) {
                return;
            }

            int index = SelectedIndex < 0 ? 0 : (SelectedIndex + direction + Items.Count) % Items.Count;
            SetCurrentValue(SelectedIndexProperty, index);
        }

        private static void SetIndicatorName(DependencyObject container, int index) {
            System.Windows.Data.BindingOperations.SetBinding(container, AutomationProperties.NameProperty,
                new LocExtension("LblImageGalleryImage") { StringFormat = "{0} " + (index + 1) });
        }
    }
}
