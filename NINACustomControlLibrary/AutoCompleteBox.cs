#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NINACustomControlLibrary {

    public interface IAutoCompleteItem {
        string Column1 { get; }
        string Column2 { get; }
        string Column3 { get; }
    }

    public class AutoCompleteBox : HintTextBox {

        static AutoCompleteBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoCompleteBox), new FrameworkPropertyMetadata(typeof(AutoCompleteBox)));
        }

        public static readonly DependencyProperty PopupBackgroundProperty =
           DependencyProperty.Register(nameof(PopupBackground), typeof(Brush), typeof(AutoCompleteBox), new UIPropertyMetadata(Brushes.Transparent));

        public Brush PopupBackground {
            get {
                return (Brush)GetValue(PopupBackgroundProperty);
            }
            set {
                SetValue(PopupBackgroundProperty, value);
            }
        }

        public static readonly DependencyProperty SearchResultProperty =
           DependencyProperty.Register(nameof(SearchResult), typeof(ICollection<IAutoCompleteItem>), typeof(AutoCompleteBox), new UIPropertyMetadata(null));

        public ICollection<IAutoCompleteItem> SearchResult {
            get {
                return (ICollection<IAutoCompleteItem>)GetValue(SearchResultProperty);
            }
            set {
                SetValue(SearchResultProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedSearchResultProperty =
           DependencyProperty.Register(nameof(SelectedSearchResult), typeof(IAutoCompleteItem), typeof(AutoCompleteBox), new UIPropertyMetadata(null));

        public IAutoCompleteItem SelectedSearchResult {
            get {
                return (IAutoCompleteItem)GetValue(SelectedSearchResultProperty);
            }
            set {
                SetValue(SelectedSearchResultProperty, value);
            }
        }

        public static readonly DependencyProperty ShowPopupProperty =
           DependencyProperty.Register(nameof(ShowPopup), typeof(bool), typeof(AutoCompleteBox), new UIPropertyMetadata(false));

        public bool ShowPopup {
            get {
                return (bool)GetValue(ShowPopupProperty);
            }
            set {
                SetValue(ShowPopupProperty, value);
            }
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            var tb = GetTemplateChild("PART_TextBox") as TextBox;
            if (tb != null) {
                tb.PreviewKeyDown += Tb_PreviewKeyDown; ;
                tb.LostFocus += Tb_LostFocus;
            }
            var list = GetTemplateChild("PART_SearchCommandResultView") as ListView;
            if (list != null) {
                list.PreviewKeyDown += List_PreviewKeyDown;
                list.PreviewKeyUp += List_PreviewKeyUp;
                list.SelectionChanged += List_SelectionChanged;
            }
        }

        private void Tb_LostFocus(object sender, RoutedEventArgs e) {
            var list = GetTemplateChild("PART_SearchCommandResultView") as ListView;
            var subItemFocused = false;
            if (list != null) {
                for (int i = 0; i < list.Items.Count; i++) {
                    ListViewItem item = list.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                    if (item != null) {
                        if (item.IsFocused) {
                            subItemFocused = true;
                            break;
                        }
                    }
                }
            }
            if (!subItemFocused) {
                ShowPopup = false;
            }
        }

        private void List_PreviewKeyUp(object sender, KeyEventArgs e) {
            forceShowPopup = false;
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!forceShowPopup) {
                ShowPopup = false;
            }
        }

        private void List_PreviewKeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case (Key.Back): {
                        var tb = GetTemplateChild("PART_TextBox") as TextBox;
                        if (tb != null) {
                            tb.Focus();
                        }
                        ShowPopup = false;
                        break;
                    }

                case (Key.Enter): {
                        ShowPopup = false;
                        break;
                    }
                case (Key.Up):
                case (Key.Down): {
                        forceShowPopup = true;
                        break;
                    }
            }
        }

        /// <summary>
        /// Flag to suppress popup closing on selectionchange, when navigating using arrow keys
        /// </summary>
        private bool forceShowPopup;

        private void Tb_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (ShowPopup) {
                switch (e.Key) {
                    case (Key.Enter): {
                            var list = GetTemplateChild("PART_SearchCommandResultView") as ListView;
                            if (list != null && list.Items.Count > 0) {
                                ListViewItem item = list.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                                if (item != null) {
                                    item.IsSelected = true;
                                }
                                ShowPopup = false;
                            }
                            break;
                        }
                    case (Key.Down): {
                            var list = GetTemplateChild("PART_SearchCommandResultView") as ListView;
                            if (list != null && list.Items.Count > 0) {
                                forceShowPopup = true;
                                ListViewItem item = list.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                                if (item != null) {
                                    item.Focus();
                                    item.IsSelected = true;
                                    ShowPopup = true;
                                }
                            }
                            break;
                        }
                    case (Key.Tab):
                    case (Key.Escape): {
                            ShowPopup = false;
                            break;
                        }
                }
            }
        }
    }
}