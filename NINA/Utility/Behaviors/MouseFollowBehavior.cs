#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace NINA.Utility.Behaviors {

    internal class MouseFollowBehavior {

        public static MouseFollowBehavior GetBehavior(DependencyObject obj) {
            return (MouseFollowBehavior)obj.GetValue(BehaviorProperty);
        }

        public static readonly DependencyProperty BehaviorProperty =
          DependencyProperty.RegisterAttached(
              "Behavior",
              typeof(MouseFollowBehavior),
              typeof(MouseFollowBehavior),
              new PropertyMetadata(new MouseFollowBehavior()));

        public static readonly DependencyProperty IsEnabledProperty =
          DependencyProperty.RegisterAttached(
              "Enabled",
              typeof(bool),
              typeof(MouseFollowBehavior),
              new PropertyMetadata(false, RegisterAutoMove));

        public static bool GetEnabled(DependencyObject obj) {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetEnabled(DependencyObject obj, bool value) {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void RegisterAutoMove(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var element = (Popup)d;
            var isEnabled = (bool)(e.NewValue);

            var instance = GetBehavior(element);

            if (isEnabled) {
                element.Opened += instance.Opened;
                element.Closed += instance.Closed;
            } else {
                element.Opened -= instance.Opened;
                element.Closed -= instance.Closed;
            }
        }

        private Popup popup = null;

        internal void Opened(object sender, EventArgs e) {
            popup = (Popup)sender;
            var placementTarget = popup.PlacementTarget;
            if (placementTarget != null) {
                var position = Mouse.GetPosition(placementTarget as IInputElement);
                Move(position, popup);
                placementTarget.MouseMove += Target_MouseMove;
            }
        }

        private void Target_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            if (sender == null) { return; }
            var target = (IInputElement)sender;
            var position = e.GetPosition(target);
            Move(position, popup);
        }

        private void Move(Point position, Popup popup) {
            if (popup == null) {
                return;
            }
            popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            popup.HorizontalOffset = position.X + 5;
            popup.VerticalOffset = position.Y + 5;
        }

        internal void Closed(object sender, EventArgs e) {
            popup = (Popup)sender;
            var target = popup.PlacementTarget;
            if (target != null) {
                target.MouseMove -= Target_MouseMove;
            }
            popup = null;
        }
    }
}