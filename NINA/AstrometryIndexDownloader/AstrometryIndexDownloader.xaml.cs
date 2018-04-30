using System.Windows;

namespace NINA.AstrometryIndexDownloader {

    /// <summary>
    /// Interaction logic for AstrometryIndexDownloader.xaml
    /// </summary>
    public partial class AstrometryIndexDownloader : Window {

        public AstrometryIndexDownloader() {
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }
    }
}