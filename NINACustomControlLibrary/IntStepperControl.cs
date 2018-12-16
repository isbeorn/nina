#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NINACustomControlLibrary {

    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_Decrement", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Increment", Type = typeof(Button))]
    public class IntStepperControl : UserControl {

        static IntStepperControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IntStepperControl), new FrameworkPropertyMetadata(typeof(IntStepperControl)));
        }

        public static readonly DependencyProperty ButtonForegroundBrushProperty =
           DependencyProperty.Register(nameof(ButtonForegroundBrush), typeof(Brush), typeof(IntStepperControl), new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush ButtonForegroundBrush {
            get {
                return (Brush)GetValue(ButtonForegroundBrushProperty);
            }
            set {
                SetValue(ButtonForegroundBrushProperty, value);
            }
        }

        public static readonly DependencyProperty AddSVGProperty =
           DependencyProperty.Register(nameof(AddSVG), typeof(Geometry), typeof(IntStepperControl), new UIPropertyMetadata(null));

        public Geometry AddSVG {
            get {
                return (Geometry)GetValue(AddSVGProperty);
            }
            set {
                SetValue(AddSVGProperty, value);
            }
        }

        public static readonly DependencyProperty SubstractSVGProperty =
           DependencyProperty.Register(nameof(SubstractSVG), typeof(Geometry), typeof(IntStepperControl), new UIPropertyMetadata(null));

        public Geometry SubstractSVG {
            get {
                return (Geometry)GetValue(SubstractSVGProperty);
            }
            set {
                SetValue(SubstractSVGProperty, value);
            }
        }

        public static readonly DependencyProperty ValueProperty =
           DependencyProperty.Register(nameof(Value), typeof(int), typeof(IntStepperControl), new UIPropertyMetadata(0));

        public int Value {
            get {
                return (int)GetValue(ValueProperty);
            }
            set {
                SetValue(ValueProperty, value);
            }
        }

        public static readonly DependencyProperty MinValueProperty =
           DependencyProperty.Register(nameof(MinValue), typeof(int), typeof(IntStepperControl), new UIPropertyMetadata(int.MinValue));

        public int MinValue {
            get {
                return (int)GetValue(MinValueProperty);
            }
            set {
                SetValue(MinValueProperty, value);
            }
        }

        public static readonly DependencyProperty MaxValueProperty =
           DependencyProperty.Register(nameof(MaxValue), typeof(int), typeof(IntStepperControl), new UIPropertyMetadata(int.MaxValue));

        public int MaxValue {
            get {
                return (int)GetValue(MaxValueProperty);
            }
            set {
                SetValue(MaxValueProperty, value);
            }
        }

        public static readonly DependencyProperty StepSizeProperty =
           DependencyProperty.Register(nameof(StepSize), typeof(int), typeof(IntStepperControl), new UIPropertyMetadata(1));

        public int StepSize {
            get {
                return (int)GetValue(StepSizeProperty);
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
            if (tb != null) {
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