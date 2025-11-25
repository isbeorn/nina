#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Data {
    public delegate void CurrentChangingEventHandler(object sender, CurrentChangingEventArgs e);

    public class CurrentChangingEventArgs : System.EventArgs {
        public bool Cancel { get; set; }
    }

    public class GroupDescription { }

    public class SortDescription { }

    public class SortDescriptionCollection : System.Collections.ObjectModel.Collection<SortDescription> { }

    public class Binding : BindingBase {
        public Binding() { }
        public Binding(string path) { Path = path; }

        public string Path { get; set; }
        public BindingMode Mode { get; set; }
        public object Source { get; set; }
        public string UpdateSourceTrigger { get; set; }
        public object Converter { get; set; }
        public object ConverterParameter { get; set; }
    }

    public interface IValueConverter { }
    public class BindingBase { }
    public enum BindingMode { OneWay, TwoWay, OneTime, OneWayToSource, Default }
    public class MultiBinding : BindingBase { }
    public interface IMultiValueConverter { }

    public interface ICollectionView : System.Collections.IEnumerable, System.Collections.Specialized.INotifyCollectionChanged {
        System.Collections.IEnumerable SourceCollection { get; }
        object CurrentItem { get; }
        int CurrentPosition { get; }
        bool IsCurrentAfterLast { get; }
        bool IsCurrentBeforeFirst { get; }
        System.Globalization.CultureInfo Culture { get; set; }
        System.Predicate<object> Filter { get; set; }
        bool CanFilter { get; }
        SortDescriptionCollection SortDescriptions { get; }
        bool CanSort { get; }
        bool CanGroup { get; }
        System.Collections.ObjectModel.ObservableCollection<GroupDescription> GroupDescriptions { get; }
        System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups { get; }
        bool IsEmpty { get; }
        event CurrentChangingEventHandler CurrentChanging;
        event System.EventHandler CurrentChanged;
        bool MoveCurrentToFirst();
        bool MoveCurrentToLast();
        bool MoveCurrentToNext();
        bool MoveCurrentToPrevious();
        bool MoveCurrentTo(object item);
        bool MoveCurrentToPosition(int position);
        void Refresh();
        System.IDisposable DeferRefresh();
        bool Contains(object item);
    }
}

namespace System.Windows {
    public static class MessageBox {
        public static MessageBoxResult Show(string messageBoxText) => MessageBoxResult.OK;
        public static MessageBoxResult Show(string messageBoxText, string caption) => MessageBoxResult.OK;
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button) => MessageBoxResult.OK;
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) => MessageBoxResult.OK;
    }
    public enum SizeToContent {
        Manual = 0,
        Width = 1,
        Height = 2,
        WidthAndHeight = 3
    }

    public enum MessageBoxResult {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    public enum MessageBoxButton {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }

    public enum MessageBoxImage {
        None = 0,
        Error = 16,
        Question = 32,
        Warning = 48,
        Information = 64
    }

    public enum Visibility {
        Visible = 0,
        Hidden = 1,
        Collapsed = 2
    }

    public class Style { }

    public class DependencyObject {
        // Stub for WPF DependencyObject
    }

    public class Application {
        public static Application Current { get; } = new Application();
        public Window MainWindow { get; set; }
        public Threading.Dispatcher Dispatcher { get; } = Threading.Dispatcher.CurrentDispatcher;
        public ResourceDictionary Resources { get; set; } = new ResourceDictionary();
        public WindowCollection Windows { get; } = new WindowCollection();
        public object TryFindResource(object key) => null;

        // Store reference to ASP.NET Core application lifetime for shutdown
        public static Microsoft.Extensions.Hosting.IHostApplicationLifetime ApplicationLifetime { get; set; }

        public void Shutdown() {
            ApplicationLifetime?.StopApplication();
        }

        public void Shutdown(int exitCode) {
            ApplicationLifetime?.StopApplication();
        }
    }

    public class WindowCollection : System.Collections.Generic.List<Window> {
        // Collection of Window objects - empty in headless mode
    }

    public class Window : DependencyObject {
        public bool IsFocused { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public WindowState WindowState { get; set; }
        public object SizeToContent { get; set; }
        public string Title { get; set; }
        public object Background { get; set; }
        public object ResizeMode { get; set; }
        public object WindowStyle { get; set; }
        public double MinHeight { get; set; }
        public double MinWidth { get; set; }
        public object Style { get; set; }
        public object Content { get; set; }
        public event EventHandler Closing;
        public object DataContext { get; set; }
        public object Owner { get; set; }
        public WindowStartupLocation WindowStartupLocation { get; set; }
        public double Opacity { get; set; } = 1.0;
        public bool? DialogResult { get; set; }
        public Threading.Dispatcher Dispatcher { get; } = Threading.Dispatcher.CurrentDispatcher;
        public event EventHandler Closed;
        public event System.EventHandler SourceInitialized;

        public bool? ShowDialog() => true;
        public void Show() { }
        public void Close() {
            Closed?.Invoke(this, EventArgs.Empty);
        }
        public bool Focus() => true;
        public bool Activate() => true;

        // Added for stub compatibility
        public object GetValue(object dp) => null;
        public void SetValue(object dp, object value) { }
        public void InvalidateMeasure() { }
        public static object Register(string name, System.Type propertyType, System.Type ownerType, object metadata) => null;
    }

    public enum WindowStartupLocation {
        Manual = 0,
        CenterScreen = 1,
        CenterOwner = 2
    }

    public enum WindowState {
        Normal = 0,
        Minimized = 1,
        Maximized = 2
    }

    public enum ResizeMode {
        NoResize = 0,
        CanMinimize = 1,
        CanResize = 2,
        CanResizeWithGrip = 3
    }

    public enum WindowStyle {
        None = 0,
        SingleBorderWindow = 1,
        ThreeDBorderWindow = 2,
        ToolWindow = 3
    }

    public class ResourceDictionary : System.Collections.Generic.Dictionary<object, object> {
        public ResourceDictionary() : base() { }

        // Always return null - resources are never populated in headless mode
        public new object this[object key] {
            get => null;
            set { } // No-op
        }
    }

    public struct Point {
        private OpenCvSharp.Point2d _point;

        public double X {
            get => _point.X;
            set => _point.X = value;
        }

        public double Y {
            get => _point.Y;
            set => _point.Y = value;
        }

        public Point(double x, double y) {
            _point = new OpenCvSharp.Point2d(x, y);
        }

        public static implicit operator OpenCvSharp.Point2d(Point p) =>
            new OpenCvSharp.Point2d(p.X, p.Y);

        public static implicit operator Point(OpenCvSharp.Point2d p) =>
            new Point(p.X, p.Y);

        public static Vector operator -(Point point1, Point point2) =>
            new Vector(point1.X - point2.X, point1.Y - point2.Y);

        public static Point operator +(Point point, Vector vector) =>
            new Point(point.X + vector.X, point.Y + vector.Y);

        public static Point operator -(Point point, Vector vector) =>
            new Point(point.X - vector.X, point.Y - vector.Y);
    }

    public struct Vector {
        private OpenCvSharp.Vec2d _vec;

        public double X {
            get => _vec.Item0;
            set => _vec.Item0 = value;
        }

        public double Y {
            get => _vec.Item1;
            set => _vec.Item1 = value;
        }

        public Vector(double x, double y) {
            _vec = new OpenCvSharp.Vec2d(x, y);
        }

        public double Length => OpenCvSharp.Cv2.Norm(_vec);

        public void Normalize() {
            double length = Length;
            if (length > 0) {
                _vec = _vec / length;
            }
        }

        public static Vector operator +(Vector v1, Vector v2) =>
            new Vector { _vec = v1._vec + v2._vec };

        public static Vector operator -(Vector v1, Vector v2) =>
            new Vector { _vec = v1._vec - v2._vec };

        public static Vector operator *(Vector v, double scalar) =>
            new Vector { _vec = v._vec * scalar };

        public static Vector operator *(double scalar, Vector v) => v * scalar;

        public static double operator *(Vector v1, Vector v2) =>
            v1.X * v2.X + v1.Y * v2.Y;

        public static implicit operator OpenCvSharp.Vec2d(Vector v) =>
            new OpenCvSharp.Vec2d(v.X, v.Y);

        public static implicit operator Vector(OpenCvSharp.Vec2d v) =>
            new Vector(v.Item0, v.Item1);
    }
}

namespace System.Windows.Controls {

    public class Button : System.Windows.DependencyObject {
        public string Name { get; set; }
        public object Content { get; set; }
        public object ToolTip { get; set; }
        public System.Windows.Visibility Visibility { get; set; }

        public void RaiseEvent(System.Windows.RoutedEventArgs e) { }
    }

    public class TextBlock : System.Windows.DependencyObject {
        public string Text { get; set; }
    }

    public class TextBox : System.Windows.DependencyObject {
        public string Text { get; set; }
    }

    public class Label : System.Windows.DependencyObject {
        public object Content { get; set; }
    }
}

namespace System.Windows.Controls.Primitives {

    public class ButtonBase {
        public static readonly System.Windows.RoutedEvent ClickEvent = new System.Windows.RoutedEvent();
    }
}

namespace System.Windows {

    public class RoutedEventArgs : EventArgs {
        public RoutedEvent RoutedEvent { get; set; }

        public RoutedEventArgs(RoutedEvent routedEvent) {
            RoutedEvent = routedEvent;
        }

        public RoutedEventArgs() { }
    }

    public class RoutedEvent {
        // Stub
    }
}

namespace System.Windows.Media {

    public static class VisualTreeHelper {
        public static int GetChildrenCount(DependencyObject reference) {
            // In headless mode, no visual tree
            return 0;
        }

        public static DependencyObject GetChild(DependencyObject reference, int childIndex) {
            // In headless mode, no visual tree
            return null;
        }
    }
}
