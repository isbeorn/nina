#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.Core.Utility.WindowService {

    public class CustomWindow : Window {
        private bool suppressInitialPaint = true;
        private double initialOpacity = 1d;

        public CustomWindow() {
            FixInitialLayout();
        }

        public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(Window), null);

        public ICommand CloseCommand {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        private void FixInitialLayout() {
            SourceInitialized += Window_SourceInitialized;
            Loaded += Window_Loaded;
        }

        private void Window_SourceInitialized(object sender, EventArgs e) {
            if (suppressInitialPaint) {
                initialOpacity = Opacity;
                Opacity = 0d;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            if (!suppressInitialPaint) {
                return;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
                InvalidateMeasure();
                UpdateLayout();
                Opacity = initialOpacity;
                suppressInitialPaint = false;
                Loaded -= Window_Loaded;
                SourceInitialized -= Window_SourceInitialized;
            }));
        }
    }
}
