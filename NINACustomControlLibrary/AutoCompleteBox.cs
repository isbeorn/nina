using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
    }
}