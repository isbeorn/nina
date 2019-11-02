using System.IO.Ports;
using System.Windows.Controls;

namespace NINA.View.Equipment {

    /// <summary>
    /// Interaction logic for AlnitakSetupView.xaml
    /// </summary>
    public partial class AlnitakSetupView : UserControl {

        public AlnitakSetupView() {
            InitializeComponent();
            SerialPorts.ItemsSource = SerialPort.GetPortNames();
        }
    }
}