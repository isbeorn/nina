using System.Windows;

namespace NINA.EquipmentChooser {

    /// <summary>
    /// Interaction logic for EquipmentChooser.xaml
    /// </summary>
    public partial class EquipmentChooser : Window {

        public EquipmentChooser() {
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }
    }
}