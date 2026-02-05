using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.WPF.Base.View {
    /// <summary>
    /// Interaction logic for PopupButton.xaml
    /// </summary>
    public partial class PopupButton : UserControl {

        public PopupButton() {
            InitializeComponent();
            if (ButtonImage == null) {
                ButtonImage = Application.Current.TryFindResource("SettingsSVG") as Geometry;
            }
        }

        private DateTime lastCloseTime = DateTime.UtcNow;

        public static DependencyProperty FocusIndexProperty = DependencyProperty.Register(nameof(FocusIndex), typeof(string), typeof(PopupButton), new PropertyMetadata(null));

        public string FocusIndex {
            get => (string)GetValue(FocusIndexProperty);
            set => SetValue(FocusIndexProperty, value);
        }

        public static DependencyProperty OpenCommandProperty = DependencyProperty.Register(nameof(OpenCommand), typeof(ICommand), typeof(PopupButton), new PropertyMetadata(null));

        public ICommand OpenCommand {
            get => (ICommand)GetValue(OpenCommandProperty);
            set => SetValue(OpenCommandProperty, value);
        }

        public static DependencyProperty CloseCommandProperty = DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(PopupButton), new PropertyMetadata(null));

        public ICommand CloseCommand {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        public static DependencyProperty ButtonImageProperty = DependencyProperty.Register(nameof(ButtonImage), typeof(Geometry), typeof(PopupButton), new PropertyMetadata(null));

        public Geometry ButtonImage {
            get => (Geometry)GetValue(ButtonImageProperty);
            set => SetValue(ButtonImageProperty, value);
        }

        public static readonly DependencyProperty PopupContentProperty =
            DependencyProperty.Register(nameof(PopupContent), typeof(FrameworkElement), typeof(PopupButton));

        public FrameworkElement PopupContent {
            get => (FrameworkElement)GetValue(PopupContentProperty);
            set => SetValue(PopupContentProperty, value);
        }

        private void ToggleButtonPopup_Checked(object sender, RoutedEventArgs e) {
            if (DateTime.UtcNow - lastCloseTime < TimeSpan.FromMilliseconds(200)) { ToggleButtonPopup.IsChecked = false; return; }
            PopupControl.IsOpen = true;
            DependencyObject elem = PopupContent;
            if (!string.IsNullOrWhiteSpace(FocusIndex)) {
                foreach (var index in FocusIndex.Split(",")) {
                    if (int.TryParse(index, out var i)) {
                        elem = VisualTreeHelper.GetChild(elem, i);
                    }
                }
            }
            (elem as FrameworkElement).Focus();
            OpenCommand?.Execute(PopupControl);
        }

        private void ToggleButtonPopup_Unchecked(object sender, RoutedEventArgs e) {
            if (DateTime.UtcNow - lastCloseTime < TimeSpan.FromMilliseconds(200)) { return; }
            lastCloseTime = DateTime.UtcNow;
            PopupControl.IsOpen = false;
            CloseCommand?.Execute(PopupControl);
        }

        private void PopupControl_LostFocus(object sender, RoutedEventArgs e) {
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
                if (PopupControl.IsOpen) {
                    ToggleButtonPopup.IsChecked = false;
                }
            }
        }
    }
}
