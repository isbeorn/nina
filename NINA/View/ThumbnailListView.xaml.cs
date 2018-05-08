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