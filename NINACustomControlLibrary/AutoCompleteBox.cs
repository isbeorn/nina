using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NINACustomControlLibrary {

    public class AutoCompleteBox : HintTextBox {

        static AutoCompleteBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoCompleteBox), new FrameworkPropertyMetadata(typeof(AutoCompleteBox)));
        }

        public static readonly DependencyProperty SearchResultProperty =
           DependencyProperty.Register(nameof(SearchResult), typeof(IEnumerable<string>), typeof(AutoCompleteBox), new UIPropertyMetadata(null));

        public IEnumerable<string> SearchResult {
            get {
                return (IEnumerable<string>)GetValue(SearchResultProperty);
            }
            set {
                SetValue(SearchResultProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedSearchResultProperty =
           DependencyProperty.Register(nameof(SelectedSearchResult), typeof(string), typeof(AutoCompleteBox), new UIPropertyMetadata(string.Empty));

        public string SelectedSearchResult {
            get {
                return (string)GetValue(SelectedSearchResultProperty);
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
    }
}