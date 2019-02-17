#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using System.Windows.Input;

namespace NINA.Utility.Behaviors {

    internal class MouseWheelCommandBehavior {

        public static MouseWheelCommandBehavior GetBehavior(DependencyObject obj) {
            return (MouseWheelCommandBehavior)obj.GetValue(BehaviorProperty);
        }

        public static readonly DependencyProperty BehaviorProperty =
          DependencyProperty.RegisterAttached(
              "Behavior",
              typeof(MouseWheelCommandBehavior),
              typeof(MouseWheelCommandBehavior),
              new PropertyMetadata(new MouseWheelCommandBehavior()));

        public static readonly DependencyProperty IsEnabledProperty =
          DependencyProperty.RegisterAttached(
              "Enabled",
              typeof(bool),
              typeof(MouseWheelCommandBehavior),
              new PropertyMetadata(false, RegisterOnMouseWheel));

        public static bool GetEnabled(DependencyObject obj) {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetEnabled(DependencyObject obj, bool value) {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty MouseWheelCommandProperty =
            DependencyProperty.RegisterAttached("MouseWheelCommand", typeof(ICommand), typeof(MouseWheelCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetMouseWheelCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(MouseWheelCommandProperty);
        }

        public static void SetMouseWheelCommand(DependencyObject obj, bool value) {
            obj.SetValue(MouseWheelCommandProperty, value);
        }

        private static void RegisterOnMouseWheel(object sender, DependencyPropertyChangedEventArgs e) {
            var element = (UIElement)sender;
            var isEnabled = (bool)(e.NewValue);

            var instance = GetBehavior(element);

            if (isEnabled) {
                element.MouseWheel += instance.ElementMouseWheel;
            } else {
                element.MouseWheel -= instance.ElementMouseWheel;
            }
        }

        private void ElementMouseWheel(object sender, MouseWheelEventArgs e) {
            var element = (FrameworkElement)sender;
            var cmd = GetMouseWheelCommand(element);

            cmd?.Execute(new MouseWheelResult() { Delta = e.Delta });
        }
    }

    public class MouseWheelResult {
        public int Delta { get; set; }
    }
}