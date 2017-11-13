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
        Point? lastCenterPositionOnTarget;
        Point? lastMousePositionOnTarget;
        Point? lastDragPoint;

        double fittingScale = 1;

        public ImageAreaView() {
            InitializeComponent();

            sv.SizeChanged += Sv_SizeChanged;
            sv.ScrollChanged += OnsvScrollChanged;
            sv.MouseLeftButtonUp += OnMouseLeftButtonUp;
            sv.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            sv.PreviewMouseWheel += OnPreviewMouseWheel;

            sv.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            sv.MouseMove += OnMouseMove;
            scaleTransform.ScaleX = fittingScale;
            scaleTransform.ScaleY = fittingScale;
        }

        private void Sv_SizeChanged(object sender, SizeChangedEventArgs e) {
            RecalculateScalingFactors();
        }

        void OnMouseMove(object sender, MouseEventArgs e) {
            if (lastDragPoint.HasValue) {
                Point posNow = e.GetPosition(sv);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                sv.ScrollToHorizontalOffset(sv.HorizontalOffset - dX);
                sv.ScrollToVerticalOffset(sv.VerticalOffset - dY);
            }
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var mousePos = e.GetPosition(sv);
            if (mousePos.X <= sv.ViewportWidth && mousePos.Y <
                sv.ViewportHeight) //make sure we still can use the scrollbars
            {
                sv.Cursor = Cursors.SizeAll;
                lastDragPoint = mousePos;
                Mouse.Capture(sv);
            }
        }

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            lastMousePositionOnTarget = Mouse.GetPosition(grid);

            var val = scaleTransform.ScaleX;
            if (e.Delta > 0) {
                val += val * .25;
            }
            if (e.Delta < 0) {
                val -= val * .25;
            }

            Zoom(val);

            var centerOfViewport = new Point(sv.ViewportWidth / 2,
                                             sv.ViewportHeight / 2);
            lastCenterPositionOnTarget = sv.TranslatePoint(centerOfViewport, grid);
            e.Handled = true;
        }

        private void Zoom(double val) {
            if (val < 0) { val = 0; }
            scaleTransform.ScaleX = val;
            scaleTransform.ScaleY = val;


        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            sv.Cursor = Cursors.Arrow;
            sv.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        void OnSliderValueChanged(object sender,
             RoutedPropertyChangedEventArgs<double> e) {
            scaleTransform.ScaleX = e.NewValue;
            scaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(sv.ViewportWidth / 2,
                                             sv.ViewportHeight / 2);
            lastCenterPositionOnTarget = sv.TranslatePoint(centerOfViewport, grid);
        }

        void RecalculateScalingFactors() {
            if (image?.ActualWidth > 0) {
                var scale = sv.ActualWidth / image.ActualWidth;
                if (fittingScale != scale) {
                    var newScaleFactor = fittingScale / scale;
                    fittingScale = scale;
                    Zoom(scaleTransform.ScaleX * newScaleFactor);
                }
            }
        }

        void OnsvScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0) {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue) {
                    if (lastCenterPositionOnTarget.HasValue) {
                        var centerOfViewport = new Point(sv.ViewportWidth / 2,
                                                         sv.ViewportHeight / 2);
                        Point centerOfTargetNow =
                              sv.TranslatePoint(centerOfViewport, grid);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                } else {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(grid);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue) {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / grid.ActualWidth;
                    double multiplicatorY = e.ExtentHeight / grid.ActualHeight;

                    double newOffsetX = sv.HorizontalOffset -
                                        dXInTargetPixels * multiplicatorX;
                    double newOffsetY = sv.VerticalOffset -
                                        dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY)) {
                        return;
                    }

                    sv.ScrollToHorizontalOffset(newOffsetX);
                    sv.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e) {
            Zoom(scaleTransform.ScaleX + scaleTransform.ScaleX * 0.25);
            var centerOfViewport = new Point(sv.ViewportWidth / 2,
                                                         sv.ViewportHeight / 2);
            lastCenterPositionOnTarget =
                  sv.TranslatePoint(centerOfViewport, grid);

        }
        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e) {
            Zoom(scaleTransform.ScaleX - scaleTransform.ScaleX * 0.25);
            var centerOfViewport = new Point(sv.ViewportWidth / 2,
                                                         sv.ViewportHeight / 2);
            lastCenterPositionOnTarget =
                  sv.TranslatePoint(centerOfViewport, grid);
        }
        private void ButtonZoomReset_Click(object sender, RoutedEventArgs e) {
            RecalculateScalingFactors();
            Zoom(fittingScale);
        }

        private void ButtonZoomOneToOne_Click(object sender, RoutedEventArgs e) {
            Zoom(1);
        }
    }
}
