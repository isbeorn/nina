#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Input;

namespace NINA.WPF.Base.Behaviors {

    public class MouseCommandBehavior {

        public static MouseCommandBehavior GetBehavior(DependencyObject obj) {
            return (MouseCommandBehavior)obj.GetValue(BehaviorProperty);
        }

        public static readonly DependencyProperty BehaviorProperty =
          DependencyProperty.RegisterAttached(
              "Behavior",
              typeof(MouseCommandBehavior),
              typeof(MouseCommandBehavior),
              new PropertyMetadata(new MouseCommandBehavior()));

        public static readonly DependencyProperty IsEnabledProperty =
          DependencyProperty.RegisterAttached(
              "Enabled",
              typeof(bool),
              typeof(MouseCommandBehavior),
              new PropertyMetadata(false, RegisterMouse));

        public static bool GetEnabled(DependencyObject obj) {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetEnabled(DependencyObject obj, bool value) {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void RegisterMouse(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var element = (UIElement)d;
            var isEnabled = (bool)(e.NewValue);

            var instance = GetBehavior(element);

            if (isEnabled) {
                element.MouseEnter += instance.ElementMouseEnter;
                element.MouseLeave += instance.ElementMouseLeave;
                element.MouseDown += instance.ElementMouseDown;
                element.MouseUp += instance.ElementMouseUp;
                element.MouseMove += instance.ElementMouseMove;
            } else {
                element.MouseEnter -= instance.ElementMouseEnter;
                element.MouseLeave -= instance.ElementMouseLeave;
                element.MouseDown -= instance.ElementMouseDown;
                element.MouseUp -= instance.ElementMouseUp;
                element.MouseMove -= instance.ElementMouseMove;
            }
        }

        private void ElementMouseEnter(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left);
                ElementMouseDown(sender, args);
            }

            if (e.RightButton == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Right);
                ElementMouseDown(sender, args);
            }

            if (e.MiddleButton == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Middle);
                ElementMouseDown(sender, args);
            }

            if (e.XButton1 == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.XButton1);
                ElementMouseDown(sender, args);
            }

            if (e.XButton2 == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.XButton2);
                ElementMouseDown(sender, args);
            }
        }

        private void ElementMouseLeave(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left);
                ElementMouseUp(sender, args);
            }

            if (e.RightButton == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Right);
                ElementMouseUp(sender, args);
            }

            if (e.MiddleButton == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Middle);
                ElementMouseUp(sender, args);
            }

            if (e.XButton1 == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.XButton1);
                ElementMouseUp(sender, args);
            }

            if (e.XButton2 == MouseButtonState.Pressed) {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.XButton2);
                ElementMouseUp(sender, args);
            }
        }

        private void ElementMouseUp(object sender, MouseButtonEventArgs e) {
            var element = (FrameworkElement)sender;

            if (e.ChangedButton == MouseButton.Right) {
                var cmd = GetRightMouseUpCommand(element);
                cmd?.Execute(null);
            } else if (e.ChangedButton == MouseButton.Left) {
                var cmd = GetLeftMouseUpCommand(element);
                cmd?.Execute(null);
            }
        }

        private void ElementMouseMove(object sender, MouseEventArgs e) {
            var element = (FrameworkElement)sender;

            if (e.RightButton == MouseButtonState.Pressed) {
                var cmd = GetRightMouseMoveCommand(element);
                var point = e.GetPosition(element);
                cmd?.Execute(point);
            } else if (e.LeftButton == MouseButtonState.Pressed) {
                var cmd = GetLeftMouseMoveCommand(element);
                var point = e.GetPosition(element);
                cmd?.Execute(point);
            }
        }

        private void ElementMouseDown(object sender, MouseButtonEventArgs e) {
            var element = (FrameworkElement)sender;

            if (e.ChangedButton == MouseButton.Right) {
                var cmd = GetRightMouseDownCommand(element);
                var position = e.GetPosition(element);
                cmd?.Execute(position);
            } else if (e.ChangedButton == MouseButton.Left) {
                var cmd = GetLeftMouseDownCommand(element);
                var position = e.GetPosition(element);
                cmd?.Execute(position);
            }
        }

        public static readonly DependencyProperty RightMouseDownCommandProperty =
            DependencyProperty.RegisterAttached("RightMouseDownCommand", typeof(ICommand), typeof(MouseCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetRightMouseDownCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(RightMouseDownCommandProperty);
        }

        public static void SetRightMouseDownCommand(DependencyObject obj, bool value) {
            obj.SetValue(RightMouseDownCommandProperty, value);
        }

        public static readonly DependencyProperty RightMouseUpCommandProperty =
            DependencyProperty.RegisterAttached("RightMouseUpCommand", typeof(ICommand), typeof(MouseCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetRightMouseUpCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(RightMouseUpCommandProperty);
        }

        public static void SetRightMouseUpCommand(DependencyObject obj, bool value) {
            obj.SetValue(RightMouseUpCommandProperty, value);
        }

        public static readonly DependencyProperty RightMouseMoveCommandProperty =
            DependencyProperty.RegisterAttached("RightMouseMoveCommand", typeof(ICommand), typeof(MouseCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetRightMouseMoveCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(RightMouseMoveCommandProperty);
        }

        public static void SetRightMouseMoveCommand(DependencyObject obj, bool value) {
            obj.SetValue(RightMouseMoveCommandProperty, value);
        }

        public static readonly DependencyProperty LeftMouseDownCommandProperty =
            DependencyProperty.RegisterAttached("LeftMouseDownCommand", typeof(ICommand), typeof(MouseCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetLeftMouseDownCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(LeftMouseDownCommandProperty);
        }

        public static void SetLeftMouseDownCommand(DependencyObject obj, bool value) {
            obj.SetValue(LeftMouseDownCommandProperty, value);
        }

        public static readonly DependencyProperty LeftMouseUpCommandProperty =
            DependencyProperty.RegisterAttached("LeftMouseUpCommand", typeof(ICommand), typeof(MouseCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetLeftMouseUpCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(LeftMouseUpCommandProperty);
        }

        public static void SetLeftMouseUpCommand(DependencyObject obj, bool value) {
            obj.SetValue(LeftMouseUpCommandProperty, value);
        }

        public static readonly DependencyProperty LeftMouseMoveCommandProperty =
            DependencyProperty.RegisterAttached("LeftMouseMoveCommand", typeof(ICommand), typeof(MouseCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetLeftMouseMoveCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(LeftMouseMoveCommandProperty);
        }

        public static void SetLeftMouseMoveCommand(DependencyObject obj, bool value) {
            obj.SetValue(LeftMouseMoveCommandProperty, value);
        }
    }
}