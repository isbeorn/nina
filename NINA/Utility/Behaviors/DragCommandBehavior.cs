using System.Windows;
using System.Windows.Input;

namespace NINA.Utility.Behaviors {

    internal class DragCommandBehavior {
        private static DragCommandBehavior _instance = new DragCommandBehavior();

        public static DragCommandBehavior Instance {
            get { return _instance; }
            set { _instance = value; }
        }

        public static bool GetDrag(DependencyObject obj) {
            return (bool)obj.GetValue(IsDragProperty);
        }

        public static void SetDrag(DependencyObject obj, bool value) {
            obj.SetValue(IsDragProperty, value);
        }

        public static readonly DependencyProperty IsDragProperty =
          DependencyProperty.RegisterAttached(
              "Drag",
              typeof(bool),
              typeof(DragCommandBehavior),
              new PropertyMetadata(false, OnDragChanged));

        public static readonly DependencyProperty ResizeBoundaryProperty =
            DependencyProperty.RegisterAttached("ResizeBoundary", typeof(double), typeof(DragCommandBehavior), new PropertyMetadata(double.NaN));

        public static double GetResizeBoundary(DependencyObject obj) {
            return (double)obj.GetValue(ResizeBoundaryProperty);
        }

        public static void SetResizeBoundary(DependencyObject obj, bool value) {
            obj.SetValue(ResizeBoundaryProperty, value);
        }
        
        //private RectangleDragMode _subSampleRectangleDragMode;

        private static void OnDragChanged(object sender, DependencyPropertyChangedEventArgs e) {
            // ignoring error checking
            var element = (UIElement)sender;
            var isDrag = (bool)(e.NewValue);

            Instance = new DragCommandBehavior();

            if (isDrag) {
                element.MouseLeftButtonDown += Instance.ElementOnMouseLeftButtonDown;
                element.MouseLeftButtonUp += Instance.ElementOnMouseLeftButtonUp;
                element.MouseMove += Instance.ElementOnMouseMove;
                element.MouseLeave += Element_MouseLeave;
            } else {
                element.MouseLeftButtonDown -= Instance.ElementOnMouseLeftButtonDown;
                element.MouseLeftButtonUp -= Instance.ElementOnMouseLeftButtonUp;
                element.MouseMove -= Instance.ElementOnMouseMove;
            }
        }

        private static void Element_MouseLeave(object sender, MouseEventArgs e) {
            Mouse.OverrideCursor = null;
        }

        private void ElementOnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs) {
            /*var parent = Application.Current.MainWindow;
            _mouseStartPosition2 = mouseButtonEventArgs.GetPosition(parent);*/

            var element = (FrameworkElement)sender;
            element.CaptureMouse();

            var parent = (UIElement)element.Parent;
            var boundary = GetResizeBoundary(element);
            var startPoint = mouseButtonEventArgs.GetPosition(element);
            var mousePos = mouseButtonEventArgs.GetPosition(parent);

            if (!double.IsNaN(boundary)) {
                if (startPoint.X < boundary && startPoint.Y < boundary) {
                    Mouse.OverrideCursor = Cursors.SizeNWSE;
                    _mode = DragMode.Resize_Top_Left;
                } else if (startPoint.X < boundary && startPoint.Y > element.Height - boundary) {
                    Mouse.OverrideCursor = Cursors.SizeNESW;
                    _mode = DragMode.Resize_Bottom_Left;
                } else if (startPoint.X > element.Width - boundary && startPoint.Y < boundary) {
                    Mouse.OverrideCursor = Cursors.SizeNESW;
                    _mode = DragMode.Resize_Top_Right;
                } else if (startPoint.X > element.Width - boundary && startPoint.Y > element.Height - boundary) {
                    Mouse.OverrideCursor = Cursors.SizeNWSE;
                    _mode = DragMode.Resize_Bottom_Right;
                } else if (startPoint.X < boundary) {
                    Mouse.OverrideCursor = Cursors.SizeWE;
                    _mode = DragMode.Resize_Left;
                } else if (startPoint.Y < boundary) {
                    Mouse.OverrideCursor = Cursors.SizeNS;
                    _mode = DragMode.Resize_Top;
                } else if (startPoint.X > element.Width - boundary) {
                    Mouse.OverrideCursor = Cursors.SizeWE;
                    _mode = DragMode.Resize_Right;
                } else if (startPoint.Y > element.Height - boundary) {
                    Mouse.OverrideCursor = Cursors.SizeNS;
                    _mode = DragMode.Resize_Bottom;
                } else {
                    Mouse.OverrideCursor = Cursors.SizeAll;
                    _mode = DragMode.Move;
                }
            } else {
                Mouse.OverrideCursor = Cursors.SizeAll;
                _mode = DragMode.Move;
            }

            _prevMousePos = mousePos;

            var cmd = GetDragStartCommand(element);
            cmd?.Execute(null);
        }

        private void ElementOnMouseLeftButtonUp(object sender, MouseButtonEventArgs mouseButtonEventArgs) {
            /*((UIElement)sender).ReleaseMouseCapture();
            _elementStartPosition2.X = Transform.X;
            _elementStartPosition2.Y = Transform.Y;*/
            var element = (FrameworkElement)sender;
            element.ReleaseMouseCapture();
            var cmd = GetDragStopCommand(element);
            cmd?.Execute(null);
        }

        private Point _prevMousePos;
        private DragMode _mode;

        private void ElementOnMouseMove(object sender, MouseEventArgs mouseEventArgs) {
            var element = (FrameworkElement)sender;

            var parent = (UIElement)element.Parent;
            var startPoint = mouseEventArgs.GetPosition(element);
            var mousePos = mouseEventArgs.GetPosition(parent);
            var hitTest = System.Windows.Media.VisualTreeHelper.HitTest(element, startPoint);
            var boundary = GetResizeBoundary(element);
            
            if(!element.IsMouseCaptured) {
                if (!double.IsNaN(boundary)) {
                    if (startPoint.X < boundary && startPoint.Y < boundary) {
                        Mouse.OverrideCursor = Cursors.SizeNWSE;
                    } else if (startPoint.X < boundary && startPoint.Y > element.Height - boundary) {
                        Mouse.OverrideCursor = Cursors.SizeNESW;
                    } else if (startPoint.X > element.Width - boundary && startPoint.Y < boundary) {
                        Mouse.OverrideCursor = Cursors.SizeNESW;
                    } else if (startPoint.X > element.Width - boundary && startPoint.Y > element.Height - boundary) {
                        Mouse.OverrideCursor = Cursors.SizeNWSE;
                    } else if (startPoint.X < boundary) {
                        Mouse.OverrideCursor = Cursors.SizeWE;
                    } else if (startPoint.Y < boundary) {
                        Mouse.OverrideCursor = Cursors.SizeNS;
                    } else if (startPoint.X > element.Width - boundary) {
                        Mouse.OverrideCursor = Cursors.SizeWE;
                    } else if (startPoint.Y > element.Height - boundary) {
                        Mouse.OverrideCursor = Cursors.SizeNS;
                    } else {
                        Mouse.OverrideCursor = Cursors.SizeAll;
                    }
                } else {
                    Mouse.OverrideCursor = Cursors.SizeAll;
                }
            } else {
                var delta = mousePos - _prevMousePos;

                var cmd = GetDragMoveCommand(element);
                cmd?.Execute(new DragResult() { Delta = delta, Mode = _mode });

                _prevMousePos = mousePos;
            }
        }

        public static readonly DependencyProperty DragStartCommandProperty =
            DependencyProperty.RegisterAttached("DragStartCommand", typeof(ICommand), typeof(DragCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetDragStartCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(DragStartCommandProperty);
        }

        public static void SetDragStartCommand(DependencyObject obj, bool value) {
            obj.SetValue(DragStartCommandProperty, value);
        }

        public static readonly DependencyProperty DragStopCommandProperty =
            DependencyProperty.RegisterAttached("DragStopCommand", typeof(ICommand), typeof(DragCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetDragStopCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(DragStopCommandProperty);
        }

        public static void SetDragStopCommand(DependencyObject obj, bool value) {
            obj.SetValue(DragStopCommandProperty, value);
        }

        public static readonly DependencyProperty DragMoveCommandProperty =
            DependencyProperty.RegisterAttached("DragMoveCommand", typeof(ICommand), typeof(DragCommandBehavior), new PropertyMetadata(null));

        public static ICommand GetDragMoveCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(DragMoveCommandProperty);
        }

        public static void SetDragMoveCommand(DependencyObject obj, bool value) {
            obj.SetValue(DragMoveCommandProperty, value);
        }
    }

    public enum DragMode {
        Move,
        Resize_Top_Left,
        Resize_Top_Right,
        Resize_Bottom_Left,
        Resize_Bottom_Right,
        Resize_Bottom,
        Resize_Top,
        Resize_Left,
        Resize_Right
    }

    public class DragResult {
        public DragMode Mode { get; set; }
        public Vector Delta { get; set; }
    }
}