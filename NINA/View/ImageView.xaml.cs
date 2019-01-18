#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for ImageView.xaml
    /// </summary>
    public partial class ImageView : UserControl {
        private Point? lastCenterPositionOnTarget;
        private Point? lastMousePositionOnTarget;
        private Point? lastDragPoint;

        private double fittingScale = 1;

        public object PART_ScrollViewerBinding {
            get => PART_ScrollViewer;
        }

        public ImageView() {
            InitializeComponent();

            PART_ScrollViewer.SizeChanged += Sv_SizeChanged;
            PART_ScrollViewer.ScrollChanged += OnsvScrollChanged;
            PART_ScrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

            PART_ScrollViewer.MouseLeftButtonUp += OnMouseLeftButtonUp;
            PART_ScrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            PART_ScrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            PART_ScrollViewer.MouseMove += OnMouseMove;

            PART_ScaleTransform.ScaleX = fittingScale;
            PART_ScaleTransform.ScaleY = fittingScale;
            PART_TextblockScale.Text = 1d.ToString("P0", CultureInfo.InvariantCulture);
        }

        public static readonly DependencyProperty ScrollEnabledProperty =
            DependencyProperty.Register(nameof(ScrollEnabled), typeof(bool), typeof(ImageView), new PropertyMetadata(true));

        public bool ScrollEnabled {
            get { return (bool)GetValue(ScrollEnabledProperty); }
            set { SetValue(ScrollEnabledProperty, value); }
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

        private void OnMouseMove(object sender, MouseEventArgs e) {
            if (lastDragPoint.HasValue) {
                Point posNow = e.GetPosition(PART_ScrollViewer);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                PART_ScrollViewer.ScrollToHorizontalOffset(PART_ScrollViewer.HorizontalOffset - dX);
                PART_ScrollViewer.ScrollToVerticalOffset(PART_ScrollViewer.VerticalOffset - dY);
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var mousePos = e.GetPosition(PART_ScrollViewer);
            if (!PART_ImageViewContent.IsHitTestVisible || !PART_ImageViewContent.IsMouseOver) {
                if (mousePos.X <= PART_ScrollViewer.ViewportWidth && mousePos.Y <
                PART_ScrollViewer.ViewportHeight) {
                    PART_ScrollViewer.Cursor = Cursors.SizeAll;
                    lastDragPoint = mousePos;
                    Mouse.Capture(PART_ScrollViewer);
                }
            }
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            if (ScrollEnabled) {
                lastMousePositionOnTarget = Mouse.GetPosition(PART_Canvas);

                var val = PART_ScaleTransform.ScaleX;
                if (e.Delta > 0) {
                    val += val * .25;
                }
                if (e.Delta < 0) {
                    val -= val * .25;
                }

                Zoom(val);

                var centerOfViewport = new Point(PART_ScrollViewer.ViewportWidth / 2,
                                                 PART_ScrollViewer.ViewportHeight / 2);
                lastCenterPositionOnTarget = PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Canvas);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            PART_ScrollViewer.Cursor = Cursors.Arrow;
            PART_ScrollViewer.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        private void Zoom(double val) {
            RecalculateScalingFactors();
            if (val < fittingScale) {
                val = fittingScale;
            } else if (val < 0) {
                val = 0;
            }

            PART_ScaleTransform.ScaleX = val;
            PART_ScaleTransform.ScaleY = val;

            PART_TextblockScale.Text = val.ToString("P0", CultureInfo.InvariantCulture);
        }

        private void OnSliderValueChanged(object sender,
             RoutedPropertyChangedEventArgs<double> e) {
            PART_ScaleTransform.ScaleX = e.NewValue;
            PART_ScaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(PART_ScrollViewer.ViewportWidth / 2,
                                             PART_ScrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Canvas);
        }

        private void RecalculateScalingFactors() {
            if (PART_Image?.ActualWidth > 0) {
                var scale = Math.Min(PART_ScrollViewer.ActualWidth / PART_Image.ActualWidth, PART_ScrollViewer.ActualHeight / PART_Image.ActualHeight);
                if (fittingScale != scale) {
                    var newScaleFactor = fittingScale / scale;
                    fittingScale = scale;
                }
                /*if (mode == 0) {
                    Zoom(1);
                } else if (mode == 1) {
                    Zoom(fittingScale);
                }*/
            }
        }

        private void OnsvScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0) {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue) {
                    if (lastCenterPositionOnTarget.HasValue) {
                        var centerOfViewport = new Point(PART_ScrollViewer.ViewportWidth / 2,
                                                         PART_ScrollViewer.ViewportHeight / 2);
                        Point centerOfTargetNow =
                              PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Canvas);

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

                    double newOffsetX = PART_ScrollViewer.HorizontalOffset -
                                        dXInTargetPixels * multiplicatorX;
                    double newOffsetY = PART_ScrollViewer.VerticalOffset -
                                        dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY)) {
                        return;
                    }

                    PART_ScrollViewer.ScrollToHorizontalOffset(newOffsetX);
                    PART_ScrollViewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e) {
            Zoom(PART_ScaleTransform.ScaleX + PART_ScaleTransform.ScaleX * 0.25);
            var centerOfViewport = new Point(PART_ScrollViewer.ViewportWidth / 2,
                                                         PART_ScrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget =
                  PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Canvas);
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e) {
            Zoom(PART_ScaleTransform.ScaleX - PART_ScaleTransform.ScaleX * 0.25);
            var centerOfViewport = new Point(PART_ScrollViewer.ViewportWidth / 2,
                                                         PART_ScrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget =
                  PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Canvas);
        }

        private void ButtonZoomReset_Click(object sender, RoutedEventArgs e) {
            RecalculateScalingFactors();
            Zoom(fittingScale * 0.9);
            Zoom(fittingScale);
        }

        private void ButtonZoomOneToOne_Click(object sender, RoutedEventArgs e) {
            Zoom(1);
        }
    }
}