#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows.Controls;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for ThumbnailListView.xaml
    /// </summary>
    public partial class ThumbnailListView : UserControl {

        public ThumbnailListView() {
            InitializeComponent();
        }

        private bool _autoScroll = true;

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentWidthChange == 0) {   // Content unchanged : user scroll event
                if (ScrollViewer.HorizontalOffset == ScrollViewer.ScrollableWidth) {
                    // Scroll bar is most right position Set autoscroll mode
                    _autoScroll = true;
                } else {
                    // Scroll bar isn't in most right position Unset auto-scroll mode
                    _autoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (_autoScroll && e.ExtentWidthChange != 0) {
                // Content changed and auto-scroll mode set Autoscroll
                ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.ExtentWidth);
            }
        }
    }
}
