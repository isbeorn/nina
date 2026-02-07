#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;

namespace NINA.Core.Utility.Notification {
    public sealed class MainWindowWorkAreaProvider : INotificationWorkAreaProvider {
        public bool IsTopMost => false;

        public Window Owner => Application.Current?.MainWindow;
        public Rect GetWorkArea() {
            var main = Application.Current?.MainWindow;
            if (main == null) {
                var workArea = SystemParameters.WorkArea;
                return new Rect(workArea.Left, workArea.Top, workArea.Width, workArea.Height);
            }

            // When minimized, use RestoreBounds
            if (main.WindowState == WindowState.Minimized) {
                var rb = main.RestoreBounds;
                return new Rect(rb.Left, rb.Top, rb.Width, rb.Height);
            }

            // Visible: use actual location in screen coords
            var topLeft = main.PointToScreen(new Point(0, 0));
            return new Rect(topLeft.X, topLeft.Y, main.ActualWidth, main.ActualHeight);
        }
    }
}