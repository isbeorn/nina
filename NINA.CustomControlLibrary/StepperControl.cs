#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NINA.CustomControlLibrary {

    /// <summary> Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project. Add
    /// this XmlNamespace attribute to the root element of the markup file where it is to be used:
    ///
    /// xmlns:MyNamespace="clr-namespace:NINA.CustomControlLibrary"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project. Add
    /// this XmlNamespace attribute to the root element of the markup file where it is to be used:
    ///
    /// xmlns:MyNamespace="clr-namespace:NINA.CustomControlLibrary;assembly=NINA.CustomControlLibrary"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives to
    /// this project and Rebuild to avoid compilation errors:
    ///
    /// Right click on the target project in the Solution Explorer and "Add
    /// Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2) Go ahead and use your control in the XAML file.
    ///
    /// <MyNamespace:CustomControl1/>
    ///
    /// </summary>
    [TemplatePart(Name = "PART_TextBox", Type = typeof(UnitTextBox))]
    [TemplatePart(Name = "PART_Decrement", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Increment", Type = typeof(Button))]
    public class StepperControl : UserControl {

        static StepperControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StepperControl), new FrameworkPropertyMetadata(typeof(StepperControl)));
        }

        public static readonly DependencyProperty ButtonForegroundBrushProperty =
           DependencyProperty.Register(nameof(ButtonForegroundBrush), typeof(Brush), typeof(StepperControl), new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush ButtonForegroundBrush {
            get => (Brush)GetValue(ButtonForegroundBrushProperty);
            set => SetValue(ButtonForegroundBrushProperty, value);
        }

        public static readonly DependencyProperty AddSVGProperty =
           DependencyProperty.Register(nameof(AddSVG), typeof(Geometry), typeof(StepperControl), new UIPropertyMetadata(null));

        public Geometry AddSVG {
            get => (Geometry)GetValue(AddSVGProperty);
            set => SetValue(AddSVGProperty, value);
        }

        public static readonly DependencyProperty SubstractSVGProperty =
           DependencyProperty.Register(nameof(SubstractSVG), typeof(Geometry), typeof(StepperControl), new UIPropertyMetadata(null));

        public Geometry SubstractSVG {
            get => (Geometry)GetValue(SubstractSVGProperty);
            set => SetValue(SubstractSVGProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
           DependencyProperty.Register(nameof(Value), typeof(double), typeof(StepperControl), new UIPropertyMetadata(0.0d));

        public double Value {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinValueProperty =
           DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(StepperControl), new UIPropertyMetadata(double.MinValue));

        public double MinValue {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty =
           DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(StepperControl), new UIPropertyMetadata(double.MaxValue));

        public double MaxValue {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public static readonly DependencyProperty StepSizeProperty =
           DependencyProperty.Register(nameof(StepSize), typeof(double), typeof(StepperControl), new UIPropertyMetadata(1.0d));

        public double StepSize {
            get => (double)GetValue(StepSizeProperty);
            set => SetValue(StepSizeProperty, value);
        }

        public static readonly DependencyProperty UnitProperty =
           DependencyProperty.Register(nameof(Unit), typeof(string), typeof(StepperControl), new UIPropertyMetadata(string.Empty));

        public string Unit {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        /// <summary>
        /// Customizing Hook to overwrite the default Textbox. "Value" still needs proper binding for the increment/decrement buttons to work properly
        /// </summary>
        public FrameworkElement InnerContent {
            get => (FrameworkElement)GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(FrameworkElement), typeof(StepperControl), new UIPropertyMetadata(null));

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

            var tb = GetTemplateChild("PART_Textbox") as UnitTextBox;
            if (tb != null) {
                tb.LostFocus += PART_TextBox_LostFocus;
            }

            var cc = GetTemplateChild("PART_ContentControl") as ContentControl;
            if (cc != null) {
                cc.LostFocus += PART_TextBox_LostFocus;
            }
        }

        private void Button_PART_Increment_Click(object sender, RoutedEventArgs e) {
            if (Value + StepSize <= MaxValue) {
                Value += StepSize;
            } else {
                Value = MaxValue;
            }
        }

        private void Button_PART_Decrement_Click(object sender, RoutedEventArgs e) {
            if (Value - StepSize >= MinValue) {
                Value -= StepSize;
            } else {
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

    /// <summary> Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project. Add
    /// this XmlNamespace attribute to the root element of the markup file where it is to be used:
    ///
    /// xmlns:MyNamespace="clr-namespace:NINA.CustomControlLibrary"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project. Add
    /// this XmlNamespace attribute to the root element of the markup file where it is to be used:
    ///
    /// xmlns:MyNamespace="clr-namespace:NINA.CustomControlLibrary;assembly=NINA.CustomControlLibrary"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives to
    /// this project and Rebuild to avoid compilation errors:
    ///
    /// Right click on the target project in the Solution Explorer and "Add
    /// Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2) Go ahead and use your control in the XAML file.
    ///
    /// <MyNamespace:CustomControl1/>
    ///
    /// </summary>
    [TemplatePart(Name = "PART_TextBox", Type = typeof(UnitTextBox))]
    [TemplatePart(Name = "PART_Decrement", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Increment", Type = typeof(Button))]
    public class PrecisionStepperControl : UserControl {

        static PrecisionStepperControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PrecisionStepperControl), new FrameworkPropertyMetadata(typeof(PrecisionStepperControl)));
        }

        public static readonly DependencyProperty ButtonForegroundBrushProperty =
           DependencyProperty.Register(nameof(ButtonForegroundBrush), typeof(Brush), typeof(PrecisionStepperControl), new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush ButtonForegroundBrush {
            get => (Brush)GetValue(ButtonForegroundBrushProperty);
            set => SetValue(ButtonForegroundBrushProperty, value);
        }

        public static readonly DependencyProperty AddSVGProperty =
           DependencyProperty.Register(nameof(AddSVG), typeof(Geometry), typeof(PrecisionStepperControl), new UIPropertyMetadata(null));

        public Geometry AddSVG {
            get => (Geometry)GetValue(AddSVGProperty);
            set => SetValue(AddSVGProperty, value);
        }

        public static readonly DependencyProperty SubstractSVGProperty =
           DependencyProperty.Register(nameof(SubstractSVG), typeof(Geometry), typeof(PrecisionStepperControl), new UIPropertyMetadata(null));

        public Geometry SubstractSVG {
            get => (Geometry)GetValue(SubstractSVGProperty);
            set => SetValue(SubstractSVGProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
           DependencyProperty.Register(nameof(Value), typeof(double), typeof(PrecisionStepperControl), new UIPropertyMetadata(0.0d));

        public double Value {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Round(value, Precision));
        }

        public static readonly DependencyProperty MinValueProperty =
           DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(PrecisionStepperControl), new UIPropertyMetadata(double.MinValue));

        public double MinValue {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty =
           DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(PrecisionStepperControl), new UIPropertyMetadata(double.MaxValue));

        public double MaxValue {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public static readonly DependencyProperty StepSizeProperty =
           DependencyProperty.Register(nameof(StepSize), typeof(double), typeof(PrecisionStepperControl), new UIPropertyMetadata(1.0d));

        public double StepSize {
            get => (double)GetValue(StepSizeProperty);
            set => SetValue(StepSizeProperty, value);
        }

        public static readonly DependencyProperty PrecisionProperty =
           DependencyProperty.Register(nameof(Precision), typeof(int), typeof(PrecisionStepperControl), new UIPropertyMetadata(4));

        public int Precision {
            get => (int)GetValue(PrecisionProperty);
            set => SetValue(PrecisionProperty, value);
        }

        public static readonly DependencyProperty UnitProperty =
           DependencyProperty.Register(nameof(Unit), typeof(string), typeof(PrecisionStepperControl), new UIPropertyMetadata(string.Empty));

        public string Unit {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        /// <summary>
        /// Customizing Hook to overwrite the default Textbox. "Value" still needs proper binding for the increment/decrement buttons to work properly
        /// </summary>
        public FrameworkElement InnerContent {
            get => (FrameworkElement)GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(FrameworkElement), typeof(PrecisionStepperControl), new UIPropertyMetadata(null));

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

            var tb = GetTemplateChild("PART_Textbox") as UnitTextBox;
            if (tb != null) {
                tb.LostFocus += PART_TextBox_LostFocus;
            }

            var cc = GetTemplateChild("PART_ContentControl") as ContentControl;
            if (cc != null) {
                cc.LostFocus += PART_TextBox_LostFocus;
            }
        }

        private void Button_PART_Increment_Click(object sender, RoutedEventArgs e) {
            if (Value + StepSize <= MaxValue) {
                Value += StepSize;
            } else {
                Value = MaxValue;
            }
        }

        private void Button_PART_Decrement_Click(object sender, RoutedEventArgs e) {
            if (Value - StepSize >= MinValue) {
                Value -= StepSize;
            } else {
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