using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for SkyAtlasView.xaml
    /// </summary>
    public partial class SkyAtlasView : UserControl {

        public SkyAtlasView() {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}