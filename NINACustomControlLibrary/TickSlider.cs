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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NINACustomControlLibrary {

    public class TickSlider : Slider {
        private DispatcherTimer timer;
        private double tickValue;
        private bool positiveDirection = false;

        public TickSlider() {
            IsMouseCaptureWithinChanged += OnIsMouseCapturedChanged;
            timer = new DispatcherTimer(TimeSpan.FromSeconds(InitialSpeedTickValue), DispatcherPriority.Input, UpdateActualValue, Dispatcher);
            timer.IsEnabled = false;
            Value = 0.5;
            Minimum = 0;
            Maximum = 1;
        }

        private void OnIsMouseCapturedChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if ((bool)e.NewValue) {
                timer.IsEnabled = true;
                tickValue = InitialSpeedTickValue;
            } else {
                Value = 0.5;
                tickValue = 0.5;
                timer.IsEnabled = false;
                timer.Interval = TimeSpan.FromSeconds(InitialSpeedTickValue);
            }
        }

        private void UpdateActualValue(object sender, EventArgs e) {
            tickValue *= SpeedTickPercentageChangePerTick;
            if (positiveDirection && Value < 0.5 || !positiveDirection && Value >= 0.5) {
                positiveDirection = Value >= 0.5;
                tickValue = InitialSpeedTickValue;
            }
            timer.Interval = TimeSpan.FromSeconds(tickValue);
            double updatedValue = (Math.Pow(2, (Math.Abs((Value - 0.5) * 2))) - 1) * MaxActualValueIncreasePerTick;
            ActualValue = (Value - 0.5) > 0 ? ActualValue + updatedValue : ActualValue - updatedValue;
            if (ActualValue < ActualValueMinimum) {
                ActualValue = ActualValueMinimum;
            }

            if (ActualValue > ActualValueMaximum) {
                ActualValue = ActualValueMaximum;
            }
        }

        public double MaxActualValueIncreasePerTick {
            get { return (double)GetValue(MaxActualValueIncreasePerTickProperty); }
            set {
                SetValue(MaxActualValueIncreasePerTickProperty, value);
            }
        }

        public double SpeedTickPercentageChangePerTick {
            get { return (double)GetValue(SpeedTickPercentageChangePerTickProperty); }
            set {
                SetValue(SpeedTickPercentageChangePerTickProperty, value);
            }
        }

        public double InitialSpeedTickValue {
            get { return (double)GetValue(InitialSpeedTickValueProperty); }
            set {
                SetValue(InitialSpeedTickValueProperty, value);
            }
        }

        public double ActualValue {
            get { return (double)GetValue(ActualValueProperty); }
            set {
                SetValue(ActualValueProperty, value);
            }
        }

        public double ActualValueMinimum {
            get { return (double)GetValue(MinimumActualValueProperty); }
            set {
                SetValue(MinimumActualValueProperty, value);
            }
        }

        public double ActualValueMaximum {
            get { return (double)GetValue(MaximumActualValueProperty); }
            set {
                SetValue(MaximumActualValueProperty, value);
            }
        }

        public static readonly DependencyProperty MaxActualValueIncreasePerTickProperty =
            DependencyProperty.Register(nameof(MaxActualValueIncreasePerTick), typeof(double), typeof(TickSlider), new UIPropertyMetadata((double)0.5));

        public static readonly DependencyProperty InitialSpeedTickValueProperty =
            DependencyProperty.Register(nameof(InitialSpeedTickValue), typeof(double), typeof(TickSlider), new UIPropertyMetadata((double)0.5));

        public static readonly DependencyProperty ActualValueProperty =
            DependencyProperty.Register(nameof(ActualValue), typeof(double), typeof(TickSlider), new UIPropertyMetadata((double)0));

        public static readonly DependencyProperty MinimumActualValueProperty =
            DependencyProperty.Register(nameof(ActualValueMinimum), typeof(double), typeof(TickSlider), new UIPropertyMetadata(double.MinValue));

        public static readonly DependencyProperty MaximumActualValueProperty =
            DependencyProperty.Register(nameof(ActualValueMaximum), typeof(double), typeof(TickSlider), new UIPropertyMetadata(double.MaxValue));

        public static readonly DependencyProperty SpeedTickPercentageChangePerTickProperty =
            DependencyProperty.Register(nameof(SpeedTickPercentageChangePerTick), typeof(double), typeof(TickSlider), new UIPropertyMetadata((double)0.9));
    }
}