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
using System.Windows.Shapes;

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
