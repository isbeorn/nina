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

namespace NINA.WPF.Base.InputBox {
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : Window {
        public InputBox() {
            InitializeComponent();
        }

        public InputBox(string title, string hintText) {
            InitializeComponent();
            TitleText = title;
            HintText = hintText;            
        }

        public string HintText {
            get { return InputTextBox.HintText; }
            set { InputTextBox.HintText = value; }
        }

        public string TitleText {
            get { return TitleTextBox.Text; }
            set { TitleTextBox.Text = value; }
        }

        public string InputText {
            get { return InputTextBox.Text; }
            set { InputTextBox.Text = value; }
        }

        public bool Canceled { get; set; }

        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e) {
            Canceled = true;
            Close();
        }

        private void BtnOk_Click(object sender, System.Windows.RoutedEventArgs e) {
            Canceled = false;
            Close();
        }
    }
}
