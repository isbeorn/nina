using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for OptionsPlateSolverView.xaml
    /// </summary>
    public partial class OptionsPlateSolverView : UserControl {

        public OptionsPlateSolverView() {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}