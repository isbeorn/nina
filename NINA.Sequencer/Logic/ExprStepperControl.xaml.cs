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

    public partial class ExprStepperControl : UserControl {
        public ExprStepperControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ExpProperty =
            DependencyProperty.Register("Exp", typeof(Expression), typeof(ExprStepperControl), null);

        public Expression Exp {
            get => (Expression)GetValue(ExpProperty);
            set => SetValue(ExpProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(String), typeof(ExprStepperControl), null);
        
        public String Label { get; set; }

        public static readonly DependencyProperty DefaultProperty =
             DependencyProperty.Register("Default", typeof(String), typeof(ExprStepperControl), null);

        public String Default { get; set; }

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(String), typeof(ExprStepperControl), null);

        public String Unit { get; set; }

        public static readonly DependencyProperty MinProperty =
             DependencyProperty.Register("Min", typeof(double), typeof(ExprStepperControl), null);

        public double Min {
            get => (double)GetValue(MinProperty);
            set => SetValue(MinProperty, value);
        }

        public static readonly DependencyProperty MaxProperty =
             DependencyProperty.Register("Max", typeof(double), typeof(ExprStepperControl), null);

        public double Max {
            get => (double)GetValue(MaxProperty);
            set => SetValue(MaxProperty, value);
        }

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register("Step", typeof(double), typeof(ExprStepperControl), null);
        public double Step {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        public void ShowSymbols(object sender, ToolTipEventArgs e) {
            UserSymbol.ShowSymbols(sender);
            //e.Handled = true;
        }

        public void AddClick(object sender, RoutedEventArgs e) {
            if (Exp.Value + Step <= Max) {
                Exp.Definition = (Exp.Value + Step).ToString();
            }
        }

        public void SubtractClick(object sender, RoutedEventArgs e) {
            if (Exp.Value - Step >= Min) {
                Exp.Definition = (Exp.Value - Step).ToString();
            }
        }

    }
}

