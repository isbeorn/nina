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

namespace NINA.Sequencer.Logic
{
    /// <summary>
    /// Interaction logic for ExprComboControl.xaml
    /// </summary>
    public partial class ExprComboControl : UserControl {
        public ExprComboControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ExpProperty =
            DependencyProperty.Register("Exp", typeof(Expression), typeof(ExprComboControl), null);

        public Expression Exp { get; set; }

        public static readonly DependencyProperty ComboProperty =
            DependencyProperty.Register("Combo", typeof(IList<string>), typeof(ExprComboControl), null);

        public IList<string> Combo { get; set; }

        public void ShowConstants(object sender, ToolTipEventArgs e) {
            UserSymbol.ShowSymbols(e);
        }

    }
}
