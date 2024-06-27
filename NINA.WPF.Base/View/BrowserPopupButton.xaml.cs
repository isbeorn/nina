using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.WPF.Base.View {
    /// <summary>
    /// Interaction logic for BrowserPopupButton.xaml
    /// </summary>
    public partial class BrowserPopupButton : UserControl {
        public BrowserPopupButton() {
            InitializeComponent();
            firstTime = true;
            if(ButtonImage == null) {
                ButtonImage = Application.Current.TryFindResource("BookSVG") as Geometry;
            }
        }
        public static DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(Uri), typeof(BrowserPopupButton), new PropertyMetadata(null));
        private bool firstTime;

        public Uri Source {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static DependencyProperty WindowWidthFractionProperty = DependencyProperty.Register(nameof(WindowWidthFraction), typeof(double), typeof(BrowserPopupButton), new PropertyMetadata(3.0d));
        public double WindowWidthFraction {
            get => (double)GetValue(WindowWidthFractionProperty);
            set => SetValue(WindowWidthFractionProperty, value);
        }

        public static DependencyProperty WindowHeightFractionProperty = DependencyProperty.Register(nameof(WindowHeightFraction), typeof(double), typeof(BrowserPopupButton), new PropertyMetadata(1.0d));
        public double WindowHeightFraction {
            get => (double)GetValue(WindowHeightFractionProperty);
            set => SetValue(WindowHeightFractionProperty, value);
        }

        public static DependencyProperty ButtonImageProperty = DependencyProperty.Register(nameof(ButtonImage), typeof(Geometry), typeof(BrowserPopupButton), new PropertyMetadata(null));
        public Geometry ButtonImage {
            get => (Geometry)GetValue(ButtonImageProperty);
            set => SetValue(ButtonImageProperty, value);
        }


        private void Popup_LostFocus(object sender, RoutedEventArgs e) {
            if (PopupControl.IsOpen && !PopupControl.IsKeyboardFocusWithin) {
                ToggleButtonPopup.IsChecked = false;
            }
        }
        private void PopupControl_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            if (PopupControl.IsOpen && !PopupControl.IsKeyboardFocusWithin) {
                ToggleButtonPopup.IsChecked = false;
            }
        }

        private void PopupControl_PreviewKeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                if (PopupControl.IsOpen && !PopupControl.IsKeyboardFocusWithin) {
                    ToggleButtonPopup.IsChecked = false;
                }
            }
        }

        private async void ToggleButtonPopup_Checked(object sender, RoutedEventArgs e) {
            PopupControl.IsOpen = true;            
            PopupBrowser.webView.Focus();
            
            if(!firstTime) {
                // Ensure to navigate back to the original url in case the user has navigated to a different place
                await PopupBrowser.webView.EnsureCoreWebView2Async();
                PopupBrowser.webView.CoreWebView2.Navigate(Source.ToString());
                PopupBrowser.webView.NavigationCompleted += WebView_NavigationCompleted;
            }
            firstTime = false;
        }

        private void ToggleButtonPopup_Unchecked(object sender, RoutedEventArgs e) {
            PopupControl.IsOpen = false;
        }
        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e) {
            if (e.IsSuccess && !string.IsNullOrEmpty(Source.Fragment)) {
                // Execute JavaScript to scroll to the element with the specified ID
                PopupBrowser.webView.CoreWebView2.ExecuteScriptAsync($"document.getElementById('{Source.Fragment.TrimStart('#')}').scrollIntoView();");
            } else {
                // Execute JavaScript to scroll to the top of the page
                PopupBrowser.webView.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, 0);");
            }

            // Detach the event handler to prevent multiple calls
            PopupBrowser.webView.NavigationCompleted -= WebView_NavigationCompleted;
        }
    }
    // Converter to calculate the height of the Popup
    public class BrowserPopupHeightConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            double windowWidth = 0d;
            if (values.Length == 0) { return 0; }
            if (values.Length == 1 || values[1] == DependencyProperty.UnsetValue) {
                windowWidth = (double)values[0];
                return windowWidth - 50;
            }
            windowWidth = (double)values[0];
            var windowWidthFraction = (double)values[1];
            return Math.Max(0, windowWidth / windowWidthFraction - 50);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    // Converter to calculate the width of the Popup
    public class BrowserPopupWidthConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            double windowWidth = 0d;
            if (values.Length == 0) {  return 0; }
            if(values.Length == 1 || values[1] == DependencyProperty.UnsetValue) {
                windowWidth = (double)values[0];
                return windowWidth / 3;
            }
            windowWidth = (double)values[0];
            var windowWidthFraction = (double)values[1];
            return Math.Max(0, windowWidth / windowWidthFraction);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
