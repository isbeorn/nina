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