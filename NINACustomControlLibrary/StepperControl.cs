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

namespace NINACustomControlLibrary
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:NINACustomControlLibrary"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:NINACustomControlLibrary;assembly=NINACustomControlLibrary"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:CustomControl1/>
    ///
    /// </summary>
    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_Decrement", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Increment", Type = typeof(Button))]
    public class StepperControl : UserControl {
        static StepperControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StepperControl), new FrameworkPropertyMetadata(typeof(StepperControl)));
        }

        public static readonly DependencyProperty ButtonForegroundBrushProperty =
           DependencyProperty.Register(nameof(ButtonForegroundBrush), typeof(Brush), typeof(StepperControl), new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush ButtonForegroundBrush {
            get {
                return (Brush)GetValue(ButtonForegroundBrushProperty);
            }
            set {
                SetValue(ButtonForegroundBrushProperty, value);
            }
        }

        public static readonly DependencyProperty AddSVGProperty =
           DependencyProperty.Register(nameof(AddSVG), typeof(Geometry), typeof(StepperControl), new UIPropertyMetadata(null));

        public Geometry AddSVG {
            get {
                return (Geometry)GetValue(AddSVGProperty);
            } 
            set {
                SetValue(AddSVGProperty, value);
            }
        }

        public static readonly DependencyProperty SubstractSVGProperty =
           DependencyProperty.Register(nameof(SubstractSVG), typeof(Geometry), typeof(StepperControl), new UIPropertyMetadata(null));

        public Geometry SubstractSVG {
            get {
                return (Geometry)GetValue(SubstractSVGProperty);
            }
            set {
                SetValue(SubstractSVGProperty, value);
            }
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

        public static readonly DependencyProperty MinValueProperty =
           DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(StepperControl), new UIPropertyMetadata(double.MinValue));

        public double MinValue {
            get {
                return (double)GetValue(MinValueProperty);
            }
            set {
                SetValue(MinValueProperty, value);
            }
        }

        public static readonly DependencyProperty MaxValueProperty =
           DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(StepperControl), new UIPropertyMetadata(double.MaxValue));

        public double MaxValue {
            get {
                return (double)GetValue(MaxValueProperty);
            }
            set {
                SetValue(MaxValueProperty, value);
            }
        }

        public static readonly DependencyProperty StepSizeProperty =
           DependencyProperty.Register(nameof(StepSize), typeof(double), typeof(StepperControl), new UIPropertyMetadata(1.0d));

        public double StepSize {
            get {
                return (double)GetValue(StepSizeProperty);
            }
            set {
                SetValue(StepSizeProperty, value);
            }
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            var button = GetTemplateChild("PART_Increment") as Button;
            if (button != null) {
                button.Click += Button_PART_Increment_Click;
            }

            button = GetTemplateChild("PART_Decrement") as Button;
            if (button != null) {
                button.Click += Button_PART_Decrement_Click;
            }

            var tb = GetTemplateChild("PART_Textbox") as TextBox;
            if(tb != null) {
                tb.LostFocus += PART_TextBox_LostFocus;
            }
        }

        private void Button_PART_Increment_Click(object sender, RoutedEventArgs e) {
            Value += StepSize;
            if (Value > MaxValue) {
                Value = MaxValue;
            }
        }
        private void Button_PART_Decrement_Click(object sender, RoutedEventArgs e) {
            Value -= StepSize;
            if (Value < MinValue) {
                Value = MinValue;
            }
        }

        private void PART_TextBox_LostFocus(object sender, RoutedEventArgs e) {
            if (Value < MinValue) {
                Value = MinValue;
            }
            if (Value > MaxValue) {
                Value = MaxValue;
            }
        }
    }
}
