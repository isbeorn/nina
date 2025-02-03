using NINA.Core.Utility;
using NINA.Sequencer.SequenceItem;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Sequencer.Logic {

    public partial class ExprControl : UserControl {
        public ExprControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ExpProperty =
            DependencyProperty.Register("Exp", typeof(Expression), typeof(ExprControl), null);

        public Expression Exp { get; set; }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(String), typeof(ExprControl), null);

        public String Label { get; set; }

        public static readonly DependencyProperty DefaultProperty =
             DependencyProperty.Register("Default", typeof(String), typeof(ExprControl), null);

        public String Default { get; set; }
        
        public static readonly DependencyProperty UnitProperty =
             DependencyProperty.Register("Unit", typeof(String), typeof(ExprControl), null);

        public String Unit { get; set; }

        public void ShowSymbols(object sender, ToolTipEventArgs e) {
            Logger.Info("ShowSymbols shim");
        }

    }
}

