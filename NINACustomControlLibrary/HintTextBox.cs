using System.Windows;
using System.Windows.Controls;

namespace NINACustomControlLibrary {

    public class HintTextBox : TextBox {

        public HintTextBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HintTextBox), new FrameworkPropertyMetadata(typeof(HintTextBox)));
        }

        public static readonly DependencyProperty HintTextProperty =
           DependencyProperty.Register(nameof(HintText), typeof(string), typeof(HintTextBox), new UIPropertyMetadata(string.Empty));

        public string HintText {
            get {
                return (string)GetValue(HintTextProperty);
            }
            set {
                SetValue(HintTextProperty, value);
            }
        }
    }
}