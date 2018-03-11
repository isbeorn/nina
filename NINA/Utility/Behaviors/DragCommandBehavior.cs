using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINA.Utility.Behaviors {
    class DragCommandBehavior {
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

        private static void OnDragChanged(object sender, DependencyPropertyChangedEventArgs e) {
            // ignoring error checking
            var element = (UIElement)sender;
            var isDrag = (bool)(e.NewValue);

            Instance = new DragCommandBehavior();

            if (isDrag) {
                element.MouseLeftButtonDown += Instance.ElementOnMouseLeftButtonDown;
                element.MouseLeftButtonUp += Instance.ElementOnMouseLeftButtonUp;
                element.MouseMove += Instance.ElementOnMouseMove;
            } else {
                element.MouseLeftButtonDown -= Instance.ElementOnMouseLeftButtonDown;
                element.MouseLeftButtonUp -= Instance.ElementOnMouseLeftButtonUp;
                element.MouseMove -= Instance.ElementOnMouseMove;
            }
        }

        private void ElementOnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs) {
            /*var parent = Application.Current.MainWindow;
            _mouseStartPosition2 = mouseButtonEventArgs.GetPosition(parent);*/

            var element = (FrameworkElement)sender;
            element.CaptureMouse();

            var parent = (UIElement)element.Parent;
            var mousePos = mouseButtonEventArgs.GetPosition(parent);

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

        private void ElementOnMouseMove(object sender, MouseEventArgs mouseEventArgs) {
            /*var parent = Application.Current.MainWindow;
            var mousePos = mouseEventArgs.GetPosition(parent);
            var diff = (mousePos - _mouseStartPosition2);
            if (!((UIElement)sender).IsMouseCaptured) return;
            Transform.X = _elementStartPosition2.X + diff.X;
            Transform.Y = _elementStartPosition2.Y + diff.Y;*/
            var element = (FrameworkElement)sender;
            if (!element.IsMouseCaptured) return;
            var parent = (UIElement)element.Parent;
            var mousePos = mouseEventArgs.GetPosition(parent);

            var delta = mousePos - _prevMousePos;

            var cmd = GetDragMoveCommand(element);
            cmd?.Execute(delta);

            _prevMousePos = mousePos;
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
}
