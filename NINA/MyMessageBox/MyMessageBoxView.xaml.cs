using System.Windows;

namespace NINA.MyMessageBox {

    /// <summary>
    /// Interaction logic for MyMessageBoxView.xaml
    /// </summary>
    public partial class MyMessageBoxView : Window {

        public MyMessageBoxView() {
            InitializeComponent();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}