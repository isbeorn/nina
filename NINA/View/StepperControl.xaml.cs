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

namespace NINA.View {
    /// <summary>
    /// Interaction logic for StepperControl.xaml
    /// </summary>
    public partial class StepperControl : UserControl {
        public StepperControl() {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }

        public static readonly DependencyProperty ValueProperty =
           DependencyProperty.Register(nameof(Value), typeof(double), typeof(StepperControl), new UIPropertyMetadata(0.0d));

        public double Value {
            get {
                return (double)GetValue(ValueProperty);
            }
            set {
                SetValue(ValueProperty, value);
            }
        }        

        private void Button_Plus_Click(object sender, RoutedEventArgs e) {
            Value++;
        }
        private void Button_Minus_Click(object sender, RoutedEventArgs e) {
            Value--;
        }
    }
}
