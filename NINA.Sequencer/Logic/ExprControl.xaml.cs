using NINA.Sequencer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NINA.Sequencer.Logic {

    public partial class ExprControl : UserControl {
        public ExprControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(ExprControl), null);

        public string Suffix { get; set; }
        
        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register("Suffix", typeof(string), typeof(ExprControl), null);

        public string Label { get; set; }

        public static readonly DependencyProperty ExpProperty =
             DependencyProperty.Register("Exp", typeof(Expression), typeof(ExprControl), null);

        public Expression Exp { get; set; }

 
        public void ShowConstants(object sender, ToolTipEventArgs e) {
            Symbol.ShowSymbols(sender);
        }

        public void IfConstant_PredicateToolTip(object sender, ToolTipEventArgs e) {
        //    TextBox predicateText = (TextBox)sender;
        //    IfConstant ifConstant = (IfConstant)(predicateText.DataContext);
        //    predicateText.ToolTip = ifConstant.ShowCurrentInfo();
        //
        //
        }


    }
}

