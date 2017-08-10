using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View {
    /// <summary>
    /// Interaction logic for ImageAreaView.xaml
    /// </summary>
    public partial class ImageAreaView : UserControl {
        public ImageAreaView() {
            InitializeComponent();
        }

        Point scrollMousePoint = new Point();
        double hOff = 1;
        double vOff = 1;

        private void sv_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            scrollMousePoint = e.GetPosition(sv);
            hOff = sv.HorizontalOffset;
            vOff = sv.VerticalOffset;
            sv.CaptureMouse();
        }

        private void sv_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {            
            sv.ReleaseMouseCapture();
        }

        private void sv_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (sv.IsMouseCaptured) {
                sv.ScrollToHorizontalOffset(hOff + (scrollMousePoint.X - e.GetPosition(sv).X));
                sv.ScrollToVerticalOffset(vOff + (scrollMousePoint.Y - e.GetPosition(sv).Y));
            }
        }        
    }
}
