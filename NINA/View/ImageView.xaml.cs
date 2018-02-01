using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View
{
    /// <summary>
    /// Interaction logic for ImageView.xaml
    /// </summary>
    public partial class ImageView : UserControl
    {
        Point? lastCenterPositionOnTarget;
        Point? lastMousePositionOnTarget;
        Point? lastDragPoint;

        double fittingScale = 1;

        //0: 100%; 1: fit to screen; 2: custom
        int mode = 0;

        public ImageView() {
            InitializeComponent();

            sv.SizeChanged += Sv_SizeChanged;
            sv.ScrollChanged += OnsvScrollChanged;
            //sv.MouseLeftButtonUp += OnMouseLeftButtonUp;
            //sv.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            sv.PreviewMouseWheel += OnPreviewMouseWheel;
            
            //sv.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            sv.MouseMove += OnMouseMove;
            scaleTransform.ScaleX = fittingScale;
            scaleTransform.ScaleY = fittingScale;
            tbScale.Text = 1d.ToString("P0", CultureInfo.InvariantCulture);
        }

        
        
        public static readonly DependencyProperty ImageAreaContentProperty =
            DependencyProperty.Register(nameof(ImageAreaContent), typeof(object), typeof(ImageView));

        public object ImageAreaContent {
            get { return (object)GetValue(ImageAreaContentProperty); }
            set { SetValue(ImageAreaContentProperty, value); }
        }

        public static readonly DependencyProperty ButtonHeaderContentProperty =
            DependencyProperty.Register(nameof(ButtonHeaderContent), typeof(object), typeof(ImageView));

        public object ButtonHeaderContent {
            get { return (object)GetValue(ButtonHeaderContentProperty); }
            set { SetValue(ButtonHeaderContentProperty, value); }
        }
        
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(nameof(Image), typeof(BitmapSource), typeof(ImageView));

        public BitmapSource Image {
            get { return (BitmapSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
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

        //void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        //    var mousePos = e.GetPosition(sv);
        //    if (mousePos.X <= sv.ViewportWidth && mousePos.Y <
        //        sv.ViewportHeight) //make sure we still can use the scrollbars
        //    {
        //        sv.Cursor = Cursors.SizeAll;
        //        lastDragPoint = mousePos;
        //        Mouse.Capture(sv);
        //    }
        //}

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            mode = 2;
            lastMousePositionOnTarget = Mouse.GetPosition(PART_Canvas);

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
            lastCenterPositionOnTarget = sv.TranslatePoint(centerOfViewport, PART_Canvas);
            e.Handled = true;
        }

        //void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        //    sv.Cursor = Cursors.Arrow;
        //    sv.ReleaseMouseCapture();
        //    lastDragPoint = null;
        //}

        private void Zoom(double val) {
            if (val < 0) { val = 0; }
            scaleTransform.ScaleX = val;
            scaleTransform.ScaleY = val;

            tbScale.Text = val.ToString("P0", CultureInfo.InvariantCulture);


        }

        void OnSliderValueChanged(object sender,
             RoutedPropertyChangedEventArgs<double> e) {
            scaleTransform.ScaleX = e.NewValue;
            scaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(sv.ViewportWidth / 2,
                                             sv.ViewportHeight / 2);
            lastCenterPositionOnTarget = sv.TranslatePoint(centerOfViewport, PART_Canvas);
        }

        void RecalculateScalingFactors() {
            if (PART_Image?.ActualWidth > 0) {
                if (mode == 0) {
                    Zoom(1);
                } else if (mode == 1) {
                    var scale = Math.Min(sv.ActualWidth / PART_Image.ActualWidth, sv.ActualHeight / PART_Image.ActualHeight);
                    if (fittingScale != scale) {
                        var newScaleFactor = fittingScale / scale;
                        fittingScale = scale;
                        Zoom(fittingScale);
                    }
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
                              sv.TranslatePoint(centerOfViewport, PART_Canvas);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                } else {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(PART_Canvas);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue) {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / PART_Canvas.ActualWidth;
                    double multiplicatorY = e.ExtentHeight / PART_Canvas.ActualHeight;

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
            mode = 2;
            Zoom(scaleTransform.ScaleX + scaleTransform.ScaleX * 0.25);
            var centerOfViewport = new Point(sv.ViewportWidth / 2,
                                                         sv.ViewportHeight / 2);
            lastCenterPositionOnTarget =
                  sv.TranslatePoint(centerOfViewport, PART_Canvas);

        }
        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e) {
            mode = 2;
            Zoom(scaleTransform.ScaleX - scaleTransform.ScaleX * 0.25);
            var centerOfViewport = new Point(sv.ViewportWidth / 2,
                                                         sv.ViewportHeight / 2);
            lastCenterPositionOnTarget =
                  sv.TranslatePoint(centerOfViewport, PART_Canvas);
        }
        private void ButtonZoomReset_Click(object sender, RoutedEventArgs e) {
            mode = 1;
            RecalculateScalingFactors();
            Zoom(fittingScale);
        }

        private void ButtonZoomOneToOne_Click(object sender, RoutedEventArgs e) {
            mode = 0;
            Zoom(1);
        }
    }
}
